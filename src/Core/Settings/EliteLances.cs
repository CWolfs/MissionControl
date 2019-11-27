using System.Collections.Generic;

using Newtonsoft.Json;

using BattleTech;

namespace MissionControl.Config {
  public class EliteLances {
    [JsonProperty("Conditions")]
    public List<string> Conditions { get; set; } = new List<string>();

    [JsonProperty("Suffix")]
    public string Suffix { get; set; } = "_ELITE";

    [JsonProperty("Overrides")]
    public Dictionary<string, string> Overrides { get; set; } = new Dictionary<string, string>();

    public bool ShouldEliteLancesBeSelected(FactionDef faction) {
      if (!Main.Settings.AdditionalLanceSettings.UseElites) return false;
      if (MissionControl.Instance.IsSkirmish()) return false;
      bool useEliteLances = true;

      if (useEliteLances && Conditions.Contains("IsEnemy")) {
        useEliteLances = UnityGameInstance.Instance.Game.Simulation.IsFactionEnemy(faction.FactionValue);
      }

      if (useEliteLances && Conditions.Contains("IsAlly")) {
        useEliteLances = UnityGameInstance.Instance.Game.Simulation.IsFactionAlly(faction.FactionValue);
      }

      return useEliteLances;
    }
  }
}