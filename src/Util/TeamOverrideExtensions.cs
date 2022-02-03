using BattleTech.Framework;

using System.Collections.Generic;

public static class TeamOverrideExtensions {
  public static bool IsLanceInTeam(this TeamOverride teamOverride, string guid) {
    List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;

    foreach (LanceOverride lanceOverride in lanceOverrides) {
      if (lanceOverride.GUID == guid) return true;
    }

    return false;
  }
}