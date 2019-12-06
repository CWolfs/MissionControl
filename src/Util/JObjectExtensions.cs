using System.Collections.Generic;

using Newtonsoft.Json.Linq;

public static class JObjectExtensions {
  public static bool ContainsKey(this JObject jObject, string key) {
    IDictionary<string, JToken> dictionary = jObject;
    if (dictionary.ContainsKey(key)) return true;
    return false;
  }
}