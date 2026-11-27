using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BTAssetReader.Core;
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

      Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed ===");
      return _failed == 0 ? 0 : 1;
    }
  }
}
