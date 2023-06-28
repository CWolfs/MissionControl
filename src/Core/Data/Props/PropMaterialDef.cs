using Newtonsoft.Json;

namespace MissionControl.Data {
  public class PropMaterialDef {
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Texture")]
    public string Texture { get; set; }

    [JsonProperty("Shader")]
    public string Shader { get; set; }
  }
}