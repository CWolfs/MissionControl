using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BTAssetReader.Core.CustomModels {
  /// <summary>
  /// Lightweight, zero-dependency JSON parser for glTF 2.0 JSON chunks across net471 and net8.0.
  /// Returns Dictionary&lt;string, object&gt;, List&lt;object&gt;, string, double, bool, or null.
  /// </summary>
  internal sealed class MiniJson {
    private readonly string _src;
    private int _pos;

    private MiniJson(string src) {
      _src = src ?? string.Empty;
      _pos = 0;
    }

    public static object Parse(string json) {
      var parser = new MiniJson(json);
      return parser.ParseValue();
    }

    private void SkipWs() {
      while (_pos < _src.Length) {
        char c = _src[_pos];
        if (c == ' ' || c == '\t' || c == '\r' || c == '\n') _pos++;
        else break;
      }
    }

    private object ParseValue() {
      SkipWs();
      if (_pos >= _src.Length) return null;
      char c = _src[_pos];
      if (c == '{') return ParseObject();
      if (c == '[') return ParseArray();
      if (c == '"') return ParseString();
      if (c == 't') { _pos += 4; return true; }
      if (c == 'f') { _pos += 5; return false; }
      if (c == 'n') { _pos += 4; return null; }
      return ParseNumber();
    }

    private Dictionary<string, object> ParseObject() {
      var dict = new Dictionary<string, object>(StringComparer.Ordinal);
      _pos++; // '{'
      SkipWs();
      if (_pos < _src.Length && _src[_pos] == '}') { _pos++; return dict; }
      while (_pos < _src.Length) {
        SkipWs();
        string key = ParseString();
        SkipWs();
        if (_pos < _src.Length && _src[_pos] == ':') _pos++;
        object val = ParseValue();
        dict[key] = val;
        SkipWs();
        if (_pos < _src.Length && _src[_pos] == ',') { _pos++; continue; }
        if (_pos < _src.Length && _src[_pos] == '}') { _pos++; break; }
        break;
      }
      return dict;
    }

    private List<object> ParseArray() {
      var list = new List<object>();
      _pos++; // '['
      SkipWs();
      if (_pos < _src.Length && _src[_pos] == ']') { _pos++; return list; }
      while (_pos < _src.Length) {
        list.Add(ParseValue());
        SkipWs();
        if (_pos < _src.Length && _src[_pos] == ',') { _pos++; continue; }
        if (_pos < _src.Length && _src[_pos] == ']') { _pos++; break; }
        break;
      }
      return list;
    }

    private string ParseString() {
      if (_pos >= _src.Length || _src[_pos] != '"') return string.Empty;
      _pos++; // '"'
      var sb = new StringBuilder();
      while (_pos < _src.Length) {
        char c = _src[_pos++];
        if (c == '"') break;
        if (c == '\\' && _pos < _src.Length) {
          char esc = _src[_pos++];
          switch (esc) {
            case '"': sb.Append('"'); break;
            case '\\': sb.Append('\\'); break;
            case '/': sb.Append('/'); break;
            case 'b': sb.Append('\b'); break;
            case 'f': sb.Append('\f'); break;
            case 'n': sb.Append('\n'); break;
            case 'r': sb.Append('\r'); break;
            case 't': sb.Append('\t'); break;
            case 'u':
              if (_pos + 4 <= _src.Length) {
                string hex = _src.Substring(_pos, 4);
                if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code)) {
                  sb.Append((char)code);
                }
                _pos += 4;
              }
              break;
            default: sb.Append(esc); break;
          }
        } else {
          sb.Append(c);
        }
      }
      return sb.ToString();
    }

    private double ParseNumber() {
      int start = _pos;
      while (_pos < _src.Length) {
        char c = _src[_pos];
        if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') _pos++;
        else break;
      }
      string token = _src.Substring(start, _pos - start);
      return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0;
    }

    // Typed helpers
    public static Dictionary<string, object> AsDict(object o) => o as Dictionary<string, object>;
    public static List<object> AsList(object o) => o as List<object>;
    public static string GetString(Dictionary<string, object> d, string key, string def = null)
      => d != null && d.TryGetValue(key, out object v) && v is string s ? s : def;
    public static int GetInt(Dictionary<string, object> d, string key, int def = 0)
      => d != null && d.TryGetValue(key, out object v) && v is double n ? (int)n : def;
    public static float GetFloat(Dictionary<string, object> d, string key, float def = 0f)
      => d != null && d.TryGetValue(key, out object v) && v is double n ? (float)n : def;
    public static bool GetBool(Dictionary<string, object> d, string key, bool def = false)
      => d != null && d.TryGetValue(key, out object v) && v is bool b ? b : def;
    public static Dictionary<string, object> GetDict(Dictionary<string, object> d, string key)
      => d != null && d.TryGetValue(key, out object v) ? v as Dictionary<string, object> : null;
    public static List<object> GetList(Dictionary<string, object> d, string key)
      => d != null && d.TryGetValue(key, out object v) ? v as List<object> : null;
  }
}
