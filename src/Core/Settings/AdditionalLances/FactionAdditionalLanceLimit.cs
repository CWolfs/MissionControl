using System;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class FactionAdditionalLanceLimit {
    [JsonProperty("Min")]
    public int? Min { get; set; }

    [JsonProperty("Max")]
    public int? Max { get; set; }

    [JsonProperty("MaxMod")]
    public int? MaxMod { get; set; }

    [JsonProperty("OnlyWhenExtendedLancesActive")]
    public bool? OnlyWhenExtendedLancesActive { get; set; }

    [JsonProperty("OnlyWhenLargerThanOpposingLance")]
    public bool? OnlyWhenLargerThanOpposingLance { get; set; }

    [JsonProperty("OnlyWhenLargerThanPlayerDrop")]
    public bool? OnlyWhenLargerThanPlayerDrop { get; set; }

    [JsonIgnore]
    public bool HasLimits => Min.HasValue || Max.HasValue || MaxMod.HasValue;
  }

  public class FactionAdditionalLanceOverride : FactionAdditionalLanceLimit {
    [JsonProperty("Enemy")]
    public FactionAdditionalLanceLimit Enemy { get; set; }

    [JsonProperty("Allies")]
    public FactionAdditionalLanceLimit Allies { get; set; }

    public FactionAdditionalLanceLimit ResolveForTeam(string teamType) {
      FactionAdditionalLanceLimit teamOverride = null;

      if (string.Equals(teamType, "enemy", StringComparison.OrdinalIgnoreCase)) {
        teamOverride = Enemy;
      } else if (string.Equals(teamType, "allies", StringComparison.OrdinalIgnoreCase)) {
        teamOverride = Allies;
      }

      if (teamOverride == null && !HasLimits) {
        return null;
      }

      FactionAdditionalLanceLimit resolved = new FactionAdditionalLanceLimit {
        Min = teamOverride?.Min ?? Min,
        Max = teamOverride?.Max ?? Max,
        MaxMod = teamOverride?.MaxMod ?? MaxMod,
        OnlyWhenExtendedLancesActive = teamOverride?.OnlyWhenExtendedLancesActive ?? OnlyWhenExtendedLancesActive,
        OnlyWhenLargerThanOpposingLance = teamOverride?.OnlyWhenLargerThanOpposingLance ?? OnlyWhenLargerThanOpposingLance,
        OnlyWhenLargerThanPlayerDrop = teamOverride?.OnlyWhenLargerThanPlayerDrop ?? OnlyWhenLargerThanPlayerDrop
      };

      return resolved.HasLimits ? resolved : null;
    }
  }
}
