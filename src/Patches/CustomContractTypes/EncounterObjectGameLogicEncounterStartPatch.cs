using Harmony;

using BattleTech;

using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterObjectGameLogic), "SetState")]
  public class EncounterObjectGameLogicEncounterStartPatch {
    static void Prefix(EncounterObjectGameLogic __instance, EncounterObjectStatus status) {
      EncounterObjectStateChangeMessage message = new EncounterObjectStateChangeMessage(__instance.encounterObjectGuid, status);
      UnityGameInstance.BattleTechGame.Combat.MessageCenter.PublishMessage(message);
    }

    static void Postfix(EncounterObjectGameLogic __instance, EncounterObjectStatus status) {
      EncounterObjectStateChangeMessage message = new EncounterObjectStateChangeMessage(__instance.encounterObjectGuid, status);
      UnityGameInstance.BattleTechGame.MessageCenter.PublishMessage(message);
    }
  }
}