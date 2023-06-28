using Newtonsoft.Json;

using System.Collections.Generic;

namespace MissionControl.Data {
  public class PropBuildingDef {
    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("MainModel")]
    public string MainModelKey { get; set; }

    [JsonProperty("FlimsyModels")]
    public List<PropFlimsyDef> FlimsyModels { get; set; } = new List<PropFlimsyDef>();
  }
}