using Newtonsoft.Json;

using System.Collections.Generic;

namespace MissionControl.Config {
  public class Settings {
    [JsonProperty("DebugMode")]
    public bool DebugMode { get; set; } = false;

    [JsonProperty("Debug")]
    public DebugSettings Debug { get; set; } = new DebugSettings();

    [JsonProperty("ContractTypeLoaderWait")]
    public int ContractTypeLoaderWait { get; set; } = 1000;

    [JsonProperty("VersionCheck")]
    public bool VersionCheck { get; set; } = true;

    [JsonIgnore]
    public string Version { get; set; } = "0.0.0";

    [JsonIgnore]
    public string GithubVersion { get; set; } = "0.0.0";

    [JsonProperty("EnableSkirmishMode")]
    public bool EnableSkirmishMode { get; set; } = true;

    [JsonProperty("DebugSkirmishMode")]
    public bool DebugSkirmishMode { get; set; } = false;

    [JsonProperty("EnableFlashpointOverrides")]
    public bool EnableFlashpointOverrides { get; set; } = false;

    [JsonProperty("EnableAdditionalPlayerMechsForFlashpoints")]
    public bool EnableAdditionalPlayerMechsForFlashpoints { get; set; } = false;

    [JsonProperty("NeverFailSimGameInFlashpoints")]
    public bool NeverFailSimGameInFlashpoints { get; set; } = true;

    [JsonProperty("EnableStoryOverrides")]
    public bool EnableStoryOverrides { get; set; } = false;

    [JsonProperty("EnableAdditionalPlayerMechsForStory")]
    public bool EnableAdditionalPlayerMechsForStory { get; set; } = false;

    [JsonProperty("RandomSpawns")]
    public RandomSpawnsSettings RandomSpawns { get; set; } = new RandomSpawnsSettings();

    [JsonProperty("HotDropProtection")]
    public HotDropProtection HotDropProtection { get; set; } = new HotDropProtection();

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

    [JsonProperty("Spawners")]
    public SpawnerSettings Spawners { get; set; } = new SpawnerSettings();

    [JsonProperty("Misc")]
    public MiscSettings Misc { get; set; } = new MiscSettings();

    public Dictionary<string, ContractSettingsOverrides> ContractSettingsOverrides = new Dictionary<string, ContractSettingsOverrides>();

    public ContractSettingsOverrides ActiveContractSettings { get; set; }
  }
}