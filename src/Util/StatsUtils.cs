using BattleTech;

using MissionControl.Data;

public static class StatsUtils {
  public static StatCollection GetStats(string scope) {
    SimGameState simGame = UnityGameInstance.Instance.Game.Simulation;
    StatCollection stats = null;

    switch (scope) {
      case "Commander": stats = simGame.CommanderStats; break;
      case "Company": stats = simGame.CompanyStats; break;
      case "System": stats = simGame.CurSystem?.Stats; break;
      case "Flashpoint": stats = simGame.ActiveFlashpoint?.Stats; break;
      case "Encounter": stats = MissionControl.MissionControl.Instance.EncounterStats; break;
      default: MissionControl.Main.Logger.LogWarning($"[StatsUtils.GetStats] Unknown scope provided of '{scope}'"); break;
    }

    if (stats == null) {
      MissionControl.Main.Logger.LogWarning($"[StatsUtils.GetStats] No stats collection found for scope '{scope}'. Maybe no active flashpoint or current system.");
    }

    return stats;
  }

  public static StatCollection GetStats(Scope scope) {
    return GetStats(scope.ToString());
  }
}
