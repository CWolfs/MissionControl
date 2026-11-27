using System;
using System.Collections.Generic;
using System.IO;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core {
  public struct SerializedObjectInfo {
    public long PathId;
    public uint ByteStart; // relative to DataOffset
    public uint ByteSize;
    public int ClassId;
    public short ScriptTypeIndex;
  }

  public sealed class SerializedFileHeader {
    public string FilePath;
    public string FileName; // lower-case file name
    public int FileIndex;
    public uint MetadataSize;
    public uint FileSize;
    public uint Version;
    public uint DataOffset;
    public bool BigEndian;
    public string UnityVersion;
    public int TargetPlatform;
    public bool EnableTypeTree;
    public List<string> Externals = new List<string>();
    public Dictionary<long, SerializedObjectInfo> Objects = new Dictionary<long, SerializedObjectInfo>();
    public List<SerializedObjectInfo> ObjectList = new List<SerializedObjectInfo>();
  }

  public static class SerializedFileReader {
    /// <summary>
    /// Reads ONLY the header and object table of a Unity 2018.4 (v17) SerializedFile.
    /// Does not load bulk object payloads into memory.
    /// </summary>
    public static SerializedFileHeader ReadHeader(string filePath, int fileIndex) {
      using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        if (fs.Length < 20) return null;

        byte[] prefix = new byte[20];
        if (ReadExact(fs, prefix, 0, 20) != 20) return null;

        var pr = new BinaryStreamReader(prefix, 0, bigEndian: true);
        uint metadataSize = pr.ReadUInt32BE();
        uint fileSize = pr.ReadUInt32BE();
        uint version = pr.ReadUInt32BE();
        uint dataOffset = pr.ReadUInt32BE();
        bool bigEndian = pr.ReadByte() != 0;

        if (version < 15 || version > 22) return null;
        if (metadataSize == 0 || dataOffset > fs.Length) return null;

        // Read metadata section (typically 50 KB - 2 MB, never the full 100 MB payload)
        int metaReadLen = (int)Math.Min(fs.Length - 20, Math.Max((long)metadataSize + 16, (long)dataOffset - 20));
        byte[] metaBuf = new byte[metaReadLen];
        if (ReadExact(fs, metaBuf, 0, metaReadLen) != metaReadLen) return null;

        var r = new BinaryStreamReader(metaBuf, 0, bigEndian);
        var header = new SerializedFileHeader {
          FilePath = filePath,
          FileName = Path.GetFileName(filePath).ToLowerInvariant(),
          FileIndex = fileIndex,
          MetadataSize = metadataSize,
          FileSize = fileSize,
          Version = version,
          DataOffset = dataOffset,
          BigEndian = bigEndian
        };

        header.UnityVersion = r.ReadCString();
        header.TargetPlatform = r.ReadInt32();
        header.EnableTypeTree = r.ReadBool();

        int typeCount = r.ReadInt32();
        var types = new (int ClassId, short ScriptIdx)[typeCount];
        for (int i = 0; i < typeCount; i++) {
          int classId = r.ReadInt32();
          r.ReadByte(); // isStrippedType
          short scriptTypeIndex = r.ReadInt16();
          if (classId == UnityClassId.MonoBehaviour) {
            r.Skip(16); // scriptId
          }
          r.Skip(16); // oldTypeHash
          if (header.EnableTypeTree) {
            int nodeCount = r.ReadInt32();
            int stringBufferSize = r.ReadInt32();
            r.Skip(nodeCount * 24 + stringBufferSize);
          }
          types[i] = (classId, scriptTypeIndex);
        }

        int objCount = r.ReadInt32();
        header.Objects = new Dictionary<long, SerializedObjectInfo>(objCount);
        header.ObjectList = new List<SerializedObjectInfo>(objCount);

        for (int i = 0; i < objCount; i++) {
          r.Align4();
          long pathId = r.ReadInt64();
          uint byteStart = r.ReadUInt32();
          uint byteSize = r.ReadUInt32();
          int typeId = r.ReadInt32();

          var t = types[typeId];
          var info = new SerializedObjectInfo {
            PathId = pathId,
            ByteStart = byteStart,
            ByteSize = byteSize,
            ClassId = t.ClassId,
            ScriptTypeIndex = t.ScriptIdx
          };
          header.Objects[pathId] = info;
          header.ObjectList.Add(info);
        }

        // Script types table
        int scriptCount = r.ReadInt32();
        for (int i = 0; i < scriptCount; i++) {
          r.ReadInt32(); // localSerializedFileIndex
          r.Align4();
          r.ReadInt64(); // localIdentifierInFile
        }

        // Externals table
        int extCount = r.ReadInt32();
        for (int i = 0; i < extCount; i++) {
          r.ReadCString(); // tempEmpty
          r.Skip(16);      // guid
          r.ReadInt32();   // type
          string extPath = r.ReadCString();
          header.Externals.Add(extPath);
        }

        return header;
      }
    }

    public static byte[] ReadObjectBytes(SerializedFileHeader header, SerializedObjectInfo obj) {
      using (FileStream fs = new FileStream(header.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        return ReadObjectBytes(fs, header, obj);
      }
    }

    public static byte[] ReadObjectBytes(FileStream fs, SerializedFileHeader header, SerializedObjectInfo obj) {
      long absOffset = (long)header.DataOffset + obj.ByteStart;
      fs.Seek(absOffset, SeekOrigin.Begin);
      byte[] buf = new byte[obj.ByteSize];
      int read = ReadExact(fs, buf, 0, (int)obj.ByteSize);
      if (read != (int)obj.ByteSize) {
        throw new EndOfStreamException($"Expected {obj.ByteSize} bytes at {absOffset} in '{header.FileName}', got {read}.");
      }
      return buf;
    }

    public static byte[] ReadStreamData(string dataDir, string streamPath, uint offset, uint size) {
      if (size == 0 || string.IsNullOrEmpty(streamPath)) return Array.Empty<byte>();

      string normalized = streamPath.Replace('\\', '/');
      string fileName = Path.GetFileName(normalized);
      string fullPath = Path.Combine(dataDir, fileName);
      if (!File.Exists(fullPath)) {
        // Case-insensitive lookup fallback for Linux/macOS
        foreach (string candidate in Directory.GetFiles(dataDir)) {
          if (string.Equals(Path.GetFileName(candidate), fileName, StringComparison.OrdinalIgnoreCase)) {
            fullPath = candidate;
            break;
          }
        }
      }

      if (!File.Exists(fullPath)) {
        throw new FileNotFoundException($"Streaming resource file not found: '{fullPath}' (from '{streamPath}').");
      }

      using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        fs.Seek(offset, SeekOrigin.Begin);
        byte[] buf = new byte[size];
        int read = ReadExact(fs, buf, 0, (int)size);
        if (read != (int)size) {
          throw new EndOfStreamException($"Expected {size} bytes at offset {offset} in '{fileName}', got {read}.");
        }
        return buf;
      }
    }

    public static int ReadExact(Stream stream, byte[] buffer, int offset, int count) {
      int total = 0;
      while (total < count) {
        int n = stream.Read(buffer, offset + total, count - total);
        if (n <= 0) break;
        total += n;
      }
      return total;
    }
  }
}
