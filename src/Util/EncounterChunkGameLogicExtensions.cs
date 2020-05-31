
using BattleTech;
using BattleTech.Framework;

using Harmony;

using MissionControl;

public static class EncounterChunkGameLogicExtensions {
  public static void SetObjectivesAsPrimary(this EncounterChunkGameLogic encounterChunkGameLogic, bool flag) {
    ItemRegistry itemRegistry = UnityGameInstance.BattleTechGame.Combat.ItemRegistry;

    ObjectiveGameLogic[] objectiveGameLogics = encounterChunkGameLogic.GetComponentsInChildren<ObjectiveGameLogic>();
    foreach (ObjectiveGameLogic objectiveGameLogic in objectiveGameLogics) {
      string setting = flag ? "primary" : "non-primary";
      MissionControl.Main.LogDebug($"[SetObjectivesAsPrimary] Setting '{objectiveGameLogic.gameObject.name}' {setting}");
      AccessTools.Field(typeof(ObjectiveGameLogic), "primary").SetValue(objectiveGameLogic, flag);
    }


    ContractObjectiveGameLogic[] contractGameLogics = MissionControl.MissionControl.Instance.EncounterLayerData.GetComponentsInChildren<ContractObjectiveGameLogic>();
    foreach (ContractObjectiveGameLogic contractObjective in contractGameLogics) {
      MissionControl.Main.LogDebug($"[SetObjectivesAsPrimary] Calling '{contractObjective.gameObject.name}' QueueCheckContractObjective");
      contractObjective.QueueCheckContractObjective();
    }
  }

  public static void SetObjectivesAsIgnored(this EncounterChunkGameLogic encounterChunkGameLogic) {
    ItemRegistry itemRegistry = UnityGameInstance.BattleTechGame.Combat.ItemRegistry;

    ObjectiveGameLogic[] objectiveGameLogics = encounterChunkGameLogic.GetComponentsInChildren<ObjectiveGameLogic>();
    foreach (ObjectiveGameLogic objectiveGameLogic in objectiveGameLogics) {
      MissionControl.Main.LogDebug($"[SetObjectivesAsIgnored] Setting '{objectiveGameLogic.gameObject.name}' to IGNORED");
      objectiveGameLogic.IgnoreObjective();
    }

    ContractObjectiveGameLogic[] contractGameLogics = MissionControl.MissionControl.Instance.EncounterLayerData.GetComponentsInChildren<ContractObjectiveGameLogic>();
    foreach (ContractObjectiveGameLogic contractObjective in contractGameLogics) {
      MissionControl.Main.LogDebug($"[SetObjectivesAsIgnored] Calling '{contractObjective.gameObject.name}' QueueCheckContractObjective");
      contractObjective.QueueCheckContractObjective();
    }
  }
}