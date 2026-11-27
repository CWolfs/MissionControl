using System;
using System.IO;

namespace BTAssetReader.Core {
  /// <summary>
  /// Pure-C# LZMA1 block decoder for UnityFS compression type 1 (standard Unity AssetBundle LZMA:
  /// 5-byte properties header [props byte + 4-byte LE dictSize] followed by range-coded LZMA stream).
  /// Zero external dependencies; compatible with net471 and net8.0.
  /// </summary>
  internal static class LzmaDecoder {
    private const int NumPosBitsMax = 4;
    private const int NumStates = 12;
    private const int NumLenToPosStates = 4;
    private const int NumAlignBits = 4;
    private const int StartPosModelIndex = 4;
    private const int EndPosModelIndex = 14;
    private const int NumFullDistances = 1 << (EndPosModelIndex >> 1);
    private const int NumLowLenBits = 3;
    private const int NumMidLenBits = 3;
    private const int NumHighLenBits = 8;
    private const int NumLowLenSymbols = 1 << NumLowLenBits;
    private const int NumMidLenSymbols = 1 << NumMidLenBits;
    private const int MatchMinLen = 2;
    private const uint TopMask = 0x01000000u;
    private const int BitModelTotalBits = 11;
    private const uint BitModelTotal = 1u << BitModelTotalBits;
    private const int NumMoveBits = 5;

    public static byte[] Decompress(byte[] src, int uncompressedSize) {
      if (src == null || src.Length < 10) {
        throw new InvalidDataException("LZMA block is too small.");
      }

      byte props = src[0];
      if (props >= 9 * 5 * 5) {
        throw new InvalidDataException($"Invalid LZMA properties byte 0x{props:X2}.");
      }

      int lc = props % 9;
      int rem = props / 9;
      int lp = rem % 5;
      int pb = rem / 5;

      int dictSize = src[1] | (src[2] << 8) | (src[3] << 16) | (src[4] << 24);
      if (dictSize < 4096) dictSize = 4096;

      byte[] dst = new byte[uncompressedSize];
      var rd = new RangeDec(src, 5);

      ushort[] isMatch = InitProbs(NumStates << NumPosBitsMax);
      ushort[] isRep = InitProbs(NumStates);
      ushort[] isRepG0 = InitProbs(NumStates);
      ushort[] isRepG1 = InitProbs(NumStates);
      ushort[] isRepG2 = InitProbs(NumStates);
      ushort[] isRep0Long = InitProbs(NumStates << NumPosBitsMax);

      var posSlotDecoders = new BitTreeDec[NumLenToPosStates];
      for (int i = 0; i < NumLenToPosStates; i++) posSlotDecoders[i] = new BitTreeDec(6);

      ushort[] posDecoders = InitProbs(NumFullDistances - EndPosModelIndex);
      var posAlignDecoder = new BitTreeDec(NumAlignBits);

      var lenDecoder = new LenDec(1 << pb);
      var repLenDecoder = new LenDec(1 << pb);

      int numLitStates = 1 << (lc + lp);
      ushort[][] litProbs = new ushort[numLitStates][];
      for (int i = 0; i < numLitStates; i++) litProbs[i] = InitProbs(0x300);

      int posStateMask = (1 << pb) - 1;
      int litPosMask = (1 << lp) - 1;

      int state = 0;
      uint rep0 = 0, rep1 = 0, rep2 = 0, rep3 = 0;
      int outPos = 0;
      byte prevByte = 0;

      while (outPos < uncompressedSize) {
        int posState = outPos & posStateMask;
        int matchIdx = (state << NumPosBitsMax) + posState;

        if (rd.DecodeBit(ref isMatch[matchIdx]) == 0) {
          // Literal
          int litIdx = ((outPos & litPosMask) << lc) + (prevByte >> (8 - lc));
          ushort[] probs = litProbs[litIdx];
          int symbol = 1;
          if (state >= 7) {
            byte matchByte = (outPos >= (int)(rep0 + 1)) ? dst[outPos - (int)(rep0 + 1)] : (byte)0;
            do {
              int matchBit = (matchByte >> 7) & 1;
              matchByte <<= 1;
              int bit = rd.DecodeBit(ref probs[((1 + matchBit) << 8) + symbol]);
              symbol = (symbol << 1) | bit;
              if (matchBit != bit) {
                while (symbol < 0x100) {
                  symbol = (symbol << 1) | rd.DecodeBit(ref probs[symbol]);
                }
                break;
              }
            } while (symbol < 0x100);
          } else {
            do {
              symbol = (symbol << 1) | rd.DecodeBit(ref probs[symbol]);
            } while (symbol < 0x100);
          }

          prevByte = (byte)symbol;
          dst[outPos++] = prevByte;
          state = state < 4 ? 0 : (state < 10 ? state - 3 : state - 6);
        } else {
          int len;
          if (rd.DecodeBit(ref isRep[state]) == 1) {
            if (rd.DecodeBit(ref isRepG0[state]) == 0) {
              if (rd.DecodeBit(ref isRep0Long[matchIdx]) == 0) {
                state = state < 7 ? 9 : 11;
                prevByte = dst[outPos - (int)(rep0 + 1)];
                dst[outPos++] = prevByte;
                continue;
              }
            } else {
              uint dist;
              if (rd.DecodeBit(ref isRepG1[state]) == 0) {
                dist = rep1;
              } else {
                if (rd.DecodeBit(ref isRepG2[state]) == 0) {
                  dist = rep2;
                } else {
                  dist = rep3;
                  rep3 = rep2;
                }
                rep2 = rep1;
              }
              rep1 = rep0;
              rep0 = dist;
            }
            len = repLenDecoder.Decode(ref rd, posState) + MatchMinLen;
            state = state < 7 ? 8 : 11;
          } else {
            rep3 = rep2;
            rep2 = rep1;
            rep1 = rep0;
            len = lenDecoder.Decode(ref rd, posState) + MatchMinLen;
            state = state < 7 ? 7 : 10;

            int lenToPosState = len - MatchMinLen;
            if (lenToPosState >= NumLenToPosStates) lenToPosState = NumLenToPosStates - 1;
            int posSlot = posSlotDecoders[lenToPosState].Decode(ref rd);

            if (posSlot >= StartPosModelIndex) {
              int numDirectBits = (posSlot >> 1) - 1;
              rep0 = (uint)((2 | (posSlot & 1)) << numDirectBits);
              if (posSlot < EndPosModelIndex) {
                rep0 += BitTreeDec.ReverseDecode(posDecoders, (int)(rep0 - posSlot - 1), ref rd, numDirectBits);
              } else {
                rep0 += rd.DecodeDirectBits(numDirectBits - NumAlignBits) << NumAlignBits;
                rep0 += posAlignDecoder.ReverseDecode(ref rd);
                if (rep0 == 0xFFFFFFFFu) {
                  // End-of-stream marker
                  break;
                }
              }
            } else {
              rep0 = (uint)posSlot;
            }
          }

          int distBack = (int)rep0 + 1;
          if (distBack > outPos) {
            throw new InvalidDataException($"LZMA back-reference {distBack} exceeds output position {outPos}.");
          }
          for (int i = 0; i < len && outPos < uncompressedSize; i++) {
            prevByte = dst[outPos - distBack];
            dst[outPos++] = prevByte;
          }
        }
      }

      return dst;
    }

