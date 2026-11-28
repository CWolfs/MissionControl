using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BTAssetReader.Core;
using BTAssetReader.Core.CustomModels;
using BTAssetReader.Core.Destruction;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;
using Newtonsoft.Json.Linq;

namespace BTAssetReader.Tests {
  internal static class Program {
    private static int _passed;
    private static int _failed;

    private static void Assert(bool condition, string message) {
      if (condition) {
        _passed++;
      } else {
        _failed++;
        Console.Error.WriteLine($"  FAIL: {message}");
      }
    }

    private static int Main(string[] args) {
      string btData = args.Length > 0
        ? args[0]
        : @"D:\Program Files (x86)\Steam\steamapps\common\BATTLETECH\BattleTech_Data";

      if (!Directory.Exists(btData)) {
        Console.Error.WriteLine($"BattleTech_Data not found at '{btData}'. Pass path as first argument.");
        return 2;
      }

      string cachePath = Path.Combine(Path.GetTempPath(), "bt-asset-reader-test-index.bin");
      if (File.Exists(cachePath)) File.Delete(cachePath);

      Console.WriteLine($"=== BTAssetReader.Tests against '{btData}' ===");

      // 1. Cold index build
      var sw = Stopwatch.StartNew();
      var index = GameAssetIndex.BuildOrLoad(btData, cachePath, Console.WriteLine);
      sw.Stop();
      Console.WriteLine($"Cold index build: {sw.ElapsedMilliseconds} ms");
      Assert(index.Files.Count >= 90, $"Expected >=90 files, got {index.Files.Count}");
      Assert(index.MeshesByName.Count > 30000, $"Expected >30000 unique meshes, got {index.MeshesByName.Count}");
      Assert(index.MaterialsByName.Count > 1000, $"Expected >1000 unique materials, got {index.MaterialsByName.Count}");
      Assert(index.TexturesByName.Count > 3000, $"Expected >3000 unique textures, got {index.TexturesByName.Count}");
      Assert(File.Exists(cachePath), "Index cache file should have been written");

      // 2. Warm cache load
      sw.Restart();
      var cached = GameAssetIndex.BuildOrLoad(btData, cachePath, Console.WriteLine);
      sw.Stop();
      Console.WriteLine($"Warm cache load: {sw.ElapsedMilliseconds} ms");
      Assert(cached.MeshesByName.Count == index.MeshesByName.Count, "Cached mesh count should match fresh scan");
      Assert(cached.MaterialsByName.Count == index.MaterialsByName.Count, "Cached material count should match fresh scan");
      Assert(cached.ShaderNamesByRef.Count == index.ShaderNamesByRef.Count, "Cached shader count should match fresh scan");

      // 3. Decode ALL _LOD0, _COL, _glass, and *shell* meshes across classic sharedassets (<= sharedassets83)
      int meshesChecked = 0;
      int meshErrors = 0;
      foreach (var kv in cached.MeshesByName) {
        string name = kv.Key;
        if (!name.EndsWith("_LOD0") && !name.EndsWith("_COL") && !name.EndsWith("_glass") && name.IndexOf("shell", StringComparison.OrdinalIgnoreCase) < 0) {
          continue;
        }
        AssetRef aref = kv.Value[0];
        try {
          RawMeshData mesh = cached.DecodeMesh(aref);
          if (mesh == null || mesh.Vertices == null || mesh.Vertices.Length == 0 || mesh.SubMeshes == null || mesh.SubMeshes.Length == 0) {
            meshErrors++;
            if (meshErrors <= 5) Console.Error.WriteLine($"  Empty mesh decoded: {name} {aref}");
          } else {
            // Verify all triangle indices are within vertex bounds
            for (int s = 0; s < mesh.SubMeshIndices.Length; s++) {
              int[] idx = mesh.SubMeshIndices[s];
              for (int i = 0; i < idx.Length; i++) {
                if (idx[i] < 0 || idx[i] >= mesh.VertexCount) {
                  throw new InvalidDataException($"Index {idx[i]} out of bounds [0..{mesh.VertexCount}) in submesh {s}");
                }
              }
            }
          }
          meshesChecked++;
        } catch (Exception ex) {
          meshErrors++;
          if (meshErrors <= 10) Console.Error.WriteLine($"  Mesh decode error '{name}' {aref}: {ex.Message}");
        }
      }
      Console.WriteLine($"Decoded {meshesChecked} LOD0/COL/glass/shell meshes with {meshErrors} errors.");
      Assert(meshesChecked > 900, $"Expected >900 key building/prop meshes, got {meshesChecked}");
      Assert(meshErrors == 0, $"Expected 0 mesh decode errors, got {meshErrors}");

      // 4. Decode ALL envMat* materials and their referenced Texture2Ds
      int matsChecked = 0;
      int matErrors = 0;
      int texChecked = 0;
      int texErrors = 0;
      var decodedTextures = new HashSet<AssetRef>();
      foreach (var kv in cached.MaterialsByName) {
        if (!kv.Key.StartsWith("envMat", StringComparison.OrdinalIgnoreCase)) continue;
        AssetRef mref = kv.Value[0];
        try {
          RawMaterialData mat = cached.DecodeMaterial(mref);
          if (mat == null || string.IsNullOrEmpty(mat.Name)) {
            matErrors++;
            continue;
          }
          matsChecked++;
          foreach (var te in mat.TexEnvs) {
            if (te.TextureRef.IsNull || !decodedTextures.Add(te.TextureRef)) continue;
            try {
              RawTexture2DData tex = cached.DecodeTexture2D(te.TextureRef);
              if (tex == null || tex.Width <= 0 || tex.Height <= 0 || tex.RawBytes == null || tex.RawBytes.Length == 0) {
                texErrors++;
                if (texErrors <= 5) Console.Error.WriteLine($"  Empty texture decoded: {te.TextureRef} on mat '{mat.Name}'");
              }
              texChecked++;
            } catch (Exception ex) {
              texErrors++;
              if (texErrors <= 5) Console.Error.WriteLine($"  Texture decode error {te.TextureRef}: {ex.Message}");
            }
          }
        } catch (Exception ex) {
          matErrors++;
          if (matErrors <= 5) Console.Error.WriteLine($"  Material decode error '{kv.Key}': {ex.Message}");
        }
      }
      Console.WriteLine($"Decoded {matsChecked} envMat materials ({matErrors} errors) and {texChecked} unique referenced textures ({texErrors} errors).");
      Assert(matsChecked > 200, $"Expected >200 envMat materials, got {matsChecked}");
      Assert(matErrors == 0, $"Expected 0 material errors, got {matErrors}");
      Assert(texChecked > 200, $"Expected >200 referenced textures, got {texChecked}");
      Assert(texErrors == 0, $"Expected 0 texture errors, got {texErrors}");

      // 5. Verify specific cross-map material (envMatStct_redPaintedMetal_generic from sharedassets25)
      AssetRef redMatRef = cached.FindMaterial("envMatStct_redPaintedMetal_generic");
      Assert(!redMatRef.IsNull, "Should find envMatStct_redPaintedMetal_generic");
      if (!redMatRef.IsNull) {
        RawMaterialData redMat = cached.DecodeMaterial(redMatRef);
        Console.WriteLine($"Verified redPaintedMetal: shader='{redMat.ShaderName}', texEnvs={redMat.TexEnvs.Count}, keywords=[{string.Join(",", redMat.ShaderKeywords)}]");
        Assert(redMat.ShaderName == "BattleTech Standard", $"Expected BattleTech Standard shader, got '{redMat.ShaderName}'");
        Assert(redMat.TexEnvs.Any(t => t.PropertyName == "_MainTex" && !t.TextureRef.IsNull), "redPaintedMetal should have _MainTex");
      }

      // 6. Verify vanilla split prefab hierarchy decoding
      var splitPrefabs = cached.GameObjectsByName.Keys.Where(k => k.EndsWith("_split", StringComparison.OrdinalIgnoreCase)).Take(10).ToList();
      Console.WriteLine($"Indexed {cached.GameObjectsByName.Count} split/shell GameObject roots. Sample: {string.Join(", ", splitPrefabs.Take(5))}");
      int splitRootsDecoded = 0;
      foreach (string sp in splitPrefabs) {
        AssetRef sref = cached.FindGameObject(sp);
        RawGameObjectNode node = cached.DecodeHierarchy(sref);
        if (node != null && (node.Children.Count > 0 || !node.MeshRef.IsNull)) {
          splitRootsDecoded++;
        }
      }
      Assert(splitRootsDecoded > 0, $"Expected at least 1 split prefab hierarchy decoded, got {splitRootsDecoded}");

      // 7. Validate EVERY GameData model.json in MissionControl/props/models resolves its meshes, shells, splits, and materials
      string mcPropsModelsDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "props", "models"));
      if (Directory.Exists(mcPropsModelsDir)) {
        int gameDataModels = 0;
        int missingMeshes = 0;
        int missingMaterials = 0;
        int missingShells = 0;
        int missingSplits = 0;
        foreach (string modelJsonPath in Directory.GetFiles(mcPropsModelsDir, "model.json", SearchOption.AllDirectories)) {
          JObject jo = JObject.Parse(File.ReadAllText(modelJsonPath));
          string assetSource = jo["AssetSource"]?.ToString() ?? "Auto";
          bool inBundle = jo["IsMeshInBundle"]?.Value<bool>() ?? false;
          string completeBundle = jo["CompleteBundle"]?.ToString();
          if (!string.IsNullOrEmpty(completeBundle) || string.Equals(assetSource, "Bundle", StringComparison.OrdinalIgnoreCase) || (inBundle && !string.Equals(assetSource, "GameData", StringComparison.OrdinalIgnoreCase))) {
            continue;
          }

          gameDataModels++;
          string key = jo["Key"]?.ToString();
          string meshName = jo["Mesh"]?.ToString();
          string srcFile = jo["SourceAssetFile"]?.ToString();
          var pids = jo["MeshPathIds"] as JObject;
          long lod0Pid = pids?["LOD0"]?.Value<long>() ?? 0;
          long flimsyPid = pids?["Flimsy"]?.Value<long>() ?? 0;

          AssetRef primary = cached.FindMesh($"{meshName}_LOD0", srcFile, lod0Pid);
          if (primary.IsNull) primary = cached.FindMesh(meshName, srcFile, flimsyPid);
          if (primary.IsNull) {
            missingMeshes++;
            if (missingMeshes <= 10) Console.Error.WriteLine($"  Missing primary mesh for '{key}': '{meshName}' (src='{srcFile}')");
          }

          if (jo["HasCustomShell"]?.Value<bool>() == true) {
            long shellPid = pids?["Shell"]?.Value<long>() ?? 0;
            AssetRef shellRef = cached.FindMesh($"{meshName}_shell", srcFile, shellPid);
            if (shellRef.IsNull) {
              missingShells++;
              if (missingShells <= 10) Console.Error.WriteLine($"  Missing shell mesh for '{key}': '{meshName}_shell'");
            }
          }

          string splitPrefab = jo["SplitPrefabName"]?.ToString();
          if (!string.IsNullOrEmpty(splitPrefab)) {
            AssetRef spRef = cached.FindGameObject(splitPrefab, srcFile);
            if (spRef.IsNull) {
              missingSplits++;
              if (missingSplits <= 10) Console.Error.WriteLine($"  Missing split prefab for '{key}': '{splitPrefab}'");
            }
          }

          if (jo["Materials"] is JArray mats) {
            foreach (JObject mObj in mats.Children<JObject>()) {
              string mn = mObj["Name"]?.ToString();
              if (!string.IsNullOrEmpty(mn) && cached.FindMaterial(mn).IsNull) {
                missingMaterials++;
                if (missingMaterials <= 10) Console.Error.WriteLine($"  Missing material for '{key}': '{mn}'");
              }
            }
          }
        }
        Console.WriteLine($"Validated {gameDataModels} GameData model.json files: missingMeshes={missingMeshes}, missingShells={missingShells}, missingSplits={missingSplits}, missingMaterials={missingMaterials}");
        Assert(gameDataModels >= 450, $"Expected >=450 GameData models, got {gameDataModels}");
        Assert(missingMeshes == 0, $"Expected 0 missing meshes across all GameData model.json files, got {missingMeshes}");
        Assert(missingShells == 0, $"Expected 0 missing shells across all GameData model.json files, got {missingShells}");
        Assert(missingSplits == 0, $"Expected 0 missing split prefabs across all GameData model.json files, got {missingSplits}");
        Assert(missingMaterials == 0, $"Expected 0 missing materials across all GameData model.json files, got {missingMaterials}");
      }

