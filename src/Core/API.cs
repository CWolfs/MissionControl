using System;
using System.Collections.Generic;

using MissionControl.Data;

namespace MissionControl {
  public class API {
    private Dictionary<string, bool> overriddenAdditionalLances = new Dictionary<string, bool>();
    private Dictionary<string, int> overriddenAdditionalLanceCount = new Dictionary<string, int>();
    private Dictionary<string, List<MLanceOverride>> overriddenAdditionalLanceOverrides = new Dictionary<string, List<MLanceOverride>>();

    /* ENCOUNTER RULES */
    public void AddEncounter(string contractType, Type encounter) {
      MissionControl.Instance.AddEncounter(contractType, encounter);
    }

    public void ClearEncounters() {
      MissionControl.Instance.ClearEncounters();
    }

    public void ClearEncounters(string contractType) {
      MissionControl.Instance.ClearEncounters(contractType);
    }

    /* ADDITIONAL LANCES */
    private void SetOverriddenAdditionalLances(string teamType) {
      overriddenAdditionalLances.Add(teamType, true);
    }

    public bool HasOverriddenAdditionalLances(string teamType) {
      if (overriddenAdditionalLances.ContainsKey(teamType)) return true;
      return false;
    }

    private void SetOverriddenAdditionalLanceCount(string teamType, int count) {
      overriddenAdditionalLanceCount.Add(teamType, count);
    }

    public int GetOverriddenAdditionalLanceCount(string teamType) {
      if (overriddenAdditionalLanceCount.ContainsKey(teamType)) return overriddenAdditionalLanceCount[teamType];
      return 0;
    }

    private void SetOverriddenAdditionalLanceOverrides(string teamType, List<MLanceOverride> lanceOverrides) {
      overriddenAdditionalLanceOverrides.Add(teamType, lanceOverrides);
    }

    public List<MLanceOverride> GetOverriddenAdditionalLanceOverrides(string teamType) {
      if (overriddenAdditionalLanceOverrides.ContainsKey(teamType)) {
        return overriddenAdditionalLanceOverrides[teamType];
      }

      return new List<MLanceOverride>();
    }

    public void SetOverriddenAdditionalLances(string teamType, List<MLanceOverride> lanceOverrides) {
      SetOverriddenAdditionalLances(teamType);
      SetOverriddenAdditionalLanceCount(teamType, lanceOverrides.Count);
      SetOverriddenAdditionalLanceOverrides(teamType, lanceOverrides);
    }
  }
}