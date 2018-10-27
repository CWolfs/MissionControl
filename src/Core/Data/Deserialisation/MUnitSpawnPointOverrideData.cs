using System.Collections;

using Newtonsoft.Json;

using HBS.Collections;

namespace MissionControl.Data {
  public class MUnitSpawnPointOverrideData {
    [JsonProperty("unitType")]
    public string UnitType { get; set; } = "Mech";

    [JsonProperty("unitDefId")]
    public string UnitDefId { get; set; } = "UseLance";

    [JsonProperty("unitTagSet")]
    public MTagSetData UnitTagSet { get; set; } = new MTagSetData();

    [JsonProperty("unitExcludedTagSet")]
    public MTagSetData UnitExcludedTagSet { get; set; } = new MTagSetData();

    [JsonProperty("spawnEffectTags")]
    public MTagSetData SpawnEffectTags { get; set; } = new MTagSetData();

    [JsonProperty("pilotDefId")]
    public string PilotDefId { get; set; } = "pilotDef_InheritLance";

    [JsonProperty("pilotTagSet")]
    public MTagSetData PilotTagSet { get; set; } = new MTagSetData();

    [JsonProperty("pilotExcludedTagSet")]
    public MTagSetData PilotExcludedTagSet { get; set; } = new MTagSetData();
  }
}