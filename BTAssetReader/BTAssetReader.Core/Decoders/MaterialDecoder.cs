using System;
using System.Text;
using System.Text.RegularExpressions;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.Decoders {
  public static class ShaderNameDecoder {
    private static readonly Regex ShaderPrefixRegex = new Regex(
      @"(?:BattleTech|Standard|Hidden|Unlit|Legacy Shaders|UI|Sprites|Particles|Custom|HBS|Urban|Nature|FX|Mobile|Skybox|GUI|TextMeshPro|TMPro|Bink|Autodesk|VertexLit)[/A-Za-z0-9 _().-]{0,80}",
      RegexOptions.Compiled);

    /// <summary>
    /// Extracts the Unity shader name (e.g. "BattleTech Standard", "Urban/Urban Building")
    /// from a Unity 2018.4 SerializedShader blob without unpacking compiled shader bytecode.
    /// </summary>
    public static string ExtractShaderName(byte[] data, bool bigEndian) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      string topName = r.ReadAlignedString();
      if (!string.IsNullOrEmpty(topName)) return topName;

      // In Unity 2018.4 SerializedShader, m_ParsedForm.m_Name is a length-prefixed ASCII string.
      string ascii = Encoding.ASCII.GetString(data);
      foreach (Match m in ShaderPrefixRegex.Matches(ascii)) {
        int sOff = m.Index;
        if (sOff < 4) continue;
        int slen = bigEndian
          ? (data[sOff - 4] << 24) | (data[sOff - 3] << 16) | (data[sOff - 2] << 8) | data[sOff - 1]
          : data[sOff - 4] | (data[sOff - 3] << 8) | (data[sOff - 2] << 16) | (data[sOff - 1] << 24);
        if (slen == m.Length) {
          string candidate = m.Value;
          if (!candidate.EndsWith("GUI", StringComparison.Ordinal) && !candidate.EndsWith("Editor", StringComparison.Ordinal)) {
            return candidate;
          }
        }
      }

      return null;
    }
  }

  public static class MaterialDecoder {
    public static RawMaterialData Decode(byte[] data, bool bigEndian, AssetRef assetRef, Func<PPtr, AssetRef> resolvePPtr, Func<AssetRef, string> resolveShaderName) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      var mat = new RawMaterialData {
        Ref = assetRef,
        Name = r.ReadAlignedString()
      };

      PPtr shaderPtr = r.ReadPPtr();
      mat.ShaderRef = resolvePPtr(shaderPtr);
      mat.ShaderName = resolveShaderName != null ? resolveShaderName(mat.ShaderRef) : null;

      string keywordsRaw = r.ReadAlignedString();
      mat.ShaderKeywords = string.IsNullOrWhiteSpace(keywordsRaw)
        ? Array.Empty<string>()
        : keywordsRaw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

      mat.LightmapFlags = r.ReadUInt32();
      mat.EnableInstancing = r.ReadBool();
      mat.DoubleSidedGI = r.ReadBool();
      r.Align4();

      mat.CustomRenderQueue = r.ReadInt32();

      int tagCount = r.ReadInt32();
      for (int i = 0; i < tagCount; i++) {
        string k = r.ReadAlignedString();
        string v = r.ReadAlignedString();
        mat.StringTagMap[k] = v;
      }

      int disabledPassCount = r.ReadInt32();
      for (int i = 0; i < disabledPassCount; i++) {
        r.ReadAlignedString();
      }

      // UnityPropertySheet (m_SavedProperties)
      int texEnvCount = r.ReadInt32();
      for (int i = 0; i < texEnvCount; i++) {
        string propName = r.ReadAlignedString();
        PPtr texPtr = r.ReadPPtr();
        Float2 scale = r.ReadFloat2();
        Float2 offset = r.ReadFloat2();
        mat.TexEnvs.Add(new RawTexEnv {
          PropertyName = propName,
          TextureRef = resolvePPtr(texPtr),
          Scale = scale,
          Offset = offset
        });
      }

      int floatCount = r.ReadInt32();
      for (int i = 0; i < floatCount; i++) {
        string propName = r.ReadAlignedString();
        float val = r.ReadSingle();
        mat.Floats[propName] = val;
      }

      int colorCount = r.ReadInt32();
      for (int i = 0; i < colorCount; i++) {
        string propName = r.ReadAlignedString();
        Float4 val = r.ReadFloat4();
        mat.Colors[propName] = val;
      }

      return mat;
    }
  }
}
