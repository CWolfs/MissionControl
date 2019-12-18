using Newtonsoft.Json;

namespace MissionControl.Data {
	public class EncounterLayer {
    [JsonProperty("EncounterLayerID")]
    public string EncounterLayerId { get; set; }

    [JsonProperty("MapID")]
    public string MapId { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("FriendlyName")]
    public string FriendlyName { get; set; }

    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonProperty("BattleValue")]
    public string BattleValue { get; set; }

    [JsonProperty("ContractTypeID")]
    public string ContractTypeId { get; set; }

    [JsonProperty("EncounterLayerGUID")]
    public string EncounterLayerGuid { get; set; }

    [JsonProperty("TagSetID")]
    public string TagSetId { get; set; }

    [JsonProperty("IncludeInBuild")]
    public string IncludeInBuild { get; set; }
	}
}