      if (File.Exists(cachePath)) File.Delete(cachePath);

      // 8. Test custom .glb, .obj, MeshNormaliser, PlaneSliceFracturer, AutoStumpShellGenerator, and MechBundleLoader
      RunCustomPropCoreTests(btData);

      Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed ===");
      return _failed == 0 ? 0 : 1;
    }

    private static void RunCustomPropCoreTests(string dataDir) {
      Console.WriteLine("\n--- Testing Custom Prop Core (.glb, .obj, Normaliser, Fracturer, Auto-Stump) ---");
      string tmpDir = Path.Combine(Path.GetTempPath(), "bt-custom-prop-tests-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(tmpDir);

      try {
        // 8a. OBJ + MTL load with LOD0 + _COL + quad triangulation
        string mtlPath = Path.Combine(tmpDir, "bunker.mtl");
        File.WriteAllText(mtlPath, "newmtl WallMat\nKd 0.6 0.5 0.4\nPm 0.2\nPr 0.8\n");
        string objPath = Path.Combine(tmpDir, "bunker.obj");
        File.WriteAllText(objPath, @"
mtllib bunker.mtl
o BunkerBody
usemtl WallMat
v -2 0 -1
v 2 0 -1
v 2 4 -1
v -2 4 -1
v -2 0 1
v 2 0 1
v 2 4 1
v -2 4 1
f 1 4 3 2
f 5 6 7 8
f 5 8 4 1
f 2 3 7 6
f 4 8 7 3
f 1 2 6 5
o BunkerBody_COL
f 1 4 3 2
");
        CustomModelBundle objBundle = CustomModelLoader.Load(objPath);
        Assert(objBundle != null && objBundle.Lod0Mesh != null, "OBJ should produce LOD0 mesh");
        Assert(objBundle.ColMesh != null, "OBJ should extract _COL group");
        Assert(objBundle.Materials.Count == 1 && objBundle.Materials[0].Name == "WallMat", "OBJ should parse MTL material");
        Assert(HasOutwardUnityWinding(objBundle.Lod0Mesh), "OBJ mesh should have outward Unity CW winding (dot(cross(e1,e2), outward) > 0)");

        // 8b. GLB binary load with KHR_lights_punctual spotlight + PBR material
        string glbPath = Path.Combine(tmpDir, "tower.glb");
        WriteMinimalTestGlb(glbPath);
        CustomModelBundle glbBundle = CustomModelLoader.Load(glbPath);
        Assert(glbBundle != null && glbBundle.Lod0Mesh != null && glbBundle.Lod0Mesh.VertexCount == 8, $"GLB should load 8-vertex cube, got {glbBundle?.Lod0Mesh?.VertexCount}");
        Assert(glbBundle.Materials.Count == 1 && glbBundle.Materials[0].Name == "TowerMetal", "GLB should load embedded PBR material");
        Assert(glbBundle.Lights.Count == 1 && glbBundle.Lights[0].Type == "Spot", $"GLB should load KHR_lights_punctual spotlight, got {glbBundle.Lights.Count}");
        Assert(HasOutwardUnityWinding(glbBundle.Lod0Mesh), "GLB mesh should have outward Unity CW winding (dot(cross(e1,e2), outward) > 0)");

        // 8b2. GLB with EXT_texture_webp texture extension
        string webpGlbPath = Path.Combine(tmpDir, "webp_ext.glb");
        WriteMinimalWebpExtGlb(webpGlbPath);
        CustomModelBundle webpBundle = CustomModelLoader.Load(webpGlbPath);
        Assert(webpBundle != null && webpBundle.Materials.Count == 1 && webpBundle.Materials[0].BaseColorImageBytes != null && webpBundle.Materials[0].BaseColorImageBytes.Length >= 12,
          "GLB with EXT_texture_webp should extract image bytes via extension source");
        Assert(webpBundle.Materials[0].BaseColorImageMime == "image/webp",
          $"EXT_texture_webp mime should be image/webp, got '{webpBundle.Materials[0].BaseColorImageMime}'");

        // 8c. MeshNormaliser: rotate 90 X, TargetHeight = 12m, PivotMode = Bottom
        MeshNormaliser.Normalize(glbBundle, new MeshTransformSettings {
          RotationOffset = new Float3(90f, 0f, 0f),
          TargetHeight = 12f,
          PivotMode = CorePivotMode.Bottom
        });
        RawBounds b = glbBundle.Lod0Mesh.LocalAabb;
        float finalHeight = b.Extents.Y * 2f;
        float minY = b.Center.Y - b.Extents.Y;
        Assert(Math.Abs(finalHeight - 12f) < 0.05f, $"Normalised height should be 12m, got {finalHeight:F3}");
        Assert(Math.Abs(minY) < 0.05f, $"Bottom pivot minY should be 0, got {minY:F3}");
        Assert(Math.Abs(b.Center.X) < 0.05f && Math.Abs(b.Center.Z) < 0.05f, $"Bottom pivot X/Z should be centred, got ({b.Center.X:F3}, {b.Center.Z:F3})");

        // 8d. PlaneSliceFracturer: 8 deterministic chunks with valid indices + interior caps
        var chunksA = PlaneSliceFracturer.Fracture(glbBundle.Lod0Mesh, targetPieces: 8, seed: 42);
        var chunksB = PlaneSliceFracturer.Fracture(glbBundle.Lod0Mesh, targetPieces: 8, seed: 42);
        Assert(chunksA.Count == 8, $"Expected 8 fractured chunks, got {chunksA.Count}");
        Assert(chunksB.Count == chunksA.Count && chunksA[0].Mesh.VertexCount == chunksB[0].Mesh.VertexCount, "PlaneSliceFracturer should be deterministic for same seed");
        bool allValid = true;
        bool anyInteriorCap = false;
        foreach (var ch in chunksA) {
          if (ch.InteriorSubMeshIndex >= 0) anyInteriorCap = true;
          foreach (int[] sub in ch.Mesh.SubMeshIndices) {
            foreach (int idx in sub) {
              if (idx < 0 || idx >= ch.Mesh.VertexCount) allValid = false;
            }
          }
        }
        Assert(allValid, "All fractured chunk triangle indices should be within bounds");
        Assert(anyInteriorCap, "Fractured chunks should include capped interior cross-section submesh");

        // 8e. AutoStumpShellGenerator: jagged low ruined stump with interior cap
        RawMeshData stump = AutoStumpShellGenerator.Generate(glbBundle.Lod0Mesh, stumpHeightFraction: 0.25f, seed: 42);
        Assert(stump != null && stump.VertexCount > 0, "AutoStumpShellGenerator should produce non-empty shell mesh");
        float stumpHeight = stump.LocalAabb.Extents.Y * 2f;
        Assert(stumpHeight > 0.5f && stumpHeight < 6.0f, $"Auto-stump height should be lower than 12m original (got {stumpHeight:F2}m)");
        Assert(stump.SubMeshIndices.Length >= 2, "Auto-stump shell should include interior rubble cap submesh");
        // 8f. Real BattleTech Mech bundle loader (UnityFS + LZ4 + TEST_MechScaleMultiplier)
        string atlasBundlePath = MechBundleLoader.ResolveMechBundlePath(dataDir, "chrprfmech_atlasbase-001");
        if (atlasBundlePath != null) {
          RawMechReference atlas = MechBundleLoader.Load(dataDir, "chrprfmech_atlasbase-001", loadTextures: true);
          Assert(atlas != null && atlas.Mesh != null && atlas.Mesh.VertexCount > 5000, $"Atlas mech bundle should load combined mesh, got {atlas?.Mesh?.VertexCount}");
          Assert(atlas.InGameHeight > 20f && atlas.InGameHeight < 28f, $"Atlas in-game height should be ~24m (got {atlas.InGameHeight:F2}m)");
          Assert(atlas.AlbedoTexture != null && atlas.AlbedoTexture.Width > 0, "Atlas mech bundle should decode _MainTex albedo texture");
          Console.WriteLine($"Mech bundle verified: Atlas raw={atlas.RawHeight:F2}m, in-game(x{atlas.ScaleMultiplier:F2})={atlas.InGameHeight:F2}m, verts={atlas.Mesh.VertexCount}, tex={atlas.AlbedoTexture.Width}x{atlas.AlbedoTexture.Height}.");
        }
        // 8g. PropBundleLoader (LZMA AssetBundle prop + GameData vanilla prop)
        string mcArenaBundle = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "props", "models", "Arenas", "ArenaLargeOneSideOpenA", "prop-model-arena-large-one-side-open-a-bundle");
        if (File.Exists(mcArenaBundle)) {
          CustomModelBundle arena = PropBundleLoader.LoadFromBundle(mcArenaBundle, "arenaLargeOneSideOpenA");
          Assert(arena != null && arena.Lod0Mesh != null && arena.Lod0Mesh.SubMeshIndices.Length == 7, $"LZMA prop bundle should load 7-submesh arena LOD0, got {arena?.Lod0Mesh?.SubMeshIndices?.Length}");
          Assert(arena.ColMesh != null && arena.ColMesh.VertexCount > 1000, $"LZMA prop bundle should load COL mesh, got {arena?.ColMesh?.VertexCount}");
          Console.WriteLine($"Prop bundle verified: ArenaLargeOneSideOpenA LOD0={arena.Lod0Mesh.VertexCount} verts (7 submeshes), COL={arena.ColMesh.VertexCount} verts.");
        }
        Console.WriteLine($"Custom prop core verified: GLB/OBJ loaded, normalized to {finalHeight:F1}m, fractured into {chunksA.Count} chunks, auto-stump height={stumpHeight:F2}m.");
      } finally {
        try { Directory.Delete(tmpDir, true); } catch { }
      }
    }

    private static bool HasOutwardUnityWinding(RawMeshData mesh) {
      Float3 center = mesh.LocalAabb.Center;
      double totalDot = 0;
      foreach (int[] sub in mesh.SubMeshIndices) {
        for (int i = 0; i + 2 < sub.Length; i += 3) {
          Float3 a = mesh.Vertices[sub[i]];
          Float3 b = mesh.Vertices[sub[i + 1]];
          Float3 c = mesh.Vertices[sub[i + 2]];
          float e1x = b.X - a.X, e1y = b.Y - a.Y, e1z = b.Z - a.Z;
          float e2x = c.X - a.X, e2y = c.Y - a.Y, e2z = c.Z - a.Z;
          float cx = e1y * e2z - e1z * e2y;
          float cy = e1z * e2x - e1x * e2z;
          float cz = e1x * e2y - e1y * e2x;
          float mx = (a.X + b.X + c.X) / 3f - center.X;
          float my = (a.Y + b.Y + c.Y) / 3f - center.Y;
          float mz = (a.Z + b.Z + c.Z) / 3f - center.Z;
          totalDot += cx * mx + cy * my + cz * mz;
        }
      }
      return totalDot > 0.0;
    }

    private static void WriteMinimalTestGlb(string path) {
      // 8-vertex 2x4x2 rectangular prism (Y from 0..4, X from -1..1, Z from -1..1) + 36 outward RH CCW ushort indices
      float[] positions = {
        -1,0,-1,  1,0,-1,  1,4,-1, -1,4,-1,
        -1,0, 1,  1,0, 1,  1,4, 1, -1,4, 1
      };
      ushort[] indices = {
        0,3,2, 0,2,1, // -Z face (outward CCW)
        4,5,6, 4,6,7, // +Z face
        4,7,3, 4,3,0, // -X face
        1,2,6, 1,6,5, // +X face
        3,7,6, 3,6,2, // +Y face
        0,1,5, 0,5,4  // -Y face
      };

      byte[] binBytes;
      int posLen = positions.Length * 4;
      int idxLen = indices.Length * 2;
      using (var ms = new MemoryStream())
      using (var bw = new BinaryWriter(ms)) {
        foreach (float f in positions) bw.Write(f);
        foreach (ushort u in indices) bw.Write(u);
        while (ms.Length % 4 != 0) bw.Write((byte)0);
        binBytes = ms.ToArray();
      }

      string json = $@"{{
        ""asset"": {{ ""version"": ""2.0"" }},
        ""extensionsUsed"": [""KHR_lights_punctual""],
        ""extensions"": {{
          ""KHR_lights_punctual"": {{
            ""lights"": [{{ ""name"": ""RoofSpot"", ""type"": ""spot"", ""color"": [1, 0.9, 0.7], ""intensity"": 2.0, ""range"": 20.0, ""spot"": {{ ""innerConeAngle"": 0.4, ""outerConeAngle"": 0.8 }} }}]
          }}
        }},
        ""scenes"": [{{ ""nodes"": [0, 1] }}],
        ""scene"": 0,
        ""nodes"": [
          {{ ""name"": ""TowerMesh"", ""mesh"": 0 }},
          {{ ""name"": ""SpotNode"", ""translation"": [0, 3.8, 1.1], ""extensions"": {{ ""KHR_lights_punctual"": {{ ""light"": 0 }} }} }}
        ],
        ""materials"": [{{ ""name"": ""TowerMetal"", ""pbrMetallicRoughness"": {{ ""baseColorFactor"": [0.7, 0.7, 0.75, 1.0], ""metallicFactor"": 0.6, ""roughnessFactor"": 0.4 }} }}],
        ""meshes"": [{{ ""name"": ""Tower"", ""primitives"": [{{ ""attributes"": {{ ""POSITION"": 0 }}, ""indices"": 1, ""material"": 0 }}] }}],
        ""buffers"": [{{ ""byteLength"": {binBytes.Length} }}],
        ""bufferViews"": [
          {{ ""buffer"": 0, ""byteOffset"": 0, ""byteLength"": {posLen} }},
          {{ ""buffer"": 0, ""byteOffset"": {posLen}, ""byteLength"": {idxLen} }}
        ],
        ""accessors"": [
          {{ ""bufferView"": 0, ""componentType"": 5126, ""count"": 8, ""type"": ""VEC3"" }},
          {{ ""bufferView"": 1, ""componentType"": 5123, ""count"": 36, ""type"": ""SCALAR"" }}
        ]
      }}";

      byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
      int jsonPad = (4 - (jsonBytes.Length % 4)) % 4;
      int jsonChunkLen = jsonBytes.Length + jsonPad;
      int totalLen = 12 + 8 + jsonChunkLen + 8 + binBytes.Length;

      using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
      using (var bw = new BinaryWriter(fs)) {
        bw.Write(0x46546C67u); // glTF
        bw.Write(2u);
        bw.Write((uint)totalLen);

        bw.Write((uint)jsonChunkLen);
        bw.Write(0x4E4F534Au); // JSON
        bw.Write(jsonBytes);
        for (int i = 0; i < jsonPad; i++) bw.Write((byte)' ');

        bw.Write((uint)binBytes.Length);
        bw.Write(0x004E4942u); // BIN
        bw.Write(binBytes);
      }
    }

    private static void WriteMinimalWebpExtGlb(string path) {
      float[] positions = { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
      ushort[] indices = { 0, 1, 2 };
      // Minimal RIFF....WEBP payload header for loader extraction verification
      byte[] fakeWebp = { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 4, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P' };

      byte[] binBytes;
      int posLen = positions.Length * 4;
      int idxLen = indices.Length * 2;
      int idxPad = (4 - (idxLen % 4)) % 4;
      int imgOff = posLen + idxLen + idxPad;
      using (var ms = new MemoryStream())
      using (var bw = new BinaryWriter(ms)) {
        foreach (float f in positions) bw.Write(f);
        foreach (ushort u in indices) bw.Write(u);
        for (int i = 0; i < idxPad; i++) bw.Write((byte)0);
        bw.Write(fakeWebp);
        while (ms.Length % 4 != 0) bw.Write((byte)0);
        binBytes = ms.ToArray();
      }

      string json = $@"{{
        ""asset"": {{ ""version"": ""2.0"" }},
        ""extensionsUsed"": [""EXT_texture_webp""],
        ""extensionsRequired"": [""EXT_texture_webp""],
        ""scenes"": [{{ ""nodes"": [0] }}],
        ""scene"": 0,
        ""nodes"": [{{ ""name"": ""Tri"", ""mesh"": 0 }}],
        ""images"": [{{ ""bufferView"": 2, ""mimeType"": ""image/webp"" }}],
        ""textures"": [{{ ""extensions"": {{ ""EXT_texture_webp"": {{ ""source"": 0 }} }} }}],
        ""materials"": [{{ ""name"": ""WebpMat"", ""pbrMetallicRoughness"": {{ ""baseColorTexture"": {{ ""index"": 0 }} }} }}],
        ""meshes"": [{{ ""name"": ""Tri"", ""primitives"": [{{ ""attributes"": {{ ""POSITION"": 0 }}, ""indices"": 1, ""material"": 0 }}] }}],
        ""buffers"": [{{ ""byteLength"": {binBytes.Length} }}],
        ""bufferViews"": [
          {{ ""buffer"": 0, ""byteOffset"": 0, ""byteLength"": {posLen} }},
          {{ ""buffer"": 0, ""byteOffset"": {posLen}, ""byteLength"": {idxLen} }},
          {{ ""buffer"": 0, ""byteOffset"": {imgOff}, ""byteLength"": {fakeWebp.Length} }}
        ],
        ""accessors"": [
          {{ ""bufferView"": 0, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC3"" }},
          {{ ""bufferView"": 1, ""componentType"": 5123, ""count"": 3, ""type"": ""SCALAR"" }}
        ]
      }}";

      byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
      int jsonPad = (4 - (jsonBytes.Length % 4)) % 4;
      int jsonChunkLen = jsonBytes.Length + jsonPad;
      int totalLen = 12 + 8 + jsonChunkLen + 8 + binBytes.Length;

      using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
      using (var bw = new BinaryWriter(fs)) {
        bw.Write(0x46546C67u);
        bw.Write(2u);
        bw.Write((uint)totalLen);
        bw.Write((uint)jsonChunkLen);
        bw.Write(0x4E4F534Au);
        bw.Write(jsonBytes);
        for (int i = 0; i < jsonPad; i++) bw.Write((byte)' ');
        bw.Write((uint)binBytes.Length);
        bw.Write(0x004E4942u);
        bw.Write(binBytes);
      }
    }
  }
}
