using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BTAssetReader.Core.Decoders;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core {
  public sealed class GameAssetIndex {
    private const uint CacheMagic = 0x52415442; // "BTAR"
    private const int CacheFormatVersion = 1;
    private static readonly Regex SharedAssetsRegex = new Regex(@"^sharedassets(\d+)\.assets$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string DataDirectory { get; }
    public List<SerializedFileHeader> Files { get; } = new List<SerializedFileHeader>();
    public Dictionary<string, int> FileIndexByName { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, List<AssetRef>> MeshesByName { get; } = new Dictionary<string, List<AssetRef>>(StringComparer.Ordinal);
    public Dictionary<string, List<AssetRef>> MaterialsByName { get; } = new Dictionary<string, List<AssetRef>>(StringComparer.Ordinal);
    public Dictionary<string, List<AssetRef>> TexturesByName { get; } = new Dictionary<string, List<AssetRef>>(StringComparer.Ordinal);
    public Dictionary<string, List<AssetRef>> GameObjectsByName { get; } = new Dictionary<string, List<AssetRef>>(StringComparer.Ordinal);

    public Dictionary<AssetRef, string> ShaderNamesByRef { get; } = new Dictionary<AssetRef, string>();
    public Dictionary<AssetRef, string> AssetNamesByRef { get; } = new Dictionary<AssetRef, string>();

    private GameAssetIndex(string dataDirectory) {
      DataDirectory = dataDirectory;
    }

    public static GameAssetIndex BuildOrLoad(string battleTechDataDir, string cacheFilePath = null, Action<string> log = null) {
      if (string.IsNullOrEmpty(battleTechDataDir) || !Directory.Exists(battleTechDataDir)) {
        throw new DirectoryNotFoundException($"BattleTech_Data directory not found: '{battleTechDataDir}'.");
      }

      var candidatePaths = EnumerateCoreAssetFiles(battleTechDataDir);
      string fingerprint = ComputeFingerprint(battleTechDataDir, candidatePaths);

      if (!string.IsNullOrEmpty(cacheFilePath) && File.Exists(cacheFilePath)) {
        try {
          GameAssetIndex cached = TryLoadCache(battleTechDataDir, cacheFilePath, fingerprint, candidatePaths);
          if (cached != null) {
            log?.Invoke($"[BTAssetReader] Loaded cached game asset index ({cached.MeshesByName.Count} unique meshes, {cached.MaterialsByName.Count} unique materials, {cached.TexturesByName.Count} unique textures).");
            return cached;
          }
        } catch (Exception ex) {
          log?.Invoke($"[BTAssetReader] Asset index cache invalid ({ex.Message}); rebuilding...");
        }
      }

      var index = BuildFresh(battleTechDataDir, candidatePaths);
      log?.Invoke($"[BTAssetReader] Built game asset index across {index.Files.Count} files ({index.MeshesByName.Count} unique meshes, {index.MaterialsByName.Count} unique materials, {index.TexturesByName.Count} unique textures, {index.ShaderNamesByRef.Count} shaders).");

      if (!string.IsNullOrEmpty(cacheFilePath)) {
        try {
          SaveCache(index, cacheFilePath, fingerprint);
        } catch (Exception ex) {
          log?.Invoke($"[BTAssetReader] Failed to save asset index cache '{cacheFilePath}': {ex.Message}");
        }
      }

      return index;
    }

    private static List<string> EnumerateCoreAssetFiles(string dataDir) {
      var list = new List<string>();
      string ggm = Path.Combine(dataDir, "globalgamemanagers.assets");
      if (File.Exists(ggm)) list.Add(ggm);
      string res = Path.Combine(dataDir, "resources.assets");
      if (File.Exists(res)) list.Add(res);

      var shared = new List<(int Num, string Path)>();
      foreach (string f in Directory.GetFiles(dataDir, "sharedassets*.assets")) {
        Match m = SharedAssetsRegex.Match(Path.GetFileName(f));
        if (m.Success && int.TryParse(m.Groups[1].Value, out int n)) {
          shared.Add((n, f));
        }
      }
      shared.Sort((a, b) => a.Num.CompareTo(b.Num));
      foreach (var s in shared) list.Add(s.Path);
      return list;
    }

    private static string ComputeFingerprint(string dataDir, List<string> files) {
      long totalSize = 0;
      long xorTicks = 0;
      foreach (string f in files) {
        var fi = new FileInfo(f);
        totalSize += fi.Length;
        xorTicks ^= fi.LastWriteTimeUtc.Ticks;
      }
      string versionJson = Path.Combine(dataDir, "StreamingAssets", "version.json");
      string verStamp = File.Exists(versionJson) ? new FileInfo(versionJson).Length.ToString() : "0";
      return $"{files.Count}|{totalSize}|{xorTicks}|{verStamp}";
    }

    private static GameAssetIndex BuildFresh(string dataDir, List<string> filePaths) {
      var index = new GameAssetIndex(dataDir);

      for (int fi = 0; fi < filePaths.Count; fi++) {
        string path = filePaths[fi];
        SerializedFileHeader header = SerializedFileReader.ReadHeader(path, fi);
        if (header == null) continue;

        index.Files.Add(header);
        index.FileIndexByName[header.FileName] = fi;

        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          byte[] namePeek = new byte[264];

          foreach (var obj in header.ObjectList) {
            if (obj.ClassId == UnityClassId.Mesh || obj.ClassId == UnityClassId.Material || obj.ClassId == UnityClassId.Texture2D) {
              string name = PeekNamedObjectName(fs, header, obj, namePeek);
              if (string.IsNullOrEmpty(name)) continue;

              var aref = new AssetRef(fi, obj.PathId);
              index.AssetNamesByRef[aref] = name;

              if (obj.ClassId == UnityClassId.Mesh) AddToMap(index.MeshesByName, name, aref);
              else if (obj.ClassId == UnityClassId.Material) AddToMap(index.MaterialsByName, name, aref);
              else if (obj.ClassId == UnityClassId.Texture2D) AddToMap(index.TexturesByName, name, aref);
            } else if (obj.ClassId == UnityClassId.Shader) {
              byte[] bytes = SerializedFileReader.ReadObjectBytes(fs, header, obj);
              string shaderName = ShaderNameDecoder.ExtractShaderName(bytes, header.BigEndian);
              if (!string.IsNullOrEmpty(shaderName)) {
                index.ShaderNamesByRef[new AssetRef(fi, obj.PathId)] = shaderName;
              }
            } else if (obj.ClassId == UnityClassId.GameObject) {
              // Index split/shell prefab roots in sharedassets*.assets for runtime split reconstruction
              if (!header.FileName.StartsWith("sharedassets", StringComparison.OrdinalIgnoreCase)) continue;
              string goName = HierarchyDecoder.PeekGameObjectName(fs, header, obj);
              if (!string.IsNullOrEmpty(goName) && IsInterestingPrefabName(goName)) {
                var aref = new AssetRef(fi, obj.PathId);
                AddToMap(index.GameObjectsByName, goName, aref);
              }
            }
          }
        }
      }

      return index;
    }

    private static bool IsInterestingPrefabName(string name) {
      return name.IndexOf("destruct_split", StringComparison.OrdinalIgnoreCase) >= 0
        || name.IndexOf("destruct_shell", StringComparison.OrdinalIgnoreCase) >= 0
        || name.EndsWith("_split", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith("_shell", StringComparison.OrdinalIgnoreCase);
    }

    private static string PeekNamedObjectName(FileStream fs, SerializedFileHeader header, SerializedObjectInfo obj, byte[] scratch) {
      long absOff = (long)header.DataOffset + obj.ByteStart;
      int toRead = (int)Math.Min(obj.ByteSize, (uint)scratch.Length);
      if (toRead < 4) return null;
      fs.Seek(absOff, SeekOrigin.Begin);
      if (SerializedFileReader.ReadExact(fs, scratch, 0, toRead) < 4) return null;

      var r = new BinaryStreamReader(scratch, 0, header.BigEndian);
      int slen = r.ReadInt32();
      if (slen <= 0 || slen > 256 || 4 + slen > toRead) return null;
      return System.Text.Encoding.UTF8.GetString(scratch, 4, slen);
    }

    private static void AddToMap(Dictionary<string, List<AssetRef>> map, string key, AssetRef aref) {
      if (!map.TryGetValue(key, out List<AssetRef> list)) {
        list = new List<AssetRef>(1);
        map[key] = list;
      }
      list.Add(aref);
    }

    public AssetRef ResolvePPtr(int sourceFileIndex, PPtr ptr) {
      if (ptr.PathId == 0) return new AssetRef(-1, 0);
      if (ptr.FileId == 0) return new AssetRef(sourceFileIndex, ptr.PathId);

      SerializedFileHeader header = Files[sourceFileIndex];
      int extIdx = ptr.FileId - 1;
      if (extIdx < 0 || extIdx >= header.Externals.Count) return new AssetRef(-1, 0);

      string extPath = header.Externals[extIdx].Replace('\\', '/');
      string extFileName = Path.GetFileName(extPath).ToLowerInvariant();
      if (FileIndexByName.TryGetValue(extFileName, out int targetFileIndex)) {
        return new AssetRef(targetFileIndex, ptr.PathId);
      }

      return new AssetRef(-1, 0);
    }

    public AssetRef ResolvePPtr(SerializedFileHeader externalHeader, PPtr ptr) {
      if (ptr.PathId == 0) return new AssetRef(-1, 0);
      if (ptr.FileId == 0) return new AssetRef(externalHeader.FileIndex, ptr.PathId);

      int extIdx = ptr.FileId - 1;
      if (extIdx < 0 || extIdx >= externalHeader.Externals.Count) return new AssetRef(-1, 0);

      string extPath = externalHeader.Externals[extIdx].Replace('\\', '/');
      string extFileName = Path.GetFileName(extPath).ToLowerInvariant();
      if (FileIndexByName.TryGetValue(extFileName, out int targetFileIndex)) {
        return new AssetRef(targetFileIndex, ptr.PathId);
      }

      return new AssetRef(-1, 0);
    }

    public AssetRef FindMesh(string meshName, string preferredFileName = null, long preferredPathId = 0) {
      if (!string.IsNullOrEmpty(preferredFileName) && preferredPathId != 0) {
        if (FileIndexByName.TryGetValue(preferredFileName.ToLowerInvariant(), out int fi)
            && Files[fi].Objects.TryGetValue(preferredPathId, out SerializedObjectInfo info)
            && info.ClassId == UnityClassId.Mesh) {
          return new AssetRef(fi, preferredPathId);
        }
      }

      if (string.IsNullOrEmpty(meshName)) return new AssetRef(-1, 0);
      if (!MeshesByName.TryGetValue(meshName, out List<AssetRef> candidates) || candidates.Count == 0) {
        return new AssetRef(-1, 0);
      }

      if (!string.IsNullOrEmpty(preferredFileName)) {
        string prefLower = preferredFileName.ToLowerInvariant();
        if (FileIndexByName.TryGetValue(prefLower, out int prefFi)) {
          foreach (var c in candidates) {
            if (c.FileIndex == prefFi && (preferredPathId == 0 || c.PathId == preferredPathId)) {
              return c;
            }
          }
        }
      }

      if (preferredPathId != 0) {
        foreach (var c in candidates) {
          if (c.PathId == preferredPathId) return c;
        }
      }

      return candidates[0];
    }

    public AssetRef FindMaterial(string materialName, string preferredFileName = null) {
      if (string.IsNullOrEmpty(materialName)) return new AssetRef(-1, 0);
      if (!MaterialsByName.TryGetValue(materialName, out List<AssetRef> candidates) || candidates.Count == 0) {
        return new AssetRef(-1, 0);
      }

      if (!string.IsNullOrEmpty(preferredFileName) && FileIndexByName.TryGetValue(preferredFileName.ToLowerInvariant(), out int prefFi)) {
        foreach (var c in candidates) {
          if (c.FileIndex == prefFi) return c;
        }
      }

      return candidates[0];
    }

    public AssetRef FindTexture(string textureName, string preferredFileName = null) {
      if (string.IsNullOrEmpty(textureName)) return new AssetRef(-1, 0);
      if (!TexturesByName.TryGetValue(textureName, out List<AssetRef> candidates) || candidates.Count == 0) {
        return new AssetRef(-1, 0);
      }

      if (!string.IsNullOrEmpty(preferredFileName) && FileIndexByName.TryGetValue(preferredFileName.ToLowerInvariant(), out int prefFi)) {
        foreach (var c in candidates) {
          if (c.FileIndex == prefFi) return c;
        }
      }

      return candidates[0];
    }

    public AssetRef FindGameObject(string name, string preferredFileName = null) {
      if (string.IsNullOrEmpty(name)) return new AssetRef(-1, 0);
      if (!GameObjectsByName.TryGetValue(name, out List<AssetRef> candidates) || candidates.Count == 0) {
        return new AssetRef(-1, 0);
      }

      if (!string.IsNullOrEmpty(preferredFileName) && FileIndexByName.TryGetValue(preferredFileName.ToLowerInvariant(), out int prefFi)) {
        foreach (var c in candidates) {
          if (c.FileIndex == prefFi) return c;
        }
      }

      return candidates[0];
    }

    public RawMeshData DecodeMesh(AssetRef aref) {
      if (aref.IsNull || aref.FileIndex >= Files.Count) return null;
      SerializedFileHeader header = Files[aref.FileIndex];
      if (!header.Objects.TryGetValue(aref.PathId, out SerializedObjectInfo obj) || obj.ClassId != UnityClassId.Mesh) {
        return null;
      }
      byte[] bytes = SerializedFileReader.ReadObjectBytes(header, obj);
      return MeshDecoder.Decode(bytes, header.BigEndian, aref, DataDirectory);
    }

    public RawTexture2DData DecodeTexture2D(AssetRef aref) {
      if (aref.IsNull || aref.FileIndex >= Files.Count) return null;
      SerializedFileHeader header = Files[aref.FileIndex];
      if (!header.Objects.TryGetValue(aref.PathId, out SerializedObjectInfo obj) || obj.ClassId != UnityClassId.Texture2D) {
        return null;
      }
      byte[] bytes = SerializedFileReader.ReadObjectBytes(header, obj);
      return Texture2DDecoder.Decode(bytes, header.BigEndian, aref, DataDirectory);
    }

    public RawMaterialData DecodeMaterial(AssetRef aref) {
      if (aref.IsNull || aref.FileIndex >= Files.Count) return null;
      SerializedFileHeader header = Files[aref.FileIndex];
      if (!header.Objects.TryGetValue(aref.PathId, out SerializedObjectInfo obj) || obj.ClassId != UnityClassId.Material) {
        return null;
      }
      byte[] bytes = SerializedFileReader.ReadObjectBytes(header, obj);
      return MaterialDecoder.Decode(
        bytes,
        header.BigEndian,
        aref,
        ptr => ResolvePPtr(aref.FileIndex, ptr),
        shRef => ShaderNamesByRef.TryGetValue(shRef, out string name) ? name : null);
    }

    public RawGameObjectNode DecodeHierarchy(AssetRef rootGameObjectRef) {
      if (rootGameObjectRef.IsNull || rootGameObjectRef.FileIndex >= Files.Count) return null;
      SerializedFileHeader header = Files[rootGameObjectRef.FileIndex];
      using (FileStream fs = new FileStream(header.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        return DecodeGameObjectNode(fs, header, rootGameObjectRef.PathId, maxDepth: 16);
      }
    }

    private RawGameObjectNode DecodeGameObjectNode(FileStream fs, SerializedFileHeader header, long goPathId, int maxDepth) {
      if (maxDepth <= 0 || !header.Objects.TryGetValue(goPathId, out SerializedObjectInfo goObj) || goObj.ClassId != UnityClassId.GameObject) {
        return null;
      }

      RawGameObjectInfo goInfo = HierarchyDecoder.DecodeGameObject(SerializedFileReader.ReadObjectBytes(fs, header, goObj), header.BigEndian);
      var node = new RawGameObjectNode {
        Name = goInfo.Name,
        Layer = goInfo.Layer,
        IsActive = goInfo.IsActive,
        GameObjectRef = new AssetRef(header.FileIndex, goPathId),
        LocalScale = new Float3(1f, 1f, 1f),
        LocalRotation = new Float4(0f, 0f, 0f, 1f)
      };

      List<PPtr> childTransforms = null;

      foreach (PPtr compPtr in goInfo.Components) {
        if (compPtr.FileId != 0 || !header.Objects.TryGetValue(compPtr.PathId, out SerializedObjectInfo compObj)) continue;

        switch (compObj.ClassId) {
          case UnityClassId.Transform: {
            RawTransformInfo tInfo = HierarchyDecoder.DecodeTransform(SerializedFileReader.ReadObjectBytes(fs, header, compObj), header.BigEndian);
            node.TransformRef = new AssetRef(header.FileIndex, compObj.PathId);
            node.LocalPosition = tInfo.LocalPosition;
            node.LocalRotation = tInfo.LocalRotation;
            node.LocalScale = tInfo.LocalScale;
            childTransforms = tInfo.Children;
            break;
          }
          case UnityClassId.MeshFilter: {
            PPtr mPtr = HierarchyDecoder.DecodeMeshFilterMesh(SerializedFileReader.ReadObjectBytes(fs, header, compObj), header.BigEndian);
            node.MeshRef = ResolvePPtr(header.FileIndex, mPtr);
            if (!node.MeshRef.IsNull && AssetNamesByRef.TryGetValue(node.MeshRef, out string mName)) {
              node.MeshName = mName;
            }
            break;
          }
          case UnityClassId.MeshRenderer: {
            List<PPtr> matPtrs = HierarchyDecoder.DecodeMeshRendererMaterials(SerializedFileReader.ReadObjectBytes(fs, header, compObj), header.BigEndian);
            foreach (PPtr mp in matPtrs) {
              AssetRef mRef = ResolvePPtr(header.FileIndex, mp);
              node.MaterialRefs.Add(mRef);
              node.MaterialNames.Add(!mRef.IsNull && AssetNamesByRef.TryGetValue(mRef, out string mn) ? mn : null);
            }
            break;
          }
          case UnityClassId.BoxCollider: {
            node.BoxCollider = HierarchyDecoder.DecodeBoxCollider(SerializedFileReader.ReadObjectBytes(fs, header, compObj), header.BigEndian);
            break;
          }
          case UnityClassId.MeshCollider: {
            node.MeshCollider = HierarchyDecoder.DecodeMeshCollider(SerializedFileReader.ReadObjectBytes(fs, header, compObj), header.BigEndian, p => ResolvePPtr(header.FileIndex, p));
            break;
          }
          case UnityClassId.Rigidbody: {
            node.Rigidbody = HierarchyDecoder.DecodeRigidbody(SerializedFileReader.ReadObjectBytes(fs, header, compObj), header.BigEndian);
            break;
          }
        }
      }

      if (childTransforms != null) {
        foreach (PPtr childTPtr in childTransforms) {
          if (childTPtr.FileId != 0 || !header.Objects.TryGetValue(childTPtr.PathId, out SerializedObjectInfo childTObj) || childTObj.ClassId != UnityClassId.Transform) continue;
          RawTransformInfo ctInfo = HierarchyDecoder.DecodeTransform(SerializedFileReader.ReadObjectBytes(fs, header, childTObj), header.BigEndian);
          if (ctInfo.GameObject.FileId == 0 && ctInfo.GameObject.PathId != 0) {
            RawGameObjectNode childNode = DecodeGameObjectNode(fs, header, ctInfo.GameObject.PathId, maxDepth - 1);
            if (childNode != null) node.Children.Add(childNode);
          }
        }
      }

      return node;
    }

    private static void SaveCache(GameAssetIndex index, string cacheFilePath, string fingerprint) {
      string dir = Path.GetDirectoryName(cacheFilePath);
      if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
        Directory.CreateDirectory(dir);
      }

      using (FileStream fs = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
      using (BinaryWriter bw = new BinaryWriter(fs)) {
        bw.Write(CacheMagic);
        bw.Write(CacheFormatVersion);
        bw.Write(fingerprint);

        bw.Write(index.Files.Count);
        foreach (var f in index.Files) {
          bw.Write(f.FileName);
          bw.Write(f.DataOffset);
          bw.Write(f.BigEndian);
          bw.Write(f.Externals.Count);
          foreach (string ext in f.Externals) bw.Write(ext);
          bw.Write(f.ObjectList.Count);
          foreach (var o in f.ObjectList) {
            bw.Write(o.PathId);
            bw.Write(o.ByteStart);
            bw.Write(o.ByteSize);
            bw.Write(o.ClassId);
          }
        }

        WriteNameMap(bw, index.MeshesByName);
        WriteNameMap(bw, index.MaterialsByName);
        WriteNameMap(bw, index.TexturesByName);
        WriteNameMap(bw, index.GameObjectsByName);

        bw.Write(index.ShaderNamesByRef.Count);
        foreach (var kv in index.ShaderNamesByRef) {
          bw.Write(kv.Key.FileIndex);
          bw.Write(kv.Key.PathId);
          bw.Write(kv.Value);
        }
      }
    }

    private static void WriteNameMap(BinaryWriter bw, Dictionary<string, List<AssetRef>> map) {
      bw.Write(map.Count);
      foreach (var kv in map) {
        bw.Write(kv.Key);
        bw.Write(kv.Value.Count);
        foreach (var r in kv.Value) {
          bw.Write(r.FileIndex);
          bw.Write(r.PathId);
        }
      }
    }

    private static GameAssetIndex TryLoadCache(string dataDir, string cacheFilePath, string expectedFingerprint, List<string> candidatePaths) {
      var pathByName = candidatePaths.ToDictionary(p => Path.GetFileName(p).ToLowerInvariant(), p => p, StringComparer.OrdinalIgnoreCase);

      using (FileStream fs = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
      using (BinaryReader br = new BinaryReader(fs)) {
        if (br.ReadUInt32() != CacheMagic) return null;
        if (br.ReadInt32() != CacheFormatVersion) return null;
        string fp = br.ReadString();
        if (!string.Equals(fp, expectedFingerprint, StringComparison.Ordinal)) return null;

        var index = new GameAssetIndex(dataDir);
        int fileCount = br.ReadInt32();
        for (int fi = 0; fi < fileCount; fi++) {
          string fileName = br.ReadString();
          if (!pathByName.TryGetValue(fileName, out string fullPath)) return null;

          var header = new SerializedFileHeader {
            FilePath = fullPath,
            FileName = fileName,
            FileIndex = fi,
            DataOffset = br.ReadUInt32(),
            BigEndian = br.ReadBoolean()
          };
          int extCount = br.ReadInt32();
          for (int e = 0; e < extCount; e++) header.Externals.Add(br.ReadString());

          int objCount = br.ReadInt32();
          header.Objects = new Dictionary<long, SerializedObjectInfo>(objCount);
          header.ObjectList = new List<SerializedObjectInfo>(objCount);
          for (int oi = 0; oi < objCount; oi++) {
            var info = new SerializedObjectInfo {
              PathId = br.ReadInt64(),
              ByteStart = br.ReadUInt32(),
              ByteSize = br.ReadUInt32(),
              ClassId = br.ReadInt32()
            };
            header.Objects[info.PathId] = info;
            header.ObjectList.Add(info);
          }

          index.Files.Add(header);
          index.FileIndexByName[fileName] = fi;
        }

        ReadNameMap(br, index.MeshesByName, index.AssetNamesByRef);
        ReadNameMap(br, index.MaterialsByName, index.AssetNamesByRef);
        ReadNameMap(br, index.TexturesByName, index.AssetNamesByRef);
        ReadNameMap(br, index.GameObjectsByName, null);

        int shCount = br.ReadInt32();
        for (int i = 0; i < shCount; i++) {
          int fi = br.ReadInt32();
          long pid = br.ReadInt64();
          string nm = br.ReadString();
          index.ShaderNamesByRef[new AssetRef(fi, pid)] = nm;
        }

        return index;
      }
    }

    private static void ReadNameMap(BinaryReader br, Dictionary<string, List<AssetRef>> map, Dictionary<AssetRef, string> reverseMap) {
      int count = br.ReadInt32();
      for (int i = 0; i < count; i++) {
        string key = br.ReadString();
        int listLen = br.ReadInt32();
        var list = new List<AssetRef>(listLen);
        for (int j = 0; j < listLen; j++) {
          var aref = new AssetRef(br.ReadInt32(), br.ReadInt64());
          list.Add(aref);
          if (reverseMap != null) reverseMap[aref] = key;
        }
        map[key] = list;
      }
    }
  }
}
