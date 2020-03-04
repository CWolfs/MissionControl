
using BattleTech;
using BattleTech.Framework;

using Harmony;

public static class EncounterChunkGameLogicExtensions {
  public static void SetObjectivesAsPrimary(this EncounterChunkGameLogic encounterChunkGameLogic, bool flag) {
    ItemRegistry itemRegistry = UnityGameInstance.BattleTechGame.Combat.ItemRegistry;

    ObjectiveGameLogic[] objectiveGameLogics = encounterChunkGameLogic.GetComponentsInChildren<ObjectiveGameLogic>();
    foreach (ObjectiveGameLogic objectiveGameLogic in objectiveGameLogics) {
      string setting = flag ? "primary" : "non-primary";
      MissionControl.Main.LogDebug($"[SetInactiveContractControlledObjectivesNotRequired] Setting '{objectiveGameLogic.gameObject.name}' {setting}");
      AccessTools.Field(typeof(ObjectiveGameLogic), "primary").SetValue(objectiveGameLogic, flag);
    }
  }
}