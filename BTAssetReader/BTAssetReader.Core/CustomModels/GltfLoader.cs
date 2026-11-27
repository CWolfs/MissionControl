using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.CustomModels {
  /// <summary>
  /// Pure-C# glTF 2.0 (.glb and .gltf) loader producing Unity-space (left-handed Y-up) RawMeshData,
  /// embedded PBR materials, optional named sub-meshes (_COL, _LOD1, _LOD2, _glass, _shell, split_*),
  /// and KHR_lights_punctual lights.
  /// </summary>
  public static class GltfLoader {
    private const uint GlbMagic = 0x46546C67; // "glTF"
    private const uint ChunkJson = 0x4E4F534A; // "JSON"
    private const uint ChunkBin = 0x004E4942;  // "BIN\0"

    private sealed class MeshSlotAccumulator {
      public readonly List<Float3> Vertices = new List<Float3>();
      public readonly List<Float3> Normals = new List<Float3>();
      public readonly List<Float4> Tangents = new List<Float4>();
      public readonly List<Float2> Uv0 = new List<Float2>();
      public readonly List<int> VertexPartIds = new List<int>();
      public readonly List<ModelPartInfo> Parts = new List<ModelPartInfo>();
      private readonly Dictionary<string, int> _partNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      private int _currentPartId = -1;
      public readonly Dictionary<int, List<int>> IndicesByMaterial = new Dictionary<int, List<int>>();
      public readonly List<int> MaterialOrder = new List<int>();
      public bool HasNormals = false;
      public bool HasTangents = false;
      public bool HasUv0 = false;

      public int BeginPart(string rawName) {
        string baseName = string.IsNullOrWhiteSpace(rawName) ? $"Part_{Parts.Count + 1}" : rawName.Trim();
        int count = _partNameCounts.TryGetValue(baseName, out int c) ? c + 1 : 1;
        _partNameCounts[baseName] = count;
        string uniqueName = count == 1 ? baseName : $"{baseName} #{count}";
        _currentPartId = Parts.Count;
        Parts.Add(new ModelPartInfo { Name = uniqueName });
        return _currentPartId;
      }

      public void AddPrimitive(
        Float3[] pos,
        Float3[] norm,
        Float4[] tang,
        Float2[] uv,
        int[] indices,
        int materialIndex,
        Mat4 worldMatrix) {
        if (pos == null || pos.Length == 0 || indices == null || indices.Length < 3) return;

        int baseVertex = Vertices.Count;
        bool hasN = norm != null && norm.Length == pos.Length;
        bool hasT = tang != null && tang.Length == pos.Length;
        bool hasU = uv != null && uv.Length == pos.Length;

        if (hasN) HasNormals = true;
        if (hasT) HasTangents = true;
        if (hasU) HasUv0 = true;

        // Converting glTF (right-handed CCW, dot(cross(e1,e2), n) > 0) to Unity (left-handed CW, dot(cross(e1,e2), n) > 0)
        // via X negation (det = -1) negates dot(cross, n), so we MUST reverse triangle winding for positive-det transforms.
        // If worldMatrix itself has a negative determinant, the two reflections cancel and winding stays unchanged.
        bool negDet = worldMatrix.Determinant3x3() < 0f;
        bool flipWinding = !negDet;

        for (int i = 0; i < pos.Length; i++) {
          Float3 wp = worldMatrix.TransformPoint(pos[i]);
          // glTF right-handed -> Unity left-handed: negate X
          Vertices.Add(new Float3(-wp.X, wp.Y, wp.Z));
          VertexPartIds.Add(_currentPartId);

          if (hasN) {
            Float3 wn = Normalize3(worldMatrix.TransformNormal(norm[i]));
            Normals.Add(new Float3(-wn.X, wn.Y, wn.Z));
          } else {
            Normals.Add(new Float3(0f, 1f, 0f));
          }

          if (hasT) {
            Float3 wt = Normalize3(worldMatrix.TransformDirection(new Float3(tang[i].X, tang[i].Y, tang[i].Z)));
            // X-mirror and V-flip (1 - v) cancel for tangent handedness unless worldMatrix has negative determinant
            float tw = negDet ? -tang[i].W : tang[i].W;
            Tangents.Add(new Float4(-wt.X, wt.Y, wt.Z, tw));
          } else {
            Tangents.Add(new Float4(1f, 0f, 0f, 1f));
          }

          if (hasU) {
            // glTF UV origin is top-left; Unity UV origin is bottom-left
            Uv0.Add(new Float2(uv[i].X, 1f - uv[i].Y));
          } else {
            Uv0.Add(new Float2(0f, 0f));
          }
        }

        if (!IndicesByMaterial.TryGetValue(materialIndex, out var triList)) {
          triList = new List<int>();
          IndicesByMaterial[materialIndex] = triList;
          MaterialOrder.Add(materialIndex);
        }

        for (int i = 0; i + 2 < indices.Length; i += 3) {
          int a = baseVertex + indices[i];
          int b = baseVertex + indices[i + 1];
          int c = baseVertex + indices[i + 2];
          if (flipWinding) {
            triList.Add(a);
            triList.Add(c);
            triList.Add(b);
          } else {
            triList.Add(a);
            triList.Add(b);
            triList.Add(c);
          }
        }
        if (_currentPartId >= 0 && _currentPartId < Parts.Count) {
          Parts[_currentPartId].TriangleCount += indices.Length / 3;
        }
      }

      public RawMeshData ToRawMesh(string name, out List<int> usedMaterialIndices, List<int> preferredMaterialOrder = null) {
        usedMaterialIndices = new List<int>();
        if (Vertices.Count == 0 || MaterialOrder.Count == 0) return null;

        List<int> orderedMats = preferredMaterialOrder != null && preferredMaterialOrder.Count > 0
          ? preferredMaterialOrder
          : MaterialOrder;

        int subCount = orderedMats.Count;
        var subMeshes = new RawSubMesh[subCount];
        var subIndices = new int[subCount][];

        for (int s = 0; s < subCount; s++) {
          int matIdx = orderedMats[s];
          usedMaterialIndices.Add(matIdx);
          int[] tris = IndicesByMaterial.TryGetValue(matIdx, out var list) ? list.ToArray() : Array.Empty<int>();
          subIndices[s] = tris;
          subMeshes[s] = new RawSubMesh {
            Topology = 0, // Triangles
            IndexCount = (uint)tris.Length
          };
        }

        var mesh = new RawMeshData {
          Name = name,
          VertexCount = Vertices.Count,
          IndexFormat = Vertices.Count > 65535 ? 1 : 0,
          Vertices = Vertices.ToArray(),
          Normals = HasNormals ? Normals.ToArray() : null,
          Tangents = HasTangents ? Tangents.ToArray() : null,
          Uv0 = HasUv0 ? Uv0.ToArray() : null,
          SubMeshes = subMeshes,
          SubMeshIndices = subIndices,
          OriginalIsReadable = true,
          Parts = Parts.Count > 0 ? Parts : null,
          VertexPartIds = Parts.Count > 0 ? VertexPartIds.ToArray() : null
        };

        MeshNormaliser.RecalculateNormalsIfMissing(mesh);
        MeshNormaliser.RecalculateTangentsIfMissing(mesh);
        MeshNormaliser.RecalculateBounds(mesh);
        MeshNormaliser.RecalculatePartBounds(mesh);
        return mesh;
      }
    }

    public static CustomModelBundle Load(string filePath) {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
        throw new FileNotFoundException("Model file not found.", filePath);
      }

      byte[] fileBytes = File.ReadAllBytes(filePath);
      string baseDir = Path.GetDirectoryName(filePath) ?? string.Empty;
      string jsonText;
      byte[] glbBinChunk = null;

      if (fileBytes.Length >= 12 && BitConverter.ToUInt32(fileBytes, 0) == GlbMagic) {
        uint version = BitConverter.ToUInt32(fileBytes, 4);
        if (version != 2) throw new InvalidDataException($"Unsupported GLB version {version}; expected glTF 2.0.");

        int offset = 12;
        jsonText = null;
        while (offset + 8 <= fileBytes.Length) {
          uint chunkLen = BitConverter.ToUInt32(fileBytes, offset);
          uint chunkType = BitConverter.ToUInt32(fileBytes, offset + 4);
          offset += 8;
          if (offset + chunkLen > fileBytes.Length) break;

          if (chunkType == ChunkJson && jsonText == null) {
            jsonText = Encoding.UTF8.GetString(fileBytes, offset, (int)chunkLen).TrimEnd('\0', ' ');
          } else if (chunkType == ChunkBin && glbBinChunk == null) {
            glbBinChunk = new byte[chunkLen];
            Buffer.BlockCopy(fileBytes, offset, glbBinChunk, 0, (int)chunkLen);
          }
          offset += (int)chunkLen;
        }
        if (string.IsNullOrEmpty(jsonText)) throw new InvalidDataException("GLB file is missing a JSON chunk.");
      } else {
        jsonText = Encoding.UTF8.GetString(fileBytes);
      }

      var root = MiniJson.AsDict(MiniJson.Parse(jsonText));
      if (root == null) throw new InvalidDataException("Failed to parse glTF JSON.");

      // Fail loudly with an actionable message if the file requires unsupported mesh compression (e.g. Draco/meshopt)
      var reqExts = MiniJson.GetList(root, "extensionsRequired");
      if (reqExts != null) {
        foreach (object eo in reqExts) {
          string extName = eo?.ToString() ?? "";
          if (extName.Equals("KHR_draco_mesh_compression", StringComparison.OrdinalIgnoreCase) ||
              extName.Equals("EXT_meshopt_compression", StringComparison.OrdinalIgnoreCase)) {
            throw new NotSupportedException(
              $"'{Path.GetFileName(filePath)}' requires '{extName}', which is not supported. Re-export from Blender as uncompressed .glb or .gltf.");
          }
        }
      }

      // Resolve buffers
      var buffersList = MiniJson.GetList(root, "buffers") ?? new List<object>();
      var buffers = new byte[buffersList.Count][];
      for (int i = 0; i < buffersList.Count; i++) {
        var bDict = MiniJson.AsDict(buffersList[i]);
        string uri = MiniJson.GetString(bDict, "uri");
        if (string.IsNullOrEmpty(uri)) {
          buffers[i] = glbBinChunk ?? Array.Empty<byte>();
        } else {
          buffers[i] = ResolveUriBytes(uri, baseDir, required: true, ownerFileName: Path.GetFileName(filePath));
        }
      }

      var bufferViews = MiniJson.GetList(root, "bufferViews") ?? new List<object>();
      var accessors = MiniJson.GetList(root, "accessors") ?? new List<object>();
      var imagesList = MiniJson.GetList(root, "images") ?? new List<object>();
      var texturesList = MiniJson.GetList(root, "textures") ?? new List<object>();
      var materialsList = MiniJson.GetList(root, "materials") ?? new List<object>();
      var meshesList = MiniJson.GetList(root, "meshes") ?? new List<object>();
      var nodesList = MiniJson.GetList(root, "nodes") ?? new List<object>();

      // Resolve raw image bytes
      var imageBytes = new byte[imagesList.Count][];
      var imageMimes = new string[imagesList.Count];
      for (int i = 0; i < imagesList.Count; i++) {
        var imgDict = MiniJson.AsDict(imagesList[i]);
        imageMimes[i] = MiniJson.GetString(imgDict, "mimeType", "image/png");
        int bvIdx = MiniJson.GetInt(imgDict, "bufferView", -1);
        if (bvIdx >= 0 && bvIdx < bufferViews.Count) {
          imageBytes[i] = ReadBufferViewBytes(MiniJson.AsDict(bufferViews[bvIdx]), buffers);
        } else {
          string uri = MiniJson.GetString(imgDict, "uri");
          if (!string.IsNullOrEmpty(uri)) {
            imageBytes[i] = ResolveUriBytes(uri, baseDir);
          }
        }
      }

      // Resolve PBR materials
      var parsedMaterials = new List<CustomPbrMaterialData>();
      for (int i = 0; i < materialsList.Count; i++) {
        var mDict = MiniJson.AsDict(materialsList[i]);
        var mat = new CustomPbrMaterialData {
          Name = MiniJson.GetString(mDict, "name", $"Material_{i}"),
          AlphaMode = MiniJson.GetString(mDict, "alphaMode", "OPAQUE"),
          AlphaCutoff = MiniJson.GetFloat(mDict, "alphaCutoff", 0.5f),
          DoubleSided = MiniJson.GetBool(mDict, "doubleSided", false)
        };

        var pbr = MiniJson.GetDict(mDict, "pbrMetallicRoughness");
        if (pbr != null) {
          var bcf = MiniJson.GetList(pbr, "baseColorFactor");
          if (bcf != null && bcf.Count >= 4) {
            mat.BaseColorFactor = new Float4(
              LinearToSrgb(Convert.ToSingle(bcf[0])),
              LinearToSrgb(Convert.ToSingle(bcf[1])),
              LinearToSrgb(Convert.ToSingle(bcf[2])),
              Convert.ToSingle(bcf[3]));
          }
          TryExtractTexture(pbr, "baseColorTexture", texturesList, imageBytes, imageMimes, out mat.BaseColorImageBytes, out mat.BaseColorImageMime);
          TryExtractTexture(pbr, "metallicRoughnessTexture", texturesList, imageBytes, imageMimes, out mat.MetallicRoughnessImageBytes, out mat.MetallicRoughnessImageMime);
          float defaultMetallic = mat.MetallicRoughnessImageBytes != null ? 1.0f : 0.0f;
          mat.MetallicFactor = MiniJson.GetFloat(pbr, "metallicFactor", defaultMetallic);
          mat.RoughnessFactor = MiniJson.GetFloat(pbr, "roughnessFactor", 0.85f);
        }

        TryExtractTexture(mDict, "normalTexture", texturesList, imageBytes, imageMimes, out mat.NormalImageBytes, out mat.NormalImageMime);
        var normInfo = MiniJson.GetDict(mDict, "normalTexture");
        if (normInfo != null) {
          mat.NormalScale = MiniJson.GetFloat(normInfo, "scale", 1.0f);
        }
        TryExtractTexture(mDict, "occlusionTexture", texturesList, imageBytes, imageMimes, out mat.OcclusionImageBytes, out mat.OcclusionImageMime);
        TryExtractTexture(mDict, "emissiveTexture", texturesList, imageBytes, imageMimes, out mat.EmissiveImageBytes, out mat.EmissiveImageMime);

        var ef = MiniJson.GetList(mDict, "emissiveFactor");
        if (ef != null && ef.Count >= 3) {
          mat.EmissiveFactor = new Float3(
            LinearToSrgb(Convert.ToSingle(ef[0])),
            LinearToSrgb(Convert.ToSingle(ef[1])),
            LinearToSrgb(Convert.ToSingle(ef[2])));
        }

        parsedMaterials.Add(mat);
      }
      if (parsedMaterials.Count == 0) {
        parsedMaterials.Add(new CustomPbrMaterialData { Name = "DefaultMaterial" });
      }

      // Resolve KHR_lights_punctual definitions
      var punctualLights = new List<Dictionary<string, object>>();
      var rootExts = MiniJson.GetDict(root, "extensions");
      var khrLights = MiniJson.GetDict(rootExts, "KHR_lights_punctual");
      var khrLightsArr = MiniJson.GetList(khrLights, "lights");
      if (khrLightsArr != null) {
        foreach (object lo in khrLightsArr) {
          var ld = MiniJson.AsDict(lo);
          if (ld != null) punctualLights.Add(ld);
        }
      }

      // Determine root nodes of default scene
      var rootNodeIndices = new List<int>();
      var scenesList = MiniJson.GetList(root, "scenes");
      int sceneIdx = MiniJson.GetInt(root, "scene", 0);
      if (scenesList != null && sceneIdx >= 0 && sceneIdx < scenesList.Count) {
        var sc = MiniJson.AsDict(scenesList[sceneIdx]);
        var sn = MiniJson.GetList(sc, "nodes");
        if (sn != null) {
          foreach (object n in sn) rootNodeIndices.Add(Convert.ToInt32(n));
        }
      } else {
        // Fallback: all nodes not referenced as a child
        var isChild = new HashSet<int>();
        foreach (object no in nodesList) {
          var ch = MiniJson.GetList(MiniJson.AsDict(no), "children");
          if (ch != null) foreach (object c in ch) isChild.Add(Convert.ToInt32(c));
        }
        for (int i = 0; i < nodesList.Count; i++) {
          if (!isChild.Contains(i)) rootNodeIndices.Add(i);
        }
      }

      var lod0Acc = new MeshSlotAccumulator();
      var lod1Acc = new MeshSlotAccumulator();
      var lod2Acc = new MeshSlotAccumulator();
      var colAcc = new MeshSlotAccumulator();
      var glassAcc = new MeshSlotAccumulator();
      var shellAcc = new MeshSlotAccumulator();
      var shellColAcc = new MeshSlotAccumulator();
      var splitPieces = new List<FracturePieceData>();
      var extractedLights = new List<PropLightData>();

      void TraverseNode(int nodeIdx, Mat4 parentWorld, string parentPartHint) {
        if (nodeIdx < 0 || nodeIdx >= nodesList.Count) return;
        var node = MiniJson.AsDict(nodesList[nodeIdx]);
        if (node == null) return;

        string nodeName = MiniJson.GetString(node, "name", $"Node_{nodeIdx}");
        Mat4 local = ReadNodeMatrix(node);
        Mat4 world = Mat4.Multiply(parentWorld, local);

        // Check KHR_lights_punctual on this node
        var nodeExts = MiniJson.GetDict(node, "extensions");
        var nodeLightExt = MiniJson.GetDict(nodeExts, "KHR_lights_punctual");
        if (nodeLightExt != null) {
          int lightIdx = MiniJson.GetInt(nodeLightExt, "light", -1);
          if (lightIdx >= 0 && lightIdx < punctualLights.Count) {
            PropLightData pl = ConvertPunctualLight(punctualLights[lightIdx], nodeName, world);
            if (pl != null) extractedLights.Add(pl);
          }
        }

        // Check mesh on this node
        int meshIdx = MiniJson.GetInt(node, "mesh", -1);
        if (meshIdx >= 0 && meshIdx < meshesList.Count) {
          var meshDict = MiniJson.AsDict(meshesList[meshIdx]);
          string meshName = MiniJson.GetString(meshDict, "name", nodeName);
          string tagName = !string.IsNullOrEmpty(nodeName) ? nodeName : meshName;
          // Prefer clean Sketchfab parent wrapper name (e.g. "SM_WoodLogs_v6" over "SM_WoodLogs_v6_WoodLogs_M_0")
          string partName = (!string.IsNullOrEmpty(parentPartHint) && tagName.StartsWith(parentPartHint + "_", StringComparison.OrdinalIgnoreCase))
            ? parentPartHint
            : tagName;

          string slot = ClassifySlotName(tagName);
          if (slot == "Split") {
            var splitAcc = new MeshSlotAccumulator();
            AppendGltfMesh(meshDict, accessors, bufferViews, buffers, world, splitAcc);
            RawMeshData splitMesh = splitAcc.ToRawMesh(tagName, out _);
            if (splitMesh != null) {
              Float3 centroid = splitMesh.LocalAabb.Center;
              // Re-centre split piece around its own centroid so RigidBody pivots naturally
              for (int vi = 0; vi < splitMesh.Vertices.Length; vi++) {
                splitMesh.Vertices[vi] = new Float3(
                  splitMesh.Vertices[vi].X - centroid.X,
                  splitMesh.Vertices[vi].Y - centroid.Y,
                  splitMesh.Vertices[vi].Z - centroid.Z);
              }
              MeshNormaliser.RecalculateBounds(splitMesh);
              splitPieces.Add(new FracturePieceData {
                Name = tagName,
                Centroid = centroid,
                Mesh = splitMesh,
                LocalBounds = splitMesh.LocalAabb,
                EstimatedMass = EstimateMass(splitMesh.LocalAabb)
              });
            }
          } else {
            MeshSlotAccumulator target = slot switch {
              "COL" => colAcc,
              "LOD1" => lod1Acc,
              "LOD2" => lod2Acc,
              "Glass" => glassAcc,
              "Shell" => shellAcc,
              "ShellCOL" => shellColAcc,
              _ => lod0Acc
            };
            target.BeginPart(partName);
            AppendGltfMesh(meshDict, accessors, bufferViews, buffers, world, target);
          }
        }

        var children = MiniJson.GetList(node, "children");
        if (children != null) {
          string childHint = (meshIdx < 0 && children.Count == 1 && IsMeaningfulPartWrapper(nodeName))
            ? nodeName
            : null;
          foreach (object c in children) {
            TraverseNode(Convert.ToInt32(c), world, childHint);
          }
        }
      }

      foreach (int rn in rootNodeIndices) {
        TraverseNode(rn, Mat4.Identity, null);
      }

      string modelName = Path.GetFileNameWithoutExtension(filePath);
      RawMeshData lod0Mesh = lod0Acc.ToRawMesh($"{modelName}_LOD0", out List<int> usedMatIndices);
      if (lod0Mesh == null) {
        throw new InvalidDataException($"No renderable triangle geometry found in '{filePath}'.");
      }

      // Reorder bundle Materials to match LOD0 submesh order 1:1
      var orderedMaterials = new List<CustomPbrMaterialData>(usedMatIndices.Count);
      foreach (int matIdx in usedMatIndices) {
        if (matIdx >= 0 && matIdx < parsedMaterials.Count) {
          orderedMaterials.Add(parsedMaterials[matIdx]);
        } else {
          orderedMaterials.Add(parsedMaterials[0]);
        }
      }

      var bundle = new CustomModelBundle {
        Name = modelName,
        SourceFilePath = filePath,
        Lod0Mesh = lod0Mesh,
        Lod1Mesh = lod1Acc.ToRawMesh($"{modelName}_LOD1", out _, usedMatIndices),
        Lod2Mesh = lod2Acc.ToRawMesh($"{modelName}_LOD2", out _, usedMatIndices),
        ColMesh = colAcc.ToRawMesh($"{modelName}_COL", out _),
        GlassMesh = glassAcc.ToRawMesh($"{modelName}_glass", out _),
        ShellMesh = shellAcc.ToRawMesh($"{modelName}_shell", out _),
        ShellColMesh = shellColAcc.ToRawMesh($"{modelName}_shell_COL", out _),
        CustomSplits = splitPieces,
        Materials = orderedMaterials,
        Lights = extractedLights
      };

      return bundle;
    }

    private static bool IsMeaningfulPartWrapper(string name) {
      if (string.IsNullOrWhiteSpace(name)) return false;
      string n = name.Trim();
      if (n.Equals("Sketchfab_model", StringComparison.OrdinalIgnoreCase) ||
          n.Equals("RootNode", StringComparison.OrdinalIgnoreCase) ||
          n.Equals("Scene", StringComparison.OrdinalIgnoreCase) ||
          n.Equals("Root", StringComparison.OrdinalIgnoreCase) ||
          n.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
          n.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) ||
          n.EndsWith(".dae", StringComparison.OrdinalIgnoreCase)) {
        return false;
      }
      return true;
    }

    internal static string ClassifySlotName(string name) {
      if (string.IsNullOrEmpty(name)) return "LOD0";
      string low = name.ToLowerInvariant();
      if (low.EndsWith("_shell_col") || low.EndsWith("_shellcol")) return "ShellCOL";
      if (low.EndsWith("_shell")) return "Shell";
      if (low.EndsWith("_col") || low.EndsWith("_collider")) return "COL";
      if (low.EndsWith("_lod1")) return "LOD1";
      if (low.EndsWith("_lod2")) return "LOD2";
      if (low.EndsWith("_glass")) return "Glass";
      if (low.StartsWith("split_") || low.Contains("_split_") || low.EndsWith("_split")) return "Split";
      return "LOD0";
    }

    private static void AppendGltfMesh(
      Dictionary<string, object> meshDict,
      List<object> accessors,
      List<object> bufferViews,
      byte[][] buffers,
      Mat4 world,
      MeshSlotAccumulator target) {
      var primitives = MiniJson.GetList(meshDict, "primitives");
      if (primitives == null) return;

      foreach (object po in primitives) {
        var prim = MiniJson.AsDict(po);
        if (prim == null) continue;
        int mode = MiniJson.GetInt(prim, "mode", 4); // 4 = TRIANGLES
        if (mode != 4) continue;

        var attrs = MiniJson.GetDict(prim, "attributes");
        if (attrs == null) continue;

        int posAcc = MiniJson.GetInt(attrs, "POSITION", -1);
        if (posAcc < 0) continue;

        Float3[] positions = ReadVec3Accessor(posAcc, accessors, bufferViews, buffers);
        if (positions == null || positions.Length == 0) continue;

        int normAcc = MiniJson.GetInt(attrs, "NORMAL", -1);
        Float3[] normals = normAcc >= 0 ? ReadVec3Accessor(normAcc, accessors, bufferViews, buffers) : null;

        int tangAcc = MiniJson.GetInt(attrs, "TANGENT", -1);
        Float4[] tangents = tangAcc >= 0 ? ReadVec4Accessor(tangAcc, accessors, bufferViews, buffers) : null;

        int uvAcc = MiniJson.GetInt(attrs, "TEXCOORD_0", -1);
        Float2[] uvs = uvAcc >= 0 ? ReadVec2Accessor(uvAcc, accessors, bufferViews, buffers) : null;

        int idxAcc = MiniJson.GetInt(prim, "indices", -1);
        int[] indices;
        if (idxAcc >= 0) {
          indices = ReadIndexAccessor(idxAcc, accessors, bufferViews, buffers);
        } else {
          indices = new int[positions.Length];
          for (int i = 0; i < indices.Length; i++) indices[i] = i;
        }

        int matIdx = MiniJson.GetInt(prim, "material", 0);
        target.AddPrimitive(positions, normals, tangents, uvs, indices, matIdx, world);
      }
    }

    private static PropLightData ConvertPunctualLight(Dictionary<string, object> ld, string nodeName, Mat4 world) {
      string type = MiniJson.GetString(ld, "type", "point").ToLowerInvariant();
      if (type == "directional") return null; // Props shouldn't spawn scene directional suns

      Float3 p = world.TransformPoint(new Float3(0f, 0f, 0f));
      // glTF lights point along -Z in local space; convert forward to Unity left-handed (+Z forward)
      Float3 fwdRh = Normalize3(world.TransformDirection(new Float3(0f, 0f, -1f)));
      Float3 fwdLh = new Float3(-fwdRh.X, fwdRh.Y, fwdRh.Z);
      Float3 euler = DirectionToEulerDegrees(fwdLh);

      var colList = MiniJson.GetList(ld, "color");
      Float4 color = colList != null && colList.Count >= 3
        ? new Float4(Convert.ToSingle(colList[0]), Convert.ToSingle(colList[1]), Convert.ToSingle(colList[2]), 1f)
        : new Float4(1f, 1f, 1f, 1f);

      float rawIntensity = MiniJson.GetFloat(ld, "intensity", 1f);
      // Blender exports candela/watt values that can be 10..1000; normalise sensibly to BT 0.5..5.0 range
      float btIntensity = rawIntensity > 20f ? Math.Min(10f, rawIntensity / 100f) : Math.Max(0.1f, rawIntensity);
      float range = MiniJson.GetFloat(ld, "range", 15f);
      if (range <= 0f) range = 15f;

      float innerDeg = 30f;
      float outerDeg = 50f;
      var spot = MiniJson.GetDict(ld, "spot");
      if (spot != null) {
        innerDeg = MiniJson.GetFloat(spot, "innerConeAngle", 0.5f) * (180f / (float)Math.PI);
        outerDeg = MiniJson.GetFloat(spot, "outerConeAngle", 0.785398f) * (180f / (float)Math.PI);
      }

      return new PropLightData {
        Name = MiniJson.GetString(ld, "name", nodeName),
        Type = type == "spot" ? "Spot" : "Point",
        Position = new Float3(-p.X, p.Y, p.Z),
        Rotation = euler,
        Color = color,
        Intensity = btIntensity,
        Radius = range,
        InnerAngle = Math.Max(1f, Math.Min(89f, innerDeg)),
        OuterAngle = Math.Max(2f, Math.Min(90f, outerDeg))
      };
    }

    private static Float3 DirectionToEulerDegrees(Float3 dir) {
      float len = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
      if (len < 1e-6f) return new Float3(0f, 0f, 0f);
      float x = dir.X / len, y = dir.Y / len, z = dir.Z / len;
      float pitch = (float)(-Math.Asin(Math.Max(-1f, Math.Min(1f, y))) * (180.0 / Math.PI));
      float yaw = (float)(Math.Atan2(x, z) * (180.0 / Math.PI));
      return new Float3(pitch, yaw, 0f);
    }

    private static void TryExtractTexture(
      Dictionary<string, object> parent,
      string key,
      List<object> texturesList,
      byte[][] imageBytes,
      string[] imageMimes,
      out byte[] bytes,
      out string mime) {
      bytes = null;
      mime = null;
      var texInfo = MiniJson.GetDict(parent, key);
      if (texInfo == null) return;
      int texIdx = MiniJson.GetInt(texInfo, "index", -1);
      if (texIdx < 0 || texIdx >= texturesList.Count) return;
      var texDict = MiniJson.AsDict(texturesList[texIdx]);
      int imgIdx = MiniJson.GetInt(texDict, "source", -1);
      if (imgIdx < 0 || imgIdx >= imageBytes.Length) return;
      bytes = imageBytes[imgIdx];
      mime = imageMimes[imgIdx];
    }

    private static byte[] ResolveUriBytes(string uri, string baseDir, bool required = false, string ownerFileName = null) {
      if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) {
        int comma = uri.IndexOf(',');
        if (comma >= 0) {
          return Convert.FromBase64String(uri.Substring(comma + 1));
        }
        if (required) throw new InvalidDataException($"Invalid embedded data URI in '{ownerFileName ?? "glTF"}'.");
        return Array.Empty<byte>();
      }

      string resolved = CustomModelLoader.ResolveExternalAssetPath(baseDir, uri);
      if (resolved != null) return File.ReadAllBytes(resolved);

      string unescaped = Uri.UnescapeDataString(uri);
      string full = Path.Combine(baseDir, unescaped);
      if (required) {
        throw new FileNotFoundException(
          $"Missing external glTF buffer '{unescaped}' referenced by '{ownerFileName ?? "glTF"}'. Keep the .bin file (and textures folder) next to the .gltf file, or use a self-contained .glb file.",
          full);
      }
      return Array.Empty<byte>();
    }

    private static byte[] ReadBufferViewBytes(Dictionary<string, object> bv, byte[][] buffers) {
      int bufIdx = MiniJson.GetInt(bv, "buffer", 0);
      int byteOffset = MiniJson.GetInt(bv, "byteOffset", 0);
      int byteLength = MiniJson.GetInt(bv, "byteLength", 0);
      if (bufIdx < 0 || bufIdx >= buffers.Length || buffers[bufIdx] == null) return Array.Empty<byte>();
      byte[] src = buffers[bufIdx];
      if (byteOffset < 0 || byteLength <= 0 || byteOffset + byteLength > src.Length) return Array.Empty<byte>();
      byte[] dst = new byte[byteLength];
      Buffer.BlockCopy(src, byteOffset, dst, 0, byteLength);
      return dst;
    }

    private static bool TryGetAccessorSpan(
      int accIdx,
      List<object> accessors,
      List<object> bufferViews,
      byte[][] buffers,
      out byte[] buf,
      out int offset,
      out int stride,
      out int count,
      out int componentType) {
      buf = null; offset = 0; stride = 0; count = 0; componentType = 0;
      if (accIdx < 0 || accIdx >= accessors.Count) return false;
      var acc = MiniJson.AsDict(accessors[accIdx]);
      if (acc == null) return false;

      int bvIdx = MiniJson.GetInt(acc, "bufferView", -1);
      if (bvIdx < 0 || bvIdx >= bufferViews.Count) return false;
      var bv = MiniJson.AsDict(bufferViews[bvIdx]);
      int bufIdx = MiniJson.GetInt(bv, "buffer", 0);
      if (bufIdx < 0 || bufIdx >= buffers.Length || buffers[bufIdx] == null) return false;

      buf = buffers[bufIdx];
      offset = MiniJson.GetInt(bv, "byteOffset", 0) + MiniJson.GetInt(acc, "byteOffset", 0);
      stride = MiniJson.GetInt(bv, "byteStride", 0);
      count = MiniJson.GetInt(acc, "count", 0);
      componentType = MiniJson.GetInt(acc, "componentType", 5126);
      return count > 0;
    }

    private static Float2[] ReadVec2Accessor(int accIdx, List<object> accessors, List<object> bufferViews, byte[][] buffers) {
      if (!TryGetAccessorSpan(accIdx, accessors, bufferViews, buffers, out byte[] buf, out int off, out int stride, out int count, out int compType)) return null;
      int elemSize = ComponentByteSize(compType);
      int step = stride > 0 ? stride : elemSize * 2;
      var result = new Float2[count];
      for (int i = 0; i < count; i++) {
        int p = off + i * step;
        if (p + elemSize * 2 > buf.Length) break;
        result[i] = new Float2(ReadComponentFloat(buf, p, compType), ReadComponentFloat(buf, p + elemSize, compType));
      }
      return result;
    }

    private static Float3[] ReadVec3Accessor(int accIdx, List<object> accessors, List<object> bufferViews, byte[][] buffers) {
      if (!TryGetAccessorSpan(accIdx, accessors, bufferViews, buffers, out byte[] buf, out int off, out int stride, out int count, out int compType)) return null;
      int elemSize = ComponentByteSize(compType);
      int step = stride > 0 ? stride : elemSize * 3;
      var result = new Float3[count];
      for (int i = 0; i < count; i++) {
        int p = off + i * step;
        if (p + elemSize * 3 > buf.Length) break;
        result[i] = new Float3(
          ReadComponentFloat(buf, p, compType),
          ReadComponentFloat(buf, p + elemSize, compType),
          ReadComponentFloat(buf, p + elemSize * 2, compType));
      }
      return result;
    }

    private static Float4[] ReadVec4Accessor(int accIdx, List<object> accessors, List<object> bufferViews, byte[][] buffers) {
      if (!TryGetAccessorSpan(accIdx, accessors, bufferViews, buffers, out byte[] buf, out int off, out int stride, out int count, out int compType)) return null;
      int elemSize = ComponentByteSize(compType);
      int step = stride > 0 ? stride : elemSize * 4;
      var result = new Float4[count];
      for (int i = 0; i < count; i++) {
        int p = off + i * step;
        if (p + elemSize * 4 > buf.Length) break;
        result[i] = new Float4(
          ReadComponentFloat(buf, p, compType),
          ReadComponentFloat(buf, p + elemSize, compType),
          ReadComponentFloat(buf, p + elemSize * 2, compType),
          ReadComponentFloat(buf, p + elemSize * 3, compType));
      }
      return result;
    }

    private static int[] ReadIndexAccessor(int accIdx, List<object> accessors, List<object> bufferViews, byte[][] buffers) {
      if (!TryGetAccessorSpan(accIdx, accessors, bufferViews, buffers, out byte[] buf, out int off, out int stride, out int count, out int compType)) return Array.Empty<int>();
      int elemSize = ComponentByteSize(compType);
      int step = stride > 0 ? stride : elemSize;
      var result = new int[count];
      for (int i = 0; i < count; i++) {
        int p = off + i * step;
        if (p + elemSize > buf.Length) break;
        result[i] = compType switch {
          5121 => buf[p],
          5123 => BitConverter.ToUInt16(buf, p),
          5125 => (int)BitConverter.ToUInt32(buf, p),
          5122 => BitConverter.ToInt16(buf, p),
          _ => 0
        };
      }
      return result;
    }

    private static int ComponentByteSize(int compType) => compType switch {
      5120 or 5121 => 1,
      5122 or 5123 => 2,
      5125 or 5126 => 4,
      _ => 4
    };

    private static float ReadComponentFloat(byte[] buf, int p, int compType) => compType switch {
      5126 => BitConverter.ToSingle(buf, p),
      5121 => buf[p] / 255f,
      5123 => BitConverter.ToUInt16(buf, p) / 65535f,
      5120 => Math.Max(-1f, (sbyte)buf[p] / 127f),
      5122 => Math.Max(-1f, BitConverter.ToInt16(buf, p) / 32767f),
      _ => 0f
    };

    private static Mat4 ReadNodeMatrix(Dictionary<string, object> node) {
      var matList = MiniJson.GetList(node, "matrix");
      if (matList != null && matList.Count == 16) {
        var m = new float[16];
        for (int i = 0; i < 16; i++) m[i] = Convert.ToSingle(matList[i]);
        return Mat4.FromColumnMajor(m);
      }

      Float3 t = new Float3(0f, 0f, 0f);
      var tl = MiniJson.GetList(node, "translation");
      if (tl != null && tl.Count >= 3) t = new Float3(Convert.ToSingle(tl[0]), Convert.ToSingle(tl[1]), Convert.ToSingle(tl[2]));

      Float4 r = new Float4(0f, 0f, 0f, 1f);
      var rl = MiniJson.GetList(node, "rotation");
      if (rl != null && rl.Count >= 4) r = new Float4(Convert.ToSingle(rl[0]), Convert.ToSingle(rl[1]), Convert.ToSingle(rl[2]), Convert.ToSingle(rl[3]));

      Float3 s = new Float3(1f, 1f, 1f);
      var sl = MiniJson.GetList(node, "scale");
      if (sl != null && sl.Count >= 3) s = new Float3(Convert.ToSingle(sl[0]), Convert.ToSingle(sl[1]), Convert.ToSingle(sl[2]));

      return Mat4.TRS(t, r, s);
    }

    private static float LinearToSrgb(float c) {
      if (c <= 0f) return 0f;
      if (c >= 1f) return 1f;
      return c <= 0.0031308f ? c * 12.92f : (float)(1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055);
    }

    private static Float3 Normalize3(Float3 v) {
      float len = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
      return len > 1e-6f ? new Float3(v.X / len, v.Y / len, v.Z / len) : new Float3(0f, 1f, 0f);
    }

    internal static float EstimateMass(RawBounds b) {
      float vol = Math.Max(0.25f, (b.Extents.X * 2f) * (b.Extents.Y * 2f) * (b.Extents.Z * 2f));
      return Math.Max(500f, Math.Min(15000f, vol * 25f));
    }

    internal readonly struct Mat4 {
      // Row-major storage: M[row * 4 + col]
      public readonly float M00, M01, M02, M03;
      public readonly float M10, M11, M12, M13;
      public readonly float M20, M21, M22, M23;
      public readonly float M30, M31, M32, M33;

      public static readonly Mat4 Identity = new Mat4(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);

      public Mat4(
        float m00, float m01, float m02, float m03,
        float m10, float m11, float m12, float m13,
        float m20, float m21, float m22, float m23,
        float m30, float m31, float m32, float m33) {
        M00 = m00; M01 = m01; M02 = m02; M03 = m03;
        M10 = m10; M11 = m11; M12 = m12; M13 = m13;
        M20 = m20; M21 = m21; M22 = m22; M23 = m23;
        M30 = m30; M31 = m31; M32 = m32; M33 = m33;
      }

      public static Mat4 FromColumnMajor(float[] c) => new Mat4(
        c[0], c[4], c[8], c[12],
        c[1], c[5], c[9], c[13],
        c[2], c[6], c[10], c[14],
        c[3], c[7], c[11], c[15]);

      public static Mat4 TRS(Float3 t, Float4 q, Float3 s) {
        float x = q.X, y = q.Y, z = q.Z, w = q.W;
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;
        float wx = w * x, wy = w * y, wz = w * z;

        float r00 = 1f - 2f * (yy + zz);
        float r01 = 2f * (xy - wz);
        float r02 = 2f * (xz + wy);
        float r10 = 2f * (xy + wz);
        float r11 = 1f - 2f * (xx + zz);
        float r12 = 2f * (yz - wx);
        float r20 = 2f * (xz - wy);
        float r21 = 2f * (yz + wx);
        float r22 = 1f - 2f * (xx + yy);

        return new Mat4(
          r00 * s.X, r01 * s.Y, r02 * s.Z, t.X,
          r10 * s.X, r11 * s.Y, r12 * s.Z, t.Y,
          r20 * s.X, r21 * s.Y, r22 * s.Z, t.Z,
          0f, 0f, 0f, 1f);
      }

      public static Mat4 Multiply(Mat4 a, Mat4 b) => new Mat4(
        a.M00 * b.M00 + a.M01 * b.M10 + a.M02 * b.M20 + a.M03 * b.M30,
        a.M00 * b.M01 + a.M01 * b.M11 + a.M02 * b.M21 + a.M03 * b.M31,
        a.M00 * b.M02 + a.M01 * b.M12 + a.M02 * b.M22 + a.M03 * b.M32,
        a.M00 * b.M03 + a.M01 * b.M13 + a.M02 * b.M23 + a.M03 * b.M33,

        a.M10 * b.M00 + a.M11 * b.M10 + a.M12 * b.M20 + a.M13 * b.M30,
        a.M10 * b.M01 + a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
        a.M10 * b.M02 + a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
        a.M10 * b.M03 + a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,

        a.M20 * b.M00 + a.M21 * b.M10 + a.M22 * b.M20 + a.M23 * b.M30,
        a.M20 * b.M01 + a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
        a.M20 * b.M02 + a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
        a.M20 * b.M03 + a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,

        a.M30 * b.M00 + a.M31 * b.M10 + a.M32 * b.M20 + a.M33 * b.M30,
        a.M30 * b.M01 + a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
        a.M30 * b.M02 + a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
        a.M30 * b.M03 + a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);

      public Float3 TransformPoint(Float3 p) => new Float3(
        M00 * p.X + M01 * p.Y + M02 * p.Z + M03,
        M10 * p.X + M11 * p.Y + M12 * p.Z + M13,
        M20 * p.X + M21 * p.Y + M22 * p.Z + M23);

      public Float3 TransformDirection(Float3 d) => new Float3(
        M00 * d.X + M01 * d.Y + M02 * d.Z,
        M10 * d.X + M11 * d.Y + M12 * d.Z,
        M20 * d.X + M21 * d.Y + M22 * d.Z);

      /// <summary>
      /// Transforms a surface normal using the 3x3 cofactor (inverse-transpose * det) matrix,
      /// preserving perpendicularity under non-uniform scale and matching handedness when mirrored.
      /// </summary>
      public Float3 TransformNormal(Float3 n) {
        float c00 = M11 * M22 - M12 * M21;
        float c01 = -(M10 * M22 - M12 * M20);
        float c02 = M10 * M21 - M11 * M20;
        float c10 = -(M01 * M22 - M02 * M21);
        float c11 = M00 * M22 - M02 * M20;
        float c12 = -(M00 * M21 - M01 * M20);
        float c20 = M01 * M12 - M02 * M11;
        float c21 = -(M00 * M12 - M02 * M10);
        float c22 = M00 * M11 - M01 * M10;
        float sign = Determinant3x3() < 0f ? -1f : 1f;
        return new Float3(
          (c00 * n.X + c01 * n.Y + c02 * n.Z) * sign,
          (c10 * n.X + c11 * n.Y + c12 * n.Z) * sign,
          (c20 * n.X + c21 * n.Y + c22 * n.Z) * sign);
      }

      public float Determinant3x3() =>
        M00 * (M11 * M22 - M12 * M21)
        - M01 * (M10 * M22 - M12 * M20)
        + M02 * (M10 * M21 - M11 * M20);
    }
  }
}
