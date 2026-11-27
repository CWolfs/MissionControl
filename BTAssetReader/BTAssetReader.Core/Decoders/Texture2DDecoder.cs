using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.Decoders {
  public static class Texture2DDecoder {
    public static RawTexture2DData Decode(byte[] data, bool bigEndian, AssetRef assetRef, string dataDir) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      var tex = new RawTexture2DData {
        Ref = assetRef,
        Name = r.ReadAlignedString()
      };

      r.ReadInt32(); // m_ForcedFallbackFormat
      r.ReadBool();  // m_DownscaleFallback
      r.Align4();

      tex.Width = r.ReadInt32();
      tex.Height = r.ReadInt32();
      tex.CompleteImageSize = r.ReadInt32();
      tex.TextureFormat = r.ReadInt32();
      tex.MipCount = r.ReadInt32();
      tex.IsReadable = r.ReadBool();
      r.ReadBool(); // m_StreamingMipmaps
      r.Align4();

      r.ReadInt32(); // m_StreamingMipmapsPriority
      r.ReadInt32(); // m_ImageCount
      r.ReadInt32(); // m_TextureDimension

      // GLTextureSettings
      tex.FilterMode = r.ReadInt32();
      tex.Aniso = r.ReadInt32();
      tex.MipBias = r.ReadSingle();
      tex.WrapU = r.ReadInt32();
      tex.WrapV = r.ReadInt32();
      tex.WrapW = r.ReadInt32();

      r.ReadInt32(); // m_LightmapFormat
      tex.ColorSpace = r.ReadInt32(); // 0 = Linear, 1 = sRGB

      int inlineLen = r.ReadInt32();
      byte[] inlineData = r.ReadBytes(inlineLen);
      r.Align4();

      if (inlineData.Length > 0) {
        tex.RawBytes = inlineData;
      } else if (r.Position + 12 <= r.Length) {
        uint streamOffset = r.ReadUInt32();
        uint streamSize = r.ReadUInt32();
        string streamPath = r.ReadAlignedString();
        if (streamSize > 0 && !string.IsNullOrEmpty(streamPath)) {
          tex.RawBytes = SerializedFileReader.ReadStreamData(dataDir, streamPath, streamOffset, streamSize);
        } else {
          tex.RawBytes = System.Array.Empty<byte>();
        }
      } else {
        tex.RawBytes = System.Array.Empty<byte>();
      }

      return tex;
    }
  }
}
