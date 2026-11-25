using System.Collections.Generic;

using BattleTech;

public static class LanceExtensions {
  public static List<AbstractActor> GetLanceUnits(this Lance lance) {
    List<AbstractActor> lanceUnits = new List<AbstractActor>();
    List<string> unitGuids = lance.unitGuids;

    foreach (string unitGuid in unitGuids) {
      AbstractActor actor = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<AbstractActor>(unitGuid);
      if (actor != null) {
        lanceUnits.Add(actor);
      } else {
        MissionControl.Main.Logger.LogError("[Lance.GetLanceUnits] Actor by guid '{unitGuid}' is null");
      }
    }

    return lanceUnits;
  }

  public static bool IsLanceDestroyed(this Lance lance) {
    if (lance == null || lance.unitGuids == null || lance.unitGuids.Count == 0) return false;

    foreach (string unitGuid in lance.unitGuids) {
      AbstractActor actor = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<AbstractActor>(unitGuid);
      if (actor == null || !actor.IsDead) return false;
    }

    return true;
  }
}