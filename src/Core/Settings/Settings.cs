using Newtonsoft.Json;

using System.Collections.Generic;

namespace MissionControl.Config {
  public class Settings {
    [JsonProperty("DebugMode")]
    public bool DebugMode { get; set; } = false;

    [JsonProperty("DebugSkirmishMode")]
    public bool DebugSkirmishMode { get; set; } = false;

    [JsonProperty("DisableIfFlashpointContract")]
    public bool DisableIfFlashpointContract { get; set; } = true;

    [JsonProperty("RandomSpawns")]
    public RandomSpawnsSettings RandomSpawns { get; set; } = new RandomSpawnsSettings();

    [JsonProperty("HotDrop")]
    public HotDrop HotDrop { get; set; } = new HotDrop();

    [JsonProperty("AdditionalLances")]
    public AdditionalLanceSettings AdditionalLanceSettings { get; set; } = new AdditionalLanceSettings();

    [JsonProperty("ExtendedLances")]
    public ExtendedLancesSettings ExtendedLances { get; set; } = new ExtendedLancesSettings();

    [JsonProperty("AdditionalPlayerMechs")]
    public bool AdditionalPlayerMechs { get; set; } = true;

    [JsonProperty("ExtendedBoundaries")]
    public ExtendedBoundariesSettings ExtendedBoundaries { get; set; } = new ExtendedBoundariesSettings();

    [JsonProperty("DynamicWithdraw")]
    public DynamicWithdrawSettings DynamicWithdraw { get; set; } = new DynamicWithdrawSettings();

    public AdditionalLances ActiveAdditionalLances { get; set; }

    [JsonIgnore]
    public Dictionary<int, AdditionalLances> AdditionalLances { get; set; } = new Dictionary<int, AdditionalLances>();

    [JsonProperty("AI")]
    public AiSettings AiSettings { get; set; } = new AiSettings();
  }
}