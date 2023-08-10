using BattleTech;

using HBS.Collections;

using MissionControl.Data;

public static class TagUtils {
  public static TagSet GetTagSet(string scope) {
    SimGameState simGame = UnityGameInstance.Instance.Game.Simulation;
    TagSet tags = null;

    switch (scope) {
      case "Commander": tags = simGame.CommanderTags; break;
      case "Company": tags = simGame.CompanyTags; break;
      case "System": tags = simGame.CurSystem?.Tags; break;
      case "Flashpoint": tags = simGame.ActiveFlashpoint?.Tags; break;
      case "Encounter": tags = MissionControl.MissionControl.Instance.EncounterTags; break;
      default: MissionControl.Main.Logger.LogWarning($"[TagUtils.GetTagSet] Unknown scope provided of '{scope}'"); break;
    }

    if (tags == null) {
      MissionControl.Main.Logger.LogWarning($"[TagUtils.GetTagSet] No tagset found for scope '{scope}'. Maybe no active flashpoint or current system.");
    }

    return tags;
  }

  public static TagSet GetTagSet(Scope scope) {
    return GetTagSet(scope.ToString());
  }
}
