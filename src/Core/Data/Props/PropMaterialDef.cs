using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MissionControl.Data {
  public class PropMaterialDef {
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("MaterialProperties")]
    public JObject MaterialProperties { get; set; }

    [JsonProperty("Shader")]
    public string Shader { get; set; }
  }
}