using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Conditional {
  public class LanceDetectedConditional : DesignConditional {
    public string TargetLanceGuid { get; set; }
    public string DetectingLanceGuid { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      LanceDetectedMessage lanceDetectedMessage = message as LanceDetectedMessage;
      if (lanceDetectedMessage == null) return false;

      Main.LogDebug($"[LanceIsDetectedConditional] Evaluating actingObjectGuid of '{lanceDetectedMessage.actingObjectGuid}' and affectedObjectGuid of '{lanceDetectedMessage.affectedObjectGuid}' against target lance guid of '{this.TargetLanceGuid}'");

      // Check if detecting GUID is a lance, if so - get all the lance units and test against their GUIDs
      LanceSpawnerGameLogic lanceSpawner = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<LanceSpawnerGameLogic>(DetectingLanceGuid);
      if (lanceSpawner != null) {
        Main.LogDebug($"[LanceIsDetectedConditional] Set DetectingLanceGuid is a LanceSpawner. Checking incoming actingObjectGuid against unit spawn GUIDs");
        UnitSpawnPointGameLogic[] units = lanceSpawner.GetComponentsInChildren<UnitSpawnPointGameLogic>();

        foreach (UnitSpawnPointGameLogic unit in units) {
          if (lanceDetectedMessage.actingObjectGuid.Contains(unit.GUID) && lanceDetectedMessage.affectedObjectGuid.Contains(this.TargetLanceGuid)) {
            Main.LogDebug($"[LanceIsDetectedConditional] Matches detected in unit of DetectingLanceGuid (LanceSpawner). '{responseName}'");
            return true;
          }
        }
      }

      if (lanceDetectedMessage.actingObjectGuid.Contains(this.DetectingLanceGuid) && lanceDetectedMessage.affectedObjectGuid.Contains(this.TargetLanceGuid)) {
        Main.LogDebug($"[LanceIsDetectedConditional] Matches detected. '{responseName}'");
        return true;
      }

      Main.LogDebug($"[LanceIsDetectedConditional] NO match. '{responseName}'");
      return false;
    }
  }
}