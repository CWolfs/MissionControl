using System.Collections.Generic;

using Newtonsoft.Json;

using System.Linq;

namespace MissionControl.Config {
  public class AdditionalLanceSettings : AdvancedSettings {
    [JsonProperty("IsPrimaryObjectiveIn")]
    public List<string> IsPrimaryObjectiveIn { get; set; } = new List<string>() { "SimpleBattle" };

    [JsonProperty("HideObjective")]
    public bool HideObjective { get; set; } = true;

    [JsonProperty("ShowObjectiveOnLanceDetected")]
    public bool ShowObjectiveOnLanceDetected { get; set; } = true;

    [JsonProperty("AlwaysDisplayHiddenObjectiveIfPrimary")]
    public bool AlwaysDisplayHiddenObjectiveIfPrimary { get; set; } = false;

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

    [JsonProperty("DisableAllies")]
    public bool DisableAllies { get; set; } = false;

    [JsonProperty("DisableEnemies")]
    public bool DisableEnemies { get; set; } = false;

    public bool IsTeamDisabled(string teamType) {
      if (teamType.ToLower() == "enemy") {
        return DisableEnemies;
      } else if (teamType.ToLower() == "allies") {
        return DisableAllies;
      }

      return false;
    }
  }
}