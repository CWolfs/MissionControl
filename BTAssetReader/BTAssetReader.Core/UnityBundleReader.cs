using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BTAssetReader.Core.Decoders;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core {
  /// <summary>
  /// Pure-C# Unity 2018.4 UnityFS AssetBundle reader (uncompressed, LZ4, LZ4HC) with on-demand block
  /// decompression so large BattleTech bundles (e.g. chrprfmech_*base-001) can be inspected and decoded
  /// without decompressing untouched blocks.
  /// </summary>
  public sealed class UnityBundleReader : IDisposable {
    public struct StorageBlock {
      public uint UncompressedSize;
      public uint CompressedSize;
      public ushort Flags;
      public long CompressedOffset;
      public long UncompressedOffset;
    }

    public struct DirectoryEntry {
      public string Path;
      public long Offset;
      public long Size;
      public uint Flags;
    }

    private readonly FileStream _fs;
    private readonly List<StorageBlock> _blocks = new List<StorageBlock>();
    private readonly List<DirectoryEntry> _entries = new List<DirectoryEntry>();
    private readonly Dictionary<int, byte[]> _blockCache = new Dictionary<int, byte[]>();

    public string BundlePath { get; }
    public IReadOnlyList<DirectoryEntry> Entries => _entries;

    private UnityBundleReader(string path, FileStream fs) {
      BundlePath = path;
      _fs = fs;
    }

    public static UnityBundleReader Open(string bundlePath) {
      var fs = new FileStream(bundlePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      try {
        var reader = new UnityBundleReader(bundlePath, fs);
        reader.ReadHeader();
        return reader;
      } catch {
        fs.Dispose();
        throw;
      }
    }

    private void ReadHeader() {
      string sig = ReadCString(_fs);
      if (sig != "UnityFS") {
        throw new InvalidDataException($"Unsupported bundle signature '{sig}' in '{BundlePath}'.");
      }

      uint version = ReadUInt32BE(_fs);
      string playerVer = ReadCString(_fs);
      string engineVer = ReadCString(_fs);
      long totalSize = ReadInt64BE(_fs);
      uint compInfoSize = ReadUInt32BE(_fs);
      uint uncompInfoSize = ReadUInt32BE(_fs);
      uint flags = ReadUInt32BE(_fs);

      long afterHeader = _fs.Position;
      bool infoAtEnd = (flags & 0x80u) != 0;
      uint compType = flags & 0x3Fu;

      byte[] compInfo = new byte[compInfoSize];
      if (infoAtEnd) {
        _fs.Seek(_fs.Length - compInfoSize, SeekOrigin.Begin);
      }
      if (SerializedFileReader.ReadExact(_fs, compInfo, 0, (int)compInfoSize) != (int)compInfoSize) {
        throw new EndOfStreamException($"Truncated UnityFS blocksInfo in '{BundlePath}'.");
      }

      long dataStart = infoAtEnd ? afterHeader : _fs.Position;
      byte[] infoBytes = DecompressBlock(compInfo, (int)uncompInfoSize, compType);

      var br = new BinaryStreamReader(infoBytes, 0, bigEndian: true);
      br.Skip(16); // uncompressedDataHash

      int blockCount = br.ReadInt32();
      long compCursor = dataStart;
      long uncompCursor = 0;
      for (int i = 0; i < blockCount; i++) {
        uint uSize = br.ReadUInt32();
        uint cSize = br.ReadUInt32();
        ushort bFlags = br.ReadUInt16();
        _blocks.Add(new StorageBlock {
          UncompressedSize = uSize,
          CompressedSize = cSize,
          Flags = bFlags,
          CompressedOffset = compCursor,
          UncompressedOffset = uncompCursor
        });
        compCursor += cSize;
        uncompCursor += uSize;
      }

      int nodeCount = br.ReadInt32();
      for (int i = 0; i < nodeCount; i++) {
        long offset = br.ReadInt64();
        long size = br.ReadInt64();
        uint nFlags = br.ReadUInt32();
        string path = br.ReadCString();
        _entries.Add(new DirectoryEntry {
          Path = path,
          Offset = offset,
          Size = size,
          Flags = nFlags
        });
      }
    }

    public DirectoryEntry? FindEntry(string nameOrSuffix) {
      string target = Path.GetFileName(nameOrSuffix.Replace('\\', '/'));
      foreach (var e in _entries) {
        if (string.Equals(e.Path, nameOrSuffix, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(e.Path), target, StringComparison.OrdinalIgnoreCase)) {
          return e;
        }
      }
      return null;
    }

    public DirectoryEntry? FindPrimarySerializedFile() {
      foreach (var e in _entries) {
        if ((e.Flags & 4u) != 0 && !e.Path.EndsWith(".resS", StringComparison.OrdinalIgnoreCase) && !e.Path.EndsWith(".resource", StringComparison.OrdinalIgnoreCase)) {
          return e;
        }
      }
      foreach (var e in _entries) {
        if (!e.Path.EndsWith(".resS", StringComparison.OrdinalIgnoreCase) && !e.Path.EndsWith(".resource", StringComparison.OrdinalIgnoreCase)) {
          return e;
        }
      }
      return null;
    }

    public Stream OpenEntryStream(DirectoryEntry entry) => new VirtualEntryStream(this, entry.Offset, entry.Size);

    public byte[] ReadStreamResource(string streamPath, uint offset, uint size) {
      if (size == 0 || string.IsNullOrEmpty(streamPath)) return Array.Empty<byte>();
      DirectoryEntry? entry = FindEntry(streamPath);
      if (!entry.HasValue) {
        throw new FileNotFoundException($"Stream resource '{streamPath}' not found inside bundle '{BundlePath}'.");
      }
      byte[] buf = new byte[size];
      ReadUncompressedRange(entry.Value.Offset + offset, buf, 0, (int)size);
      return buf;
    }

    internal int ReadUncompressedRange(long uncompressedPos, byte[] dst, int dstOffset, int count) {
      if (count <= 0 || _blocks.Count == 0) return 0;

      // Binary search first block containing uncompressedPos
      int lo = 0, hi = _blocks.Count - 1;
      while (lo < hi) {
        int mid = (lo + hi + 1) >> 1;
        if (_blocks[mid].UncompressedOffset <= uncompressedPos) lo = mid;
        else hi = mid - 1;
      }

      int totalRead = 0;
      int blkIdx = lo;
      long cursor = uncompressedPos;

      while (totalRead < count && blkIdx < _blocks.Count) {
        StorageBlock blk = _blocks[blkIdx];
        long localOff = cursor - blk.UncompressedOffset;
        if (localOff < 0 || localOff >= blk.UncompressedSize) break;

        byte[] blkData = GetBlockData(blkIdx);
        int avail = (int)Math.Min(blkData.Length - localOff, count - totalRead);
        Buffer.BlockCopy(blkData, (int)localOff, dst, dstOffset + totalRead, avail);
        totalRead += avail;
        cursor += avail;
        blkIdx++;
      }

      return totalRead;
    }

    private byte[] GetBlockData(int blockIdx) {
      if (_blockCache.TryGetValue(blockIdx, out byte[] cached)) return cached;

      StorageBlock blk = _blocks[blockIdx];
      byte[] comp = new byte[blk.CompressedSize];
      _fs.Seek(blk.CompressedOffset, SeekOrigin.Begin);
      if (SerializedFileReader.ReadExact(_fs, comp, 0, (int)blk.CompressedSize) != (int)blk.CompressedSize) {
        throw new EndOfStreamException($"Truncated block {blockIdx} in '{BundlePath}'.");
      }

      byte[] raw = DecompressBlock(comp, (int)blk.UncompressedSize, (uint)(blk.Flags & 0x3Fu));
      // Keep cache bounded so reading many large bundles never balloons memory
      if (_blockCache.Count > 128) _blockCache.Clear();
      _blockCache[blockIdx] = raw;
      return raw;
    }

    private static byte[] DecompressBlock(byte[] src, int uncompressedSize, uint compType) {
      if (compType == 0) return src;
      if (compType == 1) {
        return LzmaDecoder.Decompress(src, uncompressedSize);
      }
      if (compType == 2 || compType == 3) {
        return DecompressLz4Block(src, uncompressedSize);
      }
      throw new NotSupportedException($"Unsupported UnityFS compression type {compType}.");
    }

    private static byte[] DecompressLz4Block(byte[] src, int dstLen) {
      byte[] dst = new byte[dstLen];
      int sp = 0, dp = 0;
      int sEnd = src.Length;

      while (sp < sEnd) {
        int token = src[sp++];
        int litLen = token >> 4;
        if (litLen == 15) {
          byte b;
          do {
            b = src[sp++];
            litLen += b;
          } while (b == 255);
        }

        if (litLen > 0) {
          Buffer.BlockCopy(src, sp, dst, dp, litLen);
          sp += litLen;
          dp += litLen;
        }

        if (sp >= sEnd) break;

        int offset = src[sp] | (src[sp + 1] << 8);
        sp += 2;
        if (offset <= 0) throw new InvalidDataException("Invalid LZ4 match offset 0.");

        int matchLen = (token & 0x0F) + 4;
        if ((token & 0x0F) == 15) {
          byte b;
          do {
            b = src[sp++];
            matchLen += b;
          } while (b == 255);
        }

        int mPos = dp - offset;
        for (int i = 0; i < matchLen; i++) {
          dst[dp++] = dst[mPos++];
        }
      }

      return dst;
    }

    private static string ReadCString(Stream s) {
      var bytes = new List<byte>(32);
      int b;
      while ((b = s.ReadByte()) > 0) bytes.Add((byte)b);
      return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static uint ReadUInt32BE(Stream s) {
      byte[] b = new byte[4];
      SerializedFileReader.ReadExact(s, b, 0, 4);
      return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static long ReadInt64BE(Stream s) {
      byte[] b = new byte[8];
      SerializedFileReader.ReadExact(s, b, 0, 8);
      return ((long)b[0] << 56) | ((long)b[1] << 48) | ((long)b[2] << 40) | ((long)b[3] << 32)
           | ((long)b[4] << 24) | ((long)b[5] << 16) | ((long)b[6] << 8) | b[7];
    }

    public void Dispose() {
      _blockCache.Clear();
      _fs?.Dispose();
    }

    private sealed class VirtualEntryStream : Stream {
      private readonly UnityBundleReader _owner;
      private readonly long _baseOffset;
      private readonly long _length;
      private long _pos;

      public VirtualEntryStream(UnityBundleReader owner, long baseOffset, long length) {
        _owner = owner;
        _baseOffset = baseOffset;
        _length = length;
      }

      public override bool CanRead => true;
      public override bool CanSeek => true;
      public override bool CanWrite => false;
      public override long Length => _length;
      public override long Position {
        get => _pos;
        set => _pos = Math.Max(0, Math.Min(_length, value));
      }

      public override int Read(byte[] buffer, int offset, int count) {
        if (_pos >= _length || count <= 0) return 0;
        int toRead = (int)Math.Min(count, _length - _pos);
        int read = _owner.ReadUncompressedRange(_baseOffset + _pos, buffer, offset, toRead);
        _pos += read;
        return read;
      }

      public override long Seek(long offset, SeekOrigin origin) {
        long target = origin switch {
          SeekOrigin.Begin => offset,
          SeekOrigin.Current => _pos + offset,
          SeekOrigin.End => _length + offset,
          _ => _pos
        };
        _pos = Math.Max(0, Math.Min(_length, target));
        return _pos;
      }

      public override void Flush() { }
      public override void SetLength(long value) => throw new NotSupportedException();
      public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
  }
}