    private static ushort[] InitProbs(int count) {
      ushort[] arr = new ushort[count];
      for (int i = 0; i < count; i++) arr[i] = (ushort)(BitModelTotal >> 1);
      return arr;
    }

    private struct RangeDec {
      private readonly byte[] _src;
      private int _pos;
      private uint _range;
      private uint _code;

      public RangeDec(byte[] src, int startOffset) {
        _src = src;
        _pos = startOffset;
        _range = 0xFFFFFFFFu;
        _code = 0;
        for (int i = 0; i < 5; i++) {
          _code = (_code << 8) | ReadByteSafe();
        }
      }

      private byte ReadByteSafe() => _pos < _src.Length ? _src[_pos++] : (byte)0;

      public int DecodeBit(ref ushort prob) {
        uint bound = (_range >> BitModelTotalBits) * prob;
        int bit;
        if (_code < bound) {
          _range = bound;
          prob = (ushort)(prob + ((BitModelTotal - prob) >> NumMoveBits));
          bit = 0;
        } else {
          _range -= bound;
          _code -= bound;
          prob = (ushort)(prob - (prob >> NumMoveBits));
          bit = 1;
        }
        if (_range < TopMask) {
          _code = (_code << 8) | ReadByteSafe();
          _range <<= 8;
        }
        return bit;
      }

      public uint DecodeDirectBits(int numBits) {
        uint res = 0;
        for (int i = numBits; i > 0; i--) {
          _range >>= 1;
          uint t = (_code - _range) >> 31;
          _code -= _range & (t - 1);
          res = (res << 1) | (1u - t);
          if (_range < TopMask) {
            _code = (_code << 8) | ReadByteSafe();
            _range <<= 8;
          }
        }
        return res;
      }
    }

    private sealed class BitTreeDec {
      private readonly ushort[] _models;
      private readonly int _numBits;

      public BitTreeDec(int numBits) {
        _numBits = numBits;
        _models = InitProbs(1 << numBits);
      }

      public int Decode(ref RangeDec rd) {
        int m = 1;
        for (int i = _numBits; i > 0; i--) {
          m = (m << 1) + rd.DecodeBit(ref _models[m]);
        }
        return m - (1 << _numBits);
      }

      public uint ReverseDecode(ref RangeDec rd) {
        int m = 1;
        uint sym = 0;
        for (int i = 0; i < _numBits; i++) {
          int bit = rd.DecodeBit(ref _models[m]);
          m = (m << 1) + bit;
          sym |= (uint)(bit << i);
        }
        return sym;
      }

      public static uint ReverseDecode(ushort[] models, int startIndex, ref RangeDec rd, int numBits) {
        int m = 1;
        uint sym = 0;
        for (int i = 0; i < numBits; i++) {
          int bit = rd.DecodeBit(ref models[startIndex + m]);
          m = (m << 1) + bit;
          sym |= (uint)(bit << i);
        }
        return sym;
      }
    }

    private sealed class LenDec {
      private ushort _choice = (ushort)(BitModelTotal >> 1);
      private ushort _choice2 = (ushort)(BitModelTotal >> 1);
      private readonly BitTreeDec[] _low;
      private readonly BitTreeDec[] _mid;
      private readonly BitTreeDec _high = new BitTreeDec(NumHighLenBits);

      public LenDec(int numPosStates) {
        _low = new BitTreeDec[numPosStates];
        _mid = new BitTreeDec[numPosStates];
        for (int i = 0; i < numPosStates; i++) {
          _low[i] = new BitTreeDec(NumLowLenBits);
          _mid[i] = new BitTreeDec(NumMidLenBits);
        }
      }

      public int Decode(ref RangeDec rd, int posState) {
        if (rd.DecodeBit(ref _choice) == 0) {
          return _low[posState].Decode(ref rd);
        }
        if (rd.DecodeBit(ref _choice2) == 0) {
          return NumLowLenSymbols + _mid[posState].Decode(ref rd);
        }
        return NumLowLenSymbols + NumMidLenSymbols + _high.Decode(ref rd);
      }
    }
  }
}
