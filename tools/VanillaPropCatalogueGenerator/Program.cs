using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BTAssetReader.Core;
using BTAssetReader.Core.Decoders;
using BTAssetReader.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VanillaPropCatalogueGenerator {
  internal sealed class DiscoveredModel {
    public string MeshBaseName;
    public bool IsLodBased;
    public AssetRef Lod0Ref;
    public AssetRef Lod1Ref;
    public AssetRef Lod2Ref;
    public AssetRef ColRef;
    public AssetRef GlassRef;
    public AssetRef ShellRef;
    public AssetRef ShellColRef;
    public string SplitPrefabName;
    public string SourceAssetFile;
    public List<string> Materials = new List<string>();
    public string GlassMaterial;
    public Float3 BoundsSize;
    public int SubMeshCount;
    public int SeenCount;
    public string Role; // "Building", "Structure", "Destructible"
    public string Category;
    public string Key;
    public string FriendlyName;
  }

  internal static class Program {
    private static readonly Regex LevelFileRegex = new Regex(@"^level(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SharedAssetsRegex = new Regex(@"^sharedassets(\d+)\.assets$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TrailingDigitsRegex = new Regex(@"(?:_?\d+)$", RegexOptions.Compiled);
    private static readonly Regex CellOrChunkRegex = new Regex(@"(?:_cell_\d+|_chunk_?\d+|_piece_?\d+|_part_?\d+|strut\d+|polySurface\d+|pCube\d+|pPlane\d+|pCylinder\d+|mesh\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> ExcludedPartNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
      "arm", "leg", "lid", "cap", "bolt", "hinge", "rivet", "trim", "edge", "top", "bottom", "side", "back", "front",
      "frame", "pane", "handle", "knob", "hinges", "bracket", "clip", "hook", "pin", "rod", "bar", "plate", "strip",
      "strut", "wire", "cables", "cord", "tube", "vent", "grate", "grille", "slat", "rung", "step", "steps",
      "awning", "doorframe", "window", "windows", "glass", "roof", "floor", "ceiling", "base", "stand", "leg"
    };

    private static readonly string[] ExcludedNameSubstrings = {
      "combined mesh", "decal mesh", "region mesh", "generated water", "terrainchunk", "terrain_",
      "outerskirt", "cityskirt", "urbanskirt", "skirt", "waterplane", "skybox",
      "chrmdl", "chrprf", "vfx", "uix", "navmesh", "lightmap", "collisionplane",
      "tree", "bush", "grass", "shrub", "fern", "flower", "foliage", "ivy", "vine", "moss", "weed", "leaf", "leaves", "branch", "trunk",
      "rock", "boulder", "cliff", "stone", "crater", "pebble", "gravel", "rubblepile", "debris", "dirt", "mud", "sand",
      "cactus", "palm", "pine", "elm", "alder", "yew", "birch", "oak", "willow", "spruce", "cedar", "maple", "cypress", "stump", "fallen",
      "snow", "icicle", "glacier", "lava", "magma", "cloud", "bird", "fish", "animal",
      "_fract", "_split", "_cell", "_meshpart", "meshpart",
      "leopardpart", "fallingpiece", "door_front_", "door_rear_", "_dst_nuv",
      "reference_mesh", "exporttransform", "transform1", "polySurface", "pCube", "pPlane", "pCylinder", "pSphere", "pCone", "default"
    };

    private static readonly string[] ExcludedExactNames = {
      "cube", "sphere", "plane", "quad", "cylinder", "capsule", "mesh", "col", "lod0", "lod1", "lod2", "glass", "shell",
      "thing", "obj", "center", "detail", "middlebar", "foundation"
    };

    private static readonly string[] ExcludedMaterialPrefixes = {
      "envMatTree", "envMatBush", "envMatGrass", "envMatRock", "envMatTerr", "envMatWater",
      "envMatDecl_terrain", "Hidden/", "Unlit/", "Sprites-", "Default-Particle", "Default-Material", "BinkMaterial", "lambert"
    };

    private static int Main(string[] args) {
      string btData = @"D:\Program Files (x86)\Steam\steamapps\common\BATTLETECH\BattleTech_Data";
      string outDir = null;
      bool includeUrban = false;
      bool overwrite = false;
      bool dryRun = false;

      for (int i = 0; i < args.Length; i++) {
        switch (args[i]) {
          case "--bt-data": btData = args[++i]; break;
          case "--out": outDir = args[++i]; break;
          case "--include-urban": includeUrban = true; break;
          case "--overwrite": overwrite = true; break;
          case "--dry-run": dryRun = true; break;
          case "--help":
          case "-h":
            PrintUsage();
            return 0;
        }
      }

      if (string.IsNullOrEmpty(outDir)) {
        outDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "props"));
      }

      if (!Directory.Exists(btData)) {
        Console.Error.WriteLine($"BattleTech_Data not found at '{btData}'.");
        return 1;
      }

      Console.WriteLine($"Scanning BattleTech_Data: {btData}");
      Console.WriteLine($"Output props directory : {outDir}");
      Console.WriteLine($"Include Urban Warfare  : {includeUrban} (classic levels 8..83 only when false)");

      var index = GameAssetIndex.BuildOrLoad(btData, null, Console.WriteLine);

      // Build map of mesh AssetRef -> best observed Material list from classic combat scenes (level8..level83)
      var materialsByMeshRef = new Dictionary<AssetRef, Dictionary<string, int>>();
      var seenCountByMeshRef = new Dictionary<AssetRef, int>();

      int maxLevel = includeUrban ? 999 : 83;
      var levelFiles = Directory.GetFiles(btData, "level*")
        .Select(p => new { Path = p, Match = LevelFileRegex.Match(Path.GetFileName(p)) })
        .Where(x => x.Match.Success && int.TryParse(x.Match.Groups[1].Value, out int n) && n >= 8 && n <= maxLevel)
        .OrderBy(x => int.Parse(x.Match.Groups[1].Value))
        .ToList();

      Console.WriteLine($"Scanning {levelFiles.Count} combat map scenes for MeshFilter/MeshRenderer material bindings...");

      foreach (var lf in levelFiles) {
        SerializedFileHeader lvlHeader = SerializedFileReader.ReadHeader(lf.Path, -1);
        if (lvlHeader == null) continue;

        using (FileStream fs = new FileStream(lf.Path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          // Collect MeshFilter -> (goPathId, meshRef) and MeshRenderer -> (goPathId, matNames)
          var meshByGo = new Dictionary<long, AssetRef>();
          var matsByGo = new Dictionary<long, List<string>>();

          foreach (var obj in lvlHeader.ObjectList) {
            if (obj.ClassId == UnityClassId.MeshFilter) {
              byte[] bytes = SerializedFileReader.ReadObjectBytes(fs, lvlHeader, obj);
              var r = new BinaryStreamReader(bytes, 0, lvlHeader.BigEndian);
              PPtr goPtr = r.ReadPPtr();
              PPtr meshPtr = r.ReadPPtr();
              if (goPtr.FileId == 0 && goPtr.PathId != 0 && !meshPtr.IsNull) {
                AssetRef mref = index.ResolvePPtr(lvlHeader, meshPtr);
                if (!mref.IsNull) meshByGo[goPtr.PathId] = mref;
              }
            } else if (obj.ClassId == UnityClassId.MeshRenderer) {
              byte[] bytes = SerializedFileReader.ReadObjectBytes(fs, lvlHeader, obj);
              if (bytes.Length < 68) continue;
              var r = new BinaryStreamReader(bytes, 0, lvlHeader.BigEndian);
              PPtr goPtr = r.ReadPPtr();
              if (goPtr.FileId != 0 || goPtr.PathId == 0) continue;

              List<PPtr> matPtrs = HierarchyDecoder.DecodeMeshRendererMaterials(bytes, lvlHeader.BigEndian);
              if (matPtrs.Count == 0) continue;

              var names = new List<string>(matPtrs.Count);
              foreach (PPtr mp in matPtrs) {
                AssetRef matRef = index.ResolvePPtr(lvlHeader, mp);
                string mn = !matRef.IsNull && index.AssetNamesByRef.TryGetValue(matRef, out string n) ? n : null;
                names.Add(mn);
              }
              matsByGo[goPtr.PathId] = names;
            }
          }

          foreach (var kv in meshByGo) {
            if (!matsByGo.TryGetValue(kv.Key, out List<string> matNames)) continue;
            if (matNames.All(string.IsNullOrEmpty)) continue;

            AssetRef mref = kv.Value;
            seenCountByMeshRef[mref] = seenCountByMeshRef.TryGetValue(mref, out int sc) ? sc + 1 : 1;

            string sig = string.Join("|", matNames.Select(m => m ?? ""));
            if (!materialsByMeshRef.TryGetValue(mref, out var votes)) {
              votes = new Dictionary<string, int>();
              materialsByMeshRef[mref] = votes;
            }
            votes[sig] = votes.TryGetValue(sig, out int v) ? v + 1 : 1;
          }
        }
      }

      Console.WriteLine($"Observed {materialsByMeshRef.Count} distinct meshes with scene material bindings.");

      // Index split prefabs by base name suffix
      var splitPrefabByLowerBase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (string goName in index.GameObjectsByName.Keys) {
        const string suffix = "_generic_static_destruct_split";
        int idx = goName.IndexOf(suffix, StringComparison.OrdinalIgnoreCase);
        if (idx > 0) {
          string prefixPart = goName.Substring(0, idx);
          int us = prefixPart.IndexOf('_');
          string basePart = us > 0 && prefixPart.StartsWith("envPrf", StringComparison.OrdinalIgnoreCase)
            ? prefixPart.Substring(us + 1)
            : prefixPart;
          if (!splitPrefabByLowerBase.ContainsKey(basePart)) {
            splitPrefabByLowerBase[basePart] = goName;
          }
        }
      }

      // Build candidate DiscoveredModels
      var discovered = new Dictionary<string, DiscoveredModel>(StringComparer.OrdinalIgnoreCase);

      foreach (var kv in materialsByMeshRef) {
        AssetRef mref = kv.Key;
        if (mref.FileIndex < 0 || mref.FileIndex >= index.Files.Count) continue;

        string srcFile = index.Files[mref.FileIndex].FileName;
        if (!includeUrban && IsUrbanSharedAsset(srcFile)) continue;
        if (!index.AssetNamesByRef.TryGetValue(mref, out string meshName) || string.IsNullOrEmpty(meshName)) continue;

        // Pick most frequent material assignment
        string bestMatSig = kv.Value.OrderByDescending(p => p.Value).First().Key;
        var matList = bestMatSig.Split('|').Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (matList.Count == 0) continue;
        if (matList.All(IsExcludedMaterial)) continue;

        // Skip LOD1/LOD2/COL/glass/shell/split/fract child meshes - we key off LOD0 or standalone flimsy mesh
        if (meshName.EndsWith("_LOD1", StringComparison.OrdinalIgnoreCase)
            || meshName.EndsWith("_LOD2", StringComparison.OrdinalIgnoreCase)
            || meshName.EndsWith("_COL", StringComparison.OrdinalIgnoreCase)
            || meshName.EndsWith("_glass", StringComparison.OrdinalIgnoreCase)
            || meshName.IndexOf("_shell", StringComparison.OrdinalIgnoreCase) >= 0
            || meshName.IndexOf("_split_", StringComparison.OrdinalIgnoreCase) >= 0
            || meshName.IndexOf("_fract_", StringComparison.OrdinalIgnoreCase) >= 0) {
          continue;
        }

        bool isLod0 = meshName.EndsWith("_LOD0", StringComparison.OrdinalIgnoreCase);
        string baseName = isLod0 ? meshName.Substring(0, meshName.Length - 5) : meshName;

        if (IsExcludedMeshName(baseName)) continue;
        if (!isLod0 && CellOrChunkRegex.IsMatch(baseName)) continue;

        // If a standalone mesh has a matching _LOD0 sibling, prefer the _LOD0 entry
        if (!isLod0 && !index.FindMesh($"{baseName}_LOD0").IsNull) continue;

        // For non-LOD flimsy meshes, collapse numbered Maya/FBX duplicates (e.g. AcUnit1..AcUnit99, Light0110..0199)
        // onto a single canonical entry while preserving distinct letter variants (smallBarrelA vs smallBarrelB)
        string dedupeKey = baseName;
        if (!isLod0) {
          string stripped = TrailingDigitsRegex.Replace(baseName, "");
          if (stripped.Length >= 3) {
            if (ExcludedPartNames.Contains(stripped)) continue;
            dedupeKey = stripped;
          } else if (ExcludedPartNames.Contains(baseName)) {
            continue;
          }
        }

        // Deduplicate (prefer exact unnumbered name if available, otherwise highest seen count)
        int seen = seenCountByMeshRef.TryGetValue(mref, out int s) ? s : 1;
        if (discovered.TryGetValue(dedupeKey, out var existing)) {
          bool curExact = string.Equals(baseName, dedupeKey, StringComparison.OrdinalIgnoreCase);
          bool exExact = string.Equals(existing.MeshBaseName, dedupeKey, StringComparison.OrdinalIgnoreCase);
          if ((exExact && !curExact) || (exExact == curExact && existing.SeenCount >= seen)) {
            continue;
          }
        }

        RawMeshData rawMesh = index.DecodeMesh(mref);
        if (rawMesh == null || rawMesh.VertexCount < 4 || rawMesh.SubMeshes == null || rawMesh.SubMeshes.Length == 0) {
          continue;
        }

        var bounds = new Float3(
          rawMesh.LocalAabb.Extents.X * 2f,
          rawMesh.LocalAabb.Extents.Y * 2f,
          rawMesh.LocalAabb.Extents.Z * 2f);

        // Skip tiny degenerate/micro meshes (< 0.25m in all axes) or huge terrain-sized slabs (> 260m)
        float maxDim = Math.Max(bounds.X, Math.Max(bounds.Y, bounds.Z));
        if (maxDim < 0.25f || maxDim > 260f) continue;

        var model = new DiscoveredModel {
          MeshBaseName = baseName,
          IsLodBased = isLod0,
          Lod0Ref = mref,
          Lod1Ref = isLod0 ? index.FindMesh($"{baseName}_LOD1", srcFile) : default,
          Lod2Ref = isLod0 ? index.FindMesh($"{baseName}_LOD2", srcFile) : default,
          ColRef = index.FindMesh($"{baseName}_COL", srcFile),
          GlassRef = index.FindMesh($"{baseName}_glass", srcFile),
          ShellRef = index.FindMesh($"{baseName}_shell", srcFile),
          ShellColRef = index.FindMesh($"{baseName}_shell_COL", srcFile),
          SourceAssetFile = srcFile,
          Materials = matList,
          BoundsSize = bounds,
          SubMeshCount = rawMesh.SubMeshes.Length,
          SeenCount = seen
        };

        if (splitPrefabByLowerBase.TryGetValue(baseName, out string splitName)) {
          model.SplitPrefabName = splitName;
        }

        // Check if glass mesh has a scene-observed material
        if (!model.GlassRef.IsNull && materialsByMeshRef.TryGetValue(model.GlassRef, out var glassVotes)) {
          string gMat = glassVotes.OrderByDescending(p => p.Value).First().Key.Split('|').FirstOrDefault(x => !string.IsNullOrEmpty(x));
          if (!string.IsNullOrEmpty(gMat)) model.GlassMaterial = gMat;
        }

        ClassifyModel(model);
        if (model.Role == "Destructible" && !model.IsLodBased && model.SeenCount < 2) {
          continue;
        }
        discovered[dedupeKey] = model;
      }

      var models = discovered.Values
        .OrderBy(m => m.Role)
        .ThenBy(m => m.Category)
        .ThenBy(m => m.Key)
        .ToList();

      Console.WriteLine($"\nDiscovered {models.Count} placeable classic vanilla assets:");
      Console.WriteLine($"  Buildings     : {models.Count(m => m.Role == "Building")}");
      Console.WriteLine($"  Structures    : {models.Count(m => m.Role == "Structure")}");
      Console.WriteLine($"  Destructibles : {models.Count(m => m.Role == "Destructible")}");

      if (dryRun) {
        foreach (var g in models.GroupBy(m => $"{m.Role}/{m.Category}").OrderBy(g => g.Key)) {
          Console.WriteLine($"  {g.Key} ({g.Count()}): {string.Join(", ", g.Take(5).Select(x => x.Key))}{(g.Count() > 5 ? ", ..." : "")}");
        }
        return 0;
      }

      // Collect existing keys so we never clobber hand-curated/migrated MC keys unless --overwrite
      var existingModelKeys = CollectExistingJsonKeys(Path.Combine(outDir, "models"), "model.json");
      var existingBuildingKeys = CollectExistingJsonKeys(Path.Combine(outDir, "buildings"), "*.json");
      var existingStructureKeys = CollectExistingJsonKeys(Path.Combine(outDir, "structures"), "*.json");
      var existingDestructibleKeys = CollectExistingJsonKeys(Path.Combine(outDir, "destructibles"), "*.json");

      int writtenModels = 0, writtenBuildings = 0, writtenStructures = 0, writtenDestructibles = 0;

      foreach (var m in models) {
        bool modelNeedsDisambig = NeedsDisambiguation(index, m);

        // 1. Write model.json under props/models/Vanilla/<Category>/<Key>/model.json
        string modelDir = Path.Combine(outDir, "models", "Vanilla", m.Category, m.Key);
        string modelPath = Path.Combine(modelDir, "model.json");
        if (overwrite || (!existingModelKeys.Contains(m.Key) && !File.Exists(modelPath))) {
          Directory.CreateDirectory(modelDir);
          JObject modelJson = BuildModelJson(m, modelNeedsDisambig);
          File.WriteAllText(modelPath, modelJson.ToString(Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
          writtenModels++;
        }

        // 2. Write role-specific def JSON
        if (m.Role == "Building") {
          string bDir = Path.Combine(outDir, "buildings", m.Category);
          string bPath = Path.Combine(bDir, $"{m.Key}.json");
          if (overwrite || (!existingBuildingKeys.Contains(m.Key) && !File.Exists(bPath))) {
            Directory.CreateDirectory(bDir);
            JObject bJson = BuildBuildingJson(m);
            File.WriteAllText(bPath, bJson.ToString(Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
            writtenBuildings++;
          }
        } else if (m.Role == "Structure") {
          string sDir = Path.Combine(outDir, "structures", m.Category);
          string sPath = Path.Combine(sDir, $"{m.Key}.json");
          if (overwrite || (!existingStructureKeys.Contains(m.Key) && !File.Exists(sPath))) {
            Directory.CreateDirectory(sDir);
            JObject sJson = BuildStructureJson(m);
            File.WriteAllText(sPath, sJson.ToString(Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
            writtenStructures++;
          }
        } else {
          string dDir = Path.Combine(outDir, "destructibles", m.Category);
          string dPath = Path.Combine(dDir, $"{m.Key}.json");
          if (overwrite || (!existingDestructibleKeys.Contains(m.Key) && !File.Exists(dPath))) {
            Directory.CreateDirectory(dDir);
            JObject dJson = BuildDestructibleJson(m);
            File.WriteAllText(dPath, dJson.ToString(Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
            writtenDestructibles++;
          }
        }
      }

      Console.WriteLine($"\nWritten JSONs: {writtenModels} models, {writtenBuildings} buildings, {writtenStructures} structures, {writtenDestructibles} destructibles.");
      return 0;
    }

    private static bool IsUrbanSharedAsset(string fileName) {
      Match m = SharedAssetsRegex.Match(fileName);
      return m.Success && int.TryParse(m.Groups[1].Value, out int n) && n >= 84;
    }

    private static bool IsExcludedMeshName(string name) {
      if (string.IsNullOrWhiteSpace(name) || name.Length < 3) return true;
      string low = name.ToLowerInvariant();
      foreach (string ex in ExcludedExactNames) {
        if (low == ex) return true;
      }
      foreach (string sub in ExcludedNameSubstrings) {
        if (low.Contains(sub.ToLowerInvariant())) return true;
      }
      if (low.StartsWith("urbmdl") || low.StartsWith("urbprf") || low.Contains("|dupli|")) return true;
      return false;
    }

    private static bool IsExcludedMaterial(string matName) {
      if (string.IsNullOrEmpty(matName)) return true;
      foreach (string p in ExcludedMaterialPrefixes) {
        if (matName.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
      }
      return false;
    }

    private static bool NeedsDisambiguation(GameAssetIndex index, DiscoveredModel m) {
      string primary = m.IsLodBased ? $"{m.MeshBaseName}_LOD0" : m.MeshBaseName;
      if (index.MeshesByName.TryGetValue(primary, out var list) && list.Count > 1) return true;
      if (!m.ColRef.IsNull && index.MeshesByName.TryGetValue($"{m.MeshBaseName}_COL", out var colList) && colList.Count > 1) return true;
      return false;
    }

    private static void ClassifyModel(DiscoveredModel m) {
      string low = m.MeshBaseName.ToLowerInvariant();
      float maxHorizontal = Math.Max(m.BoundsSize.X, m.BoundsSize.Z);
      float maxDim = Math.Max(maxHorizontal, m.BoundsSize.Y);

      bool hasDestructAssets = !m.ShellRef.IsNull || !string.IsNullOrEmpty(m.SplitPrefabName);

      bool looksLikeStructure =
        low.Contains("bridge") || low.Contains("dam") || low.Contains("fndn") || low.Contains("foundation")
        || low.Contains("ramp") || low.Contains("wall") || low.Contains("barrier") || low.Contains("walkway")
        || low.Contains("dias") || low.Contains("dais") || low.Contains("platform") || low.Contains("mount")
        || low.Contains("pad") || low.Contains("arch") || low.Contains("buttress") || low.Contains("support")
        || low.Contains("arena") || low.Contains("road") || low.Contains("pier") || low.Contains("dock")
        || low.Contains("runway") || low.Contains("tunnel") || low.Contains("stair") || low.Contains("catwalk")
        || low.Contains("gantry") || low.Contains("pillar") || low.Contains("column") || low.Contains("base13");

      bool looksLikeBuilding =
        hasDestructAssets
        || low.Contains("bldg") || low.Contains("building") || low.Contains("barracks") || low.Contains("bunker")
        || low.Contains("tower") || low.Contains("factory") || low.Contains("office") || low.Contains("apartment")
        || low.Contains("dome") || low.Contains("biodome") || low.Contains("depot") || low.Contains("hangar")
        || low.Contains("warehouse") || low.Contains("station") || low.Contains("silo") || low.Contains("generator")
        || low.Contains("reactor") || low.Contains("refinery") || low.Contains("commcenter") || low.Contains("radar")
        || low.Contains("clinic") || low.Contains("hospital") || low.Contains("garage") || low.Contains("shed")
        || low.Contains("tank") || low.Contains("substation") || low.Contains("turret") || low.Contains("outpost")
        || low.Contains("plant") || low.Contains("facility") || low.Contains("foundry") || low.Contains("storage")
        || low.Contains("control") || low.Contains("command");

      if (m.IsLodBased && looksLikeBuilding && !looksLikeStructure) {
        m.Role = "Building";
      } else if (m.IsLodBased && (looksLikeStructure || !m.ColRef.IsNull)) {
        m.Role = hasDestructAssets ? "Building" : "Structure";
      } else if (hasDestructAssets) {
        m.Role = "Building";
      } else if (maxDim >= 14f && !m.ColRef.IsNull) {
        m.Role = "Structure";
      } else {
        m.Role = "Destructible";
      }

      m.Category = InferCategory(low, m.Role);
      string cleanBase = CleanKeyName(m.MeshBaseName);
      m.Key = $"Vanilla_{cleanBase}";
      m.FriendlyName = MakeFriendlyName(cleanBase);
    }

    private static string InferCategory(string low, string role) {
      if (role == "Building") {
        if (low.Contains("barrier") || low.Contains("wall")) return "Barriers";
        if (low.Contains("gate") || low.Contains("bridge") || low.Contains("tunnel")) return "GatesAndBridges";
        if (low.Contains("villa") || low.Contains("village") || low.Contains("ruin")) return "VillasAndRuins";
        if (low.Contains("hangar") || low.Contains("unloading") || low.Contains("shipping") || low.Contains("packaging") || low.Contains("depot")) return "HangarsAndDepots";
        if (low.Contains("scrap") || low.Contains("shed")) return "Outposts";
        if (low.Contains("military") || low.Contains("barracks") || low.Contains("bunker") || low.Contains("command") || low.Contains("hq") || low.Contains("fort") || low.Contains("missile")) return "Military";
        if (low.Contains("civ") || low.Contains("apartment") || low.Contains("office") || low.Contains("city") || low.Contains("house") || low.Contains("clinic") || low.Contains("biodome") || low.Contains("dome")) return "Civilian";
        if (low.Contains("industrial") || low.Contains("factory") || low.Contains("warehouse") || low.Contains("refinery") || low.Contains("silo") || low.Contains("tank") || low.Contains("foundry") || low.Contains("plant") || low.Contains("processing") || low.Contains("mining")) return "Industrial";
        if (low.Contains("tower") || low.Contains("radar") || low.Contains("comm") || low.Contains("antenna") || low.Contains("dish")) return "Towers";
        if (low.Contains("generator") || low.Contains("reactor") || low.Contains("turbine") || low.Contains("machine") || low.Contains("power") || low.Contains("aircondition")) return "Machines";
        return "Buildings";
      }

      if (role == "Structure") {
        if (low.Contains("bridge") || low.Contains("walkway") || low.Contains("catwalk")) return "Bridges";
        if (low.Contains("tunnel") || low.Contains("road")) return "TunnelsAndRoads";
        if (low.Contains("dam") || low.Contains("arch") || low.Contains("buttress")) return "Dams";
        if (low.Contains("mechbay") || low.Contains("mechdoor") || low.Contains("bay")) return "MechBays";
        if (low.Contains("ruin") || low.Contains("destroyed") || low.Contains("pirate") || low.Contains("longtom") || low.Contains("dropship")) return "RuinsAndWrecks";
        if (low.Contains("ramp") || low.Contains("stair") || low.Contains("slope")) return "Ramps";
        if (low.Contains("mount") || low.Contains("turret")) return "Mounts";
        if (low.Contains("wall") || low.Contains("barrier") || low.Contains("gate")) return "Walls";
        if (low.Contains("fndn") || low.Contains("foundation") || low.Contains("dias") || low.Contains("dais") || low.Contains("platform") || low.Contains("pad") || low.Contains("base") || low.Contains("flat") || low.Contains("block") || low.Contains("square") || low.Contains("cube") || low.Contains("lshape") || low.Contains("lowb")) return "Foundations";
        if (low.Contains("industrial") || low.Contains("pipe")) return "Industrial";
        return "Structures";
      }

      // Destructibles (flimsies)
      if (low.Contains("fence") || low.Contains("rail") || low.Contains("cage") || low.Contains("bannister")) return "Fences";
      if (low.Contains("pipe") || low.Contains("conduit") || low.Contains("duct") || low.Contains("valve")) return "Pipes";
      if (low.Contains("lamp") || low.Contains("light") || low.Contains("pole") || low.Contains("pylon")) return "Lampposts";
      if (low.Contains("crate") || low.Contains("box") || low.Contains("barrel") || low.Contains("container") || low.Contains("drum") || low.Contains("pallet") || low.Contains("tank") || low.Contains("bucket")) return "Containers";
      if (low.Contains("truck") || low.Contains("car") || low.Contains("veh") || low.Contains("apc") || low.Contains("crane") || low.Contains("forklift") || low.Contains("trailer") || low.Contains("cart") || low.Contains("rover") || low.Contains("swiftwind") || low.Contains("chassis") || low.Contains("tire") || low.Contains("wheel") || low.Contains("vhlprk")) return "Vehicles";
      if (low.Contains("hut") || low.Contains("researchplatform") || low.Contains("shed") || low.Contains("kwansit") || low.Contains("huntbend")) return "HutsAndPlatforms";
      if (low.Contains("tower") || low.Contains("watertower")) return "Towers";
      if (low.Contains("rubble")) return "Rubble";
      if (low.Contains("turbine") || low.Contains("windmill") || low.Contains("gear")) return "Gear";
      if (low.Contains("machine") || low.Contains("ac") || low.Contains("aircondition") || low.Contains("vent") || low.Contains("gen") || low.Contains("solar") || low.Contains("radar") || low.Contains("antenna") || low.Contains("dish") || low.Contains("electric") || low.Contains("console") || low.Contains("computer") || low.Contains("exhaust") || low.Contains("filter") || low.Contains("lift") || low.Contains("turret") || low.Contains("mine")) return "Machines";
      if (low.Contains("wall") || low.Contains("barrier") || low.Contains("block") || low.Contains("bollard") || low.Contains("bumper") || low.Contains("tollgate") || low.Contains("trafficcone") || low.Contains("grdpost")) return "Barriers";
      if (low.Contains("door") || low.Contains("panel") || low.Contains("ladder") || low.Contains("stair") || low.Contains("catwalk") || low.Contains("scaffold") || low.Contains("pillar") || low.Contains("beam") || low.Contains("strut")) return "StructuresAndParts";
      if (low.Contains("sign") || low.Contains("banner") || low.Contains("flag") || low.Contains("billboard") || low.Contains("clothesline")) return "Signs";
      return "Props";
    }

    private static string CleanKeyName(string raw) {
      // Strip verbose HBS prefixes like envMdlFndn_ / envMdlProp_ / envMdlStct_
      string s = raw;
      s = Regex.Replace(s, @"^envMdl[A-Za-z]+_", "", RegexOptions.IgnoreCase);
      s = Regex.Replace(s, @"_(?:generic_)?static_obstruct.*$", "", RegexOptions.IgnoreCase);
      s = Regex.Replace(s, @"[^A-Za-z0-9_]", "_");
      s = Regex.Replace(s, @"_+", "_").Trim('_');
      if (s.Length == 0) s = "Prop";
      return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    private static string MakeFriendlyName(string cleanKey) {
      string spaced = Regex.Replace(cleanKey, @"(?<=[a-z0-9])(?=[A-Z])", " ");
      spaced = spaced.Replace('_', ' ');
      spaced = Regex.Replace(spaced, @"\s+", " ").Trim();
      return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.ToLowerInvariant());
    }

    private static (string Size, string Material, string FlimsyType, string BuildingDefId) InferDestructibleStats(DiscoveredModel m) {
      float footprint = Math.Max(m.BoundsSize.X, m.BoundsSize.Z);
      float height = m.BoundsSize.Y;
      float maxDim = Math.Max(footprint, height);
      string low = m.MeshBaseName.ToLowerInvariant();

      string size;
      string bdefSize;
      if (maxDim >= 36f) { size = "huge"; bdefSize = "Large"; }
      else if (maxDim >= 22f) { size = "large"; bdefSize = "Large"; }
      else if (maxDim >= 10f) { size = "medium"; bdefSize = "Medium"; }
      else { size = "small"; bdefSize = "Small"; }

      bool isStone = low.Contains("concrete") || low.Contains("stone") || low.Contains("bunker") || low.Contains("dam")
        || m.Materials.Any(mat => mat.IndexOf("concrete", StringComparison.OrdinalIgnoreCase) >= 0 || mat.IndexOf("asphalt", StringComparison.OrdinalIgnoreCase) >= 0);
      bool isFence = low.Contains("fence");

      string matType = isFence ? "fence" : (isStone ? "stone" : "metal");
      string flimsyType;
      if (size == "small") flimsyType = isStone ? "smallStone" : "smallMetal";
      else if (size == "medium") flimsyType = isStone ? "mediumStone" : "mediumMetal";
      else flimsyType = isStone ? "largeStone" : "largeMetal";

      string bdefTheme = low.Contains("military") || low.Contains("barracks") || low.Contains("bunker") || low.Contains("turret")
        ? "Military"
        : (low.Contains("industrial") || low.Contains("factory") || low.Contains("warehouse") || low.Contains("silo") || low.Contains("generator") ? "Industrial" : "Civilian");

      string buildingDefId = $"buildingdef_{bdefTheme}_{bdefSize}";
      return (size, matType, flimsyType, buildingDefId);
    }

    private static JObject BuildModelJson(DiscoveredModel m, bool includeDisambig) {
      var stats = InferDestructibleStats(m);
      var obj = new JObject {
        ["Key"] = m.Key,
        ["AssetSource"] = "GameData",
        ["Mesh"] = m.MeshBaseName
      };

      if (includeDisambig) {
        obj["SourceAssetFile"] = m.SourceAssetFile;
        var pids = new JObject();
        if (m.IsLodBased) {
          pids["LOD0"] = m.Lod0Ref.PathId;
          if (!m.Lod1Ref.IsNull) pids["LOD1"] = m.Lod1Ref.PathId;
          if (!m.Lod2Ref.IsNull) pids["LOD2"] = m.Lod2Ref.PathId;
        } else {
          pids["Flimsy"] = m.Lod0Ref.PathId;
        }
        if (!m.ColRef.IsNull) pids["COL"] = m.ColRef.PathId;
        if (!m.GlassRef.IsNull) pids["Glass"] = m.GlassRef.PathId;
        if (!m.ShellRef.IsNull) pids["Shell"] = m.ShellRef.PathId;
        if (!m.ShellColRef.IsNull) pids["ShellCOL"] = m.ShellColRef.PathId;
        obj["MeshPathIds"] = pids;
      }

      var matsArr = new JArray();
      foreach (string matName in m.Materials) {
        matsArr.Add(new JObject { ["Name"] = matName });
      }
      obj["Materials"] = matsArr;

      if (!string.IsNullOrEmpty(m.GlassMaterial)) {
        obj["GlassMaterial"] = new JObject { ["Name"] = m.GlassMaterial };
      }

      obj["IsMeshInBundle"] = false;
      obj["HasCustomSplits"] = !string.IsNullOrEmpty(m.SplitPrefabName);
      if (!string.IsNullOrEmpty(m.SplitPrefabName)) {
        obj["SplitPrefabName"] = m.SplitPrefabName;
      }
      obj["HasCustomShell"] = !m.ShellRef.IsNull;
      obj["DestructibleSize"] = stats.Size;
      obj["DestructibleMaterial"] = stats.Material;
      obj["FlimsyDestructibleType"] = stats.FlimsyType;

      return obj;
    }

    private static JObject BuildBuildingJson(DiscoveredModel m) {
      var stats = InferDestructibleStats(m);
      return new JObject {
        ["Key"] = m.Key,
        ["FriendlyName"] = m.FriendlyName,
        ["BuildingDefID"] = stats.BuildingDefId,
        ["MainModel"] = m.Key
      };
    }

    private static JObject BuildStructureJson(DiscoveredModel m) {
      return new JObject {
        ["Key"] = m.Key,
        ["FriendlyName"] = m.FriendlyName,
        ["MainModel"] = m.Key
      };
    }

    private static JObject BuildDestructibleJson(DiscoveredModel m) {
      float mass = m.BoundsSize.X * m.BoundsSize.Y * m.BoundsSize.Z >= 20f ? 500f : 100f;
      return new JObject {
        ["Key"] = m.Key,
        ["FriendlyName"] = m.FriendlyName,
        ["MainModel"] = m.Key,
        ["Mass"] = mass
      };
    }

    private static HashSet<string> CollectExistingJsonKeys(string rootDir, string pattern) {
      var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      if (!Directory.Exists(rootDir)) return keys;
      foreach (string file in Directory.GetFiles(rootDir, pattern, SearchOption.AllDirectories)) {
        try {
          JObject jo = JObject.Parse(File.ReadAllText(file));
          string k = jo["Key"]?.ToString();
          if (!string.IsNullOrEmpty(k)) keys.Add(k);
        } catch { }
      }
      return keys;
    }

    private static void PrintUsage() {
      Console.WriteLine("VanillaPropCatalogueGenerator - Scans BattleTech_Data and generates MissionControl/Designer prop JSON definitions.");
      Console.WriteLine("Options:");
      Console.WriteLine("  --bt-data <path>    Path to BattleTech_Data directory");
      Console.WriteLine("  --out <path>        Output MissionControl props directory");
      Console.WriteLine("  --include-urban     Include Urban Warfare (uTech, level84..93) assets (post-1.6)");
      Console.WriteLine("  --overwrite         Overwrite existing generated JSON files");
      Console.WriteLine("  --dry-run           Print summary without writing files");
    }
  }
}
