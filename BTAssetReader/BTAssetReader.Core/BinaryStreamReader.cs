using System;
using System.IO;
using System.Text;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core {
  public sealed class BinaryStreamReader {
    private readonly byte[] _buffer;
    private int _pos;
    private readonly bool _bigEndian;

    public int Position {
      get => _pos;
      set => _pos = value;
    }

    public int Length => _buffer.Length;
    public bool BigEndian => _bigEndian;

    public BinaryStreamReader(byte[] buffer, int offset = 0, bool bigEndian = false) {
      _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
      _pos = offset;
      _bigEndian = bigEndian;
    }

    public void Align4() {
      int rem = _pos & 3;
      if (rem != 0) _pos += 4 - rem;
    }

    public void Skip(int count) {
      _pos += count;
    }

    public byte ReadByte() => _buffer[_pos++];
    public bool ReadBool() => _buffer[_pos++] != 0;

    public byte[] ReadBytes(int count) {
      if (count <= 0) return Array.Empty<byte>();
      byte[] result = new byte[count];
      Buffer.BlockCopy(_buffer, _pos, result, 0, count);
      _pos += count;
      return result;
    }

    public short ReadInt16() {
      short v = _bigEndian
        ? (short)((_buffer[_pos] << 8) | _buffer[_pos + 1])
        : (short)(_buffer[_pos] | (_buffer[_pos + 1] << 8));
      _pos += 2;
      return v;
    }

    public ushort ReadUInt16() {
      ushort v = _bigEndian
        ? (ushort)((_buffer[_pos] << 8) | _buffer[_pos + 1])
        : (ushort)(_buffer[_pos] | (_buffer[_pos + 1] << 8));
      _pos += 2;
      return v;
    }

    public int ReadInt32() {
      int v = _bigEndian
        ? (_buffer[_pos] << 24) | (_buffer[_pos + 1] << 16) | (_buffer[_pos + 2] << 8) | _buffer[_pos + 3]
        : _buffer[_pos] | (_buffer[_pos + 1] << 8) | (_buffer[_pos + 2] << 16) | (_buffer[_pos + 3] << 24);
      _pos += 4;
      return v;
    }

    public int ReadInt32BE() {
      int v = (_buffer[_pos] << 24) | (_buffer[_pos + 1] << 16) | (_buffer[_pos + 2] << 8) | _buffer[_pos + 3];
      _pos += 4;
      return v;
    }

    public uint ReadUInt32() => (uint)ReadInt32();
    public uint ReadUInt32BE() => (uint)ReadInt32BE();

    public long ReadInt64() {
      long v;
      if (_bigEndian) {
        uint hi = ReadUInt32();
        uint lo = ReadUInt32();
        v = ((long)hi << 32) | lo;
      } else {
        uint lo = ReadUInt32();
        uint hi = ReadUInt32();
        v = ((long)hi << 32) | lo;
      }
      return v;
    }

    public float ReadSingle() {
      if (!_bigEndian && BitConverter.IsLittleEndian) {
        float v = BitConverter.ToSingle(_buffer, _pos);
        _pos += 4;
        return v;
      }
      byte[] tmp = new byte[4];
      tmp[0] = _buffer[_pos + 3];
      tmp[1] = _buffer[_pos + 2];
      tmp[2] = _buffer[_pos + 1];
      tmp[3] = _buffer[_pos];
      _pos += 4;
      return BitConverter.ToSingle(tmp, 0);
    }

    public Float2 ReadFloat2() => new Float2(ReadSingle(), ReadSingle());
    public Float3 ReadFloat3() => new Float3(ReadSingle(), ReadSingle(), ReadSingle());
    public Float4 ReadFloat4() => new Float4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

    public RawBounds ReadBounds() {
      return new RawBounds {
        Center = ReadFloat3(),
        Extents = ReadFloat3()
      };
    }

    public PPtr ReadPPtr() {
      int fileId = ReadInt32();
      long pathId = ReadInt64();
      return new PPtr(fileId, pathId);
    }

    public string ReadCString() {
      int start = _pos;
      while (_pos < _buffer.Length && _buffer[_pos] != 0) {
        _pos++;
      }
      string s = Encoding.UTF8.GetString(_buffer, start, _pos - start);
      if (_pos < _buffer.Length) _pos++; // skip null terminator
      return s;
    }

    public string ReadAlignedString() {
      int len = ReadInt32();
      if (len <= 0) return string.Empty;
      if (len > _buffer.Length - _pos) {
        throw new InvalidDataException($"String length {len} exceeds buffer bounds at offset {_pos}.");
      }
      string s = Encoding.UTF8.GetString(_buffer, _pos, len);
      _pos += len;
      Align4();
      return s;
    }

    public static float HalfToSingle(ushort h) {
      uint sign = (uint)(h & 0x8000) << 16;
      int exp = (h >> 10) & 0x1F;
      int mant = h & 0x03FF;

      uint bits;
      if (exp == 0) {
        if (mant == 0) {
          bits = sign;
        } else {
          // Subnormal
          exp = 1;
          while ((mant & 0x0400) == 0) {
            mant <<= 1;
            exp--;
          }
          mant &= 0x03FF;
          bits = sign | (uint)((exp + 112) << 23) | (uint)(mant << 13);
        }
      } else if (exp == 31) {
        bits = sign | 0x7F800000u | (uint)(mant << 13);
      } else {
        bits = sign | (uint)((exp + 112) << 23) | (uint)(mant << 13);
      }

      byte[] b = BitConverter.GetBytes(bits);
      return BitConverter.ToSingle(b, 0);
    }
  }
}
