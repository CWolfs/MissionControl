using System;
using System.IO;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.Decoders {
  public static class MeshDecoder {
    private struct ChannelInfo {
      public byte Stream;
      public byte Offset;
      public byte Format;
      public byte Dimension; // low 4 bits
    }

    public static RawMeshData Decode(byte[] data, bool bigEndian, AssetRef assetRef, string dataDir) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      var mesh = new RawMeshData {
        Ref = assetRef,
        Name = r.ReadAlignedString()
      };

      // m_SubMeshes
      int subMeshCount = r.ReadInt32();
      mesh.SubMeshes = new RawSubMesh[subMeshCount];
      for (int i = 0; i < subMeshCount; i++) {
        mesh.SubMeshes[i] = new RawSubMesh {
          FirstByte = r.ReadUInt32(),
          IndexCount = r.ReadUInt32(),
          Topology = r.ReadInt32(),
          BaseVertex = r.ReadUInt32(),
          FirstVertex = r.ReadUInt32(),
          VertexCount = r.ReadUInt32(),
          LocalAabb = r.ReadBounds()
        };
      }

      // m_Shapes (BlendShapeData)
      int shapeVertCount = r.ReadInt32();
      r.Skip(shapeVertCount * 40); // index(4) + vertex(12) + normal(12) + tangent(12)
      int shapeCount = r.ReadInt32();
      r.Skip(shapeCount * 12); // firstVertex(4) + vertexCount(4) + hasNormals(1) + hasTangents(1) + pad(2)
      int shapeChannelCount = r.ReadInt32();
      for (int i = 0; i < shapeChannelCount; i++) {
        r.ReadAlignedString();
        r.Skip(12); // nameHash(4) + frameIndex(4) + frameCount(4)
      }
      int fullWeightCount = r.ReadInt32();
      r.Skip(fullWeightCount * 4);

      // m_BindPose, m_BoneNameHashes, m_RootBoneNameHash
      int bindPoseCount = r.ReadInt32();
      r.Skip(bindPoseCount * 64);
      int boneHashCount = r.ReadInt32();
      r.Skip(boneHashCount * 4);
      r.ReadUInt32(); // m_RootBoneNameHash

      // Flags
      mesh.MeshCompression = r.ReadByte();
      mesh.OriginalIsReadable = r.ReadBool();
      r.ReadByte(); // m_KeepVertices
      r.ReadByte(); // m_KeepIndices

      mesh.IndexFormat = r.ReadInt32(); // 0 = UInt16, 1 = UInt32

      // m_IndexBuffer
      int indexBufferLen = r.ReadInt32();
      byte[] indexBuffer = r.ReadBytes(indexBufferLen);
      r.Align4();

      // m_VertexData (Unity 2018.4)
      uint vertexCount = r.ReadUInt32();
      mesh.VertexCount = (int)vertexCount;
      int channelCount = r.ReadInt32();
      var channels = new ChannelInfo[channelCount];
      for (int i = 0; i < channelCount; i++) {
        channels[i] = new ChannelInfo {
          Stream = r.ReadByte(),
          Offset = r.ReadByte(),
          Format = r.ReadByte(),
          Dimension = (byte)(r.ReadByte() & 0x0F)
        };
      }
      int vertexDataLen = r.ReadInt32();
      byte[] vertexData = r.ReadBytes(vertexDataLen);
      r.Align4();

      // Skip m_CompressedMesh so we can read m_LocalAABB and m_StreamData
      SkipCompressedMesh(r);

      mesh.LocalAabb = r.ReadBounds();
      r.ReadInt32(); // m_MeshUsageFlags

      int convexLen = r.ReadInt32();
      r.Skip(convexLen);
      r.Align4();

      int triLen = r.ReadInt32();
      r.Skip(triLen);
      r.Align4();

      r.Skip(8); // m_MeshMetrics[0], m_MeshMetrics[1]

      // m_StreamData (present if vertexData was empty and streamed in .resS)
      if (r.Position + 12 <= r.Length) {
        uint streamOffset = r.ReadUInt32();
        uint streamSize = r.ReadUInt32();
        string streamPath = r.ReadAlignedString();
        if (vertexData.Length == 0 && streamSize > 0 && !string.IsNullOrEmpty(streamPath)) {
          vertexData = SerializedFileReader.ReadStreamData(dataDir, streamPath, streamOffset, streamSize);
        }
      }

      if (mesh.MeshCompression != 0) {
        throw new NotSupportedException($"Compressed mesh '{mesh.Name}' (compression={mesh.MeshCompression}) is not supported.");
      }

      DecodeVertexChannels(mesh, channels, vertexData, bigEndian);
      DecodeSubMeshIndices(mesh, indexBuffer, bigEndian);

      return mesh;
    }

    private static void SkipCompressedMesh(BinaryStreamReader r) {
      SkipPackedFloatVector(r); // m_Vertices
      SkipPackedFloatVector(r); // m_UV
      SkipPackedFloatVector(r); // m_Normals
      SkipPackedFloatVector(r); // m_Tangents
      SkipPackedIntVector(r);   // m_Weights
      SkipPackedIntVector(r);   // m_NormalSigns
      SkipPackedIntVector(r);   // m_TangentSigns
      SkipPackedFloatVector(r); // m_FloatColors
      SkipPackedIntVector(r);   // m_BoneIndices
      SkipPackedIntVector(r);   // m_Triangles
      r.ReadUInt32();           // m_UVInfo
    }

    private static void SkipPackedFloatVector(BinaryStreamReader r) {
      r.ReadUInt32(); // m_NumItems
      r.ReadSingle(); // m_Range
      r.ReadSingle(); // m_Start
      int dataLen = r.ReadInt32();
      r.Skip(dataLen);
      r.Align4();
      r.ReadByte();   // m_BitSize
      r.Align4();
    }

    private static void SkipPackedIntVector(BinaryStreamReader r) {
      r.ReadUInt32(); // m_NumItems
      int dataLen = r.ReadInt32();
      r.Skip(dataLen);
      r.Align4();
      r.ReadByte();   // m_BitSize
      r.Align4();
    }

    private static int GetFormatByteSize(byte format) {
      switch (format) {
        case 0: return 4; // Float32
        case 1: return 2; // Float16
        case 2: return 1; // UNorm8
        case 3: return 1; // SNorm8
        case 4: return 2; // UNorm16
        case 5: return 2; // SNorm16
        case 6: return 1; // UInt8
        case 7: return 1; // SInt8
        case 8: return 2; // UInt16
        case 9: return 2; // SInt16
        case 10: return 4; // UInt32
        case 11: return 4; // SInt32
        default: return 4;
      }
    }

    private static void DecodeVertexChannels(RawMeshData mesh, ChannelInfo[] channels, byte[] vertexData, bool bigEndian) {
      int vCount = mesh.VertexCount;
      if (vCount <= 0 || vertexData.Length == 0) return;

      // Determine stream strides & stream base offsets
      int maxStream = 0;
      for (int i = 0; i < channels.Length; i++) {
        if (channels[i].Dimension > 0 && channels[i].Stream > maxStream) {
          maxStream = channels[i].Stream;
        }
      }

      int[] streamStride = new int[maxStream + 1];
      for (int i = 0; i < channels.Length; i++) {
        var ch = channels[i];
        if (ch.Dimension == 0) continue;
        int end = ch.Offset + ch.Dimension * GetFormatByteSize(ch.Format);
        if (end > streamStride[ch.Stream]) streamStride[ch.Stream] = end;
      }

      int[] streamBase = new int[maxStream + 1];
      int running = 0;
      for (int s = 0; s <= maxStream; s++) {
        streamBase[s] = running;
        int streamLen = streamStride[s] * vCount;
        // Unity aligns stream boundaries to 16 bytes
        running += (streamLen + 15) & ~15;
      }

      float ReadComponent(int offset, byte format) {
        switch (format) {
          case 0: // Float32
            if (!bigEndian && BitConverter.IsLittleEndian) return BitConverter.ToSingle(vertexData, offset);
            return new BinaryStreamReader(vertexData, offset, bigEndian).ReadSingle();
          case 1: // Float16
            ushort h = (ushort)(vertexData[offset] | (vertexData[offset + 1] << 8));
            return BinaryStreamReader.HalfToSingle(h);
          case 2: // UNorm8
            return vertexData[offset] / 255f;
          case 3: // SNorm8
            return Math.Max(-1f, ((sbyte)vertexData[offset]) / 127f);
          case 4: // UNorm16
            return (ushort)(vertexData[offset] | (vertexData[offset + 1] << 8)) / 65535f;
          case 5: // SNorm16
            return Math.Max(-1f, ((short)(vertexData[offset] | (vertexData[offset + 1] << 8))) / 32767f);
          default:
            return 0f;
        }
      }

      for (int ci = 0; ci < channels.Length; ci++) {
        var ch = channels[ci];
        if (ch.Dimension == 0) continue;

        int baseOff = streamBase[ch.Stream] + ch.Offset;
        int stride = streamStride[ch.Stream];
        int elemSize = GetFormatByteSize(ch.Format);

        if (baseOff + (long)(vCount - 1) * stride + ch.Dimension * elemSize > vertexData.Length) {
          continue;
        }

        switch (ci) {
          case 0: // Position
            mesh.Vertices = new Float3[vCount];
            for (int v = 0; v < vCount; v++) {
              int o = baseOff + v * stride;
              mesh.Vertices[v] = new Float3(
                ReadComponent(o, ch.Format),
                ReadComponent(o + elemSize, ch.Format),
                ReadComponent(o + elemSize * 2, ch.Format));
            }
            break;

          case 1: // Normal
            mesh.Normals = new Float3[vCount];
            for (int v = 0; v < vCount; v++) {
              int o = baseOff + v * stride;
              mesh.Normals[v] = new Float3(
                ReadComponent(o, ch.Format),
                ReadComponent(o + elemSize, ch.Format),
                ReadComponent(o + elemSize * 2, ch.Format));
            }
            break;

          case 2: // Tangent
            mesh.Tangents = new Float4[vCount];
            for (int v = 0; v < vCount; v++) {
              int o = baseOff + v * stride;
              mesh.Tangents[v] = new Float4(
                ReadComponent(o, ch.Format),
                ReadComponent(o + elemSize, ch.Format),
                ReadComponent(o + elemSize * 2, ch.Format),
                ch.Dimension >= 4 ? ReadComponent(o + elemSize * 3, ch.Format) : 1f);
            }
            break;

          case 3: // Color
            mesh.Colors = new ColorRgba32[vCount];
            if (ch.Format == 2 && ch.Dimension == 4) {
              for (int v = 0; v < vCount; v++) {
                int o = baseOff + v * stride;
                mesh.Colors[v] = new ColorRgba32(vertexData[o], vertexData[o + 1], vertexData[o + 2], vertexData[o + 3]);
              }
            } else {
              for (int v = 0; v < vCount; v++) {
                int o = baseOff + v * stride;
                byte cr = (byte)Math.Min(255, Math.Max(0, (int)(ReadComponent(o, ch.Format) * 255f)));
                byte cg = (byte)Math.Min(255, Math.Max(0, (int)(ReadComponent(o + elemSize, ch.Format) * 255f)));
                byte cb = (byte)Math.Min(255, Math.Max(0, (int)(ReadComponent(o + elemSize * 2, ch.Format) * 255f)));
                byte ca = ch.Dimension >= 4 ? (byte)Math.Min(255, Math.Max(0, (int)(ReadComponent(o + elemSize * 3, ch.Format) * 255f))) : (byte)255;
                mesh.Colors[v] = new ColorRgba32(cr, cg, cb, ca);
              }
            }
            break;

          case 4: // UV0
            mesh.Uv0 = ReadUvChannel(vCount, baseOff, stride, elemSize, ch.Format, ReadComponent);
            break;

          case 5: // UV1
            mesh.Uv1 = ReadUvChannel(vCount, baseOff, stride, elemSize, ch.Format, ReadComponent);
            break;

          case 6: // UV2
            mesh.Uv2 = ReadUvChannel(vCount, baseOff, stride, elemSize, ch.Format, ReadComponent);
            break;
        }
      }
    }

    private static Float2[] ReadUvChannel(int vCount, int baseOff, int stride, int elemSize, byte format, Func<int, byte, float> readComp) {
      var uvs = new Float2[vCount];
      for (int v = 0; v < vCount; v++) {
        int o = baseOff + v * stride;
        uvs[v] = new Float2(readComp(o, format), readComp(o + elemSize, format));
      }
      return uvs;
    }

    private static void DecodeSubMeshIndices(RawMeshData mesh, byte[] indexBuffer, bool bigEndian) {
      int subCount = mesh.SubMeshes.Length;
      mesh.SubMeshIndices = new int[subCount][];

      for (int s = 0; s < subCount; s++) {
        var sm = mesh.SubMeshes[s];
        int count = (int)sm.IndexCount;
        int[] indices = new int[count];
        int byteOff = (int)sm.FirstByte;

        if (mesh.IndexFormat == 0) {
          // 16-bit indices
          for (int i = 0; i < count; i++) {
            int o = byteOff + i * 2;
            ushort idx = bigEndian
              ? (ushort)((indexBuffer[o] << 8) | indexBuffer[o + 1])
              : (ushort)(indexBuffer[o] | (indexBuffer[o + 1] << 8));
            indices[i] = idx + (int)sm.BaseVertex;
          }
        } else {
          // 32-bit indices
          for (int i = 0; i < count; i++) {
            int o = byteOff + i * 4;
            int idx = bigEndian
              ? (indexBuffer[o] << 24) | (indexBuffer[o + 1] << 16) | (indexBuffer[o + 2] << 8) | indexBuffer[o + 3]
              : indexBuffer[o] | (indexBuffer[o + 1] << 8) | (indexBuffer[o + 2] << 16) | (indexBuffer[o + 3] << 24);
            indices[i] = idx + (int)sm.BaseVertex;
          }
        }

        mesh.SubMeshIndices[s] = indices;
      }
    }
  }
}
