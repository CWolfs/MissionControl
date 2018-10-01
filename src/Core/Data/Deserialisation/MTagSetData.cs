using Newtonsoft.Json;

using HBS.Collections;

namespace MissionControl.Data {
  public class MTagSetData {
    [JsonProperty("items")]
    public string[] Items { get; set; } = new string[0];

    [JsonProperty("tags")]
    public string[] Tags { get; set; } = new string[0];

    [JsonProperty("tagSetSourceFile")]
    public string TagSetSourceFile { get; set; } = "Tags/LanceTags";
  }
}