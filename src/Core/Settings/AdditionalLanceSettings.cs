using System.Collections.Generic;

using Newtonsoft.Json;

using System.Linq;

namespace MissionControl.Config {
  public class AdditionalLanceSettings : AdvancedSettings {
    [JsonProperty("IsPrimaryObjectiveIn")]
    public List<string> IsPrimaryObjectiveIn { get; set; } = new List<string>() { "SimpleBattle" };

    [JsonProperty("HideObjective")]
    public bool HideObjective { get; set; } = false;

    [JsonProperty("ForceDisplayOfHiddenObjectiveIfPrimary")]
    public bool ForceDisplayOfHiddenObjectiveIfPrimary { get; set; } = false;

    [JsonProperty("UseElites")]
    public bool UseElites { get; set; } = true;

    [JsonProperty("UseDialogue")]
    public bool UseDialogue { get; set; } = true;

    [JsonProperty("SkullValueMatters")]
    public bool SkullValueMatters { get; set; } = true;

    [JsonProperty("BasedOnVisibleSkullValue")]
    public bool BasedOnVisibleSkullValue { get; set; } = true;

    [JsonProperty("UseGeneralProfileForSkirmish")]
    public bool UseGeneralProfileForSkirmish { get; set; } = true;

    [JsonProperty("DisableIfFlashpointContract")]
    public bool DisableIfFlashpointContract { get; set; } = true;

    [JsonProperty("DisableWhenMaxTonnage")]
    public DisableWhenMaxTonnage DisableWhenMaxTonnage { get; set; } = new DisableWhenMaxTonnage();

    [JsonProperty("MatchAllyLanceCountToEnemy")]
    public bool MatchAllyLanceCountToEnemy { get; set; } = false;

    [JsonProperty("DropWeightInfluence")]
    public DropWeightInfluenceSettings DropWeightInfluenceSettings { get; set; } = new DropWeightInfluenceSettings();
  }
}