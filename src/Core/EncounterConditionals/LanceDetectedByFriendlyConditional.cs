using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Conditional {
  public class LanceDetectedByFriendlyConditional : DesignConditional {
    public string TargetLanceGuid { get; set; }
    public string DetectingLanceGuid { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);
      LanceDetectedMessage lanceDetectedMessage = message as LanceDetectedMessage;
      if (lanceDetectedMessage == null) return false;

      Main.LogDebug($"[LanceDetectedByFriendlyConditional] Evaluating actingObjectGuid of '{lanceDetectedMessage.actingObjectGuid}' and affectedObjectGuid of '{lanceDetectedMessage.affectedObjectGuid}' against target lance guid of '{this.TargetLanceGuid}'");

      // No need to check anything if the target isn't right
      if (!lanceDetectedMessage.affectedObjectGuid.Contains(this.TargetLanceGuid)) return false;

      string actingObjectGuid = lanceDetectedMessage.actingObjectGuid;
      LanceSpawnerGameLogic lanceSpawner = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<LanceSpawnerGameLogic>(actingObjectGuid);
      if (lanceSpawner != null) {
        Main.LogDebug($"[LanceDetectedByFriendlyConditional] Acting object is a LanceSpawner named " + lanceSpawner.name);
        Team team = UnityGameInstance.Instance.Game.Combat.TurnDirector.GetTurnActorByUniqueId(lanceSpawner.teamDefinitionGuid) as Team;
        bool isFriendlyTeam = team.IsFriendly(UnityGameInstance.Instance.Game.Combat.LocalPlayerTeam);
        if (isFriendlyTeam) {
          Main.LogDebug($"[LanceDetectedByFriendlyConditional] Matches a friendly lance. '{responseName}'");
          return true;
        } else {
          Main.LogDebug($"[LanceDetectedByFriendlyConditional] Not a friendly lance. '{responseName}'");
        }
      }

      // Check if a unit
      UnitSpawnPointGameLogic unit = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<UnitSpawnPointGameLogic>(actingObjectGuid);
      if (unit != null) {
        Main.LogDebug($"[LanceDetectedByFriendlyConditional] Acting object is a Unit named " + unit.name);
        Team team = UnityGameInstance.Instance.Game.Combat.TurnDirector.GetTurnActorByUniqueId(unit.team) as Team;
        bool isFriendlyTeam = team.IsFriendly(UnityGameInstance.Instance.Game.Combat.LocalPlayerTeam);
        if (isFriendlyTeam) {
          Main.LogDebug($"[LanceDetectedByFriendlyConditional] Matches a friendly unit. '{responseName}'");
          return true;
        } else {
          Main.LogDebug($"[LanceDetectedByFriendlyConditional] Not a friendly unit. '{responseName}'");
        }
      }

      if (lanceDetectedMessage.actingObjectGuid.Contains(this.DetectingLanceGuid) && lanceDetectedMessage.affectedObjectGuid.Contains(this.TargetLanceGuid)) {
        Main.LogDebug($"[LanceDetectedByFriendlyConditional] Matches lance detected. '{responseName}'");
        return true;
      }

      Main.LogDebug($"[LanceDetectedByFriendlyConditional] NO match. '{responseName}'");
      return false;
    }
  }
}