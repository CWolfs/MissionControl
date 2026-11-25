using Harmony;

using BattleTech;

using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterObjectGameLogic), "SetState")]
  public class EncounterObjectGameLogicSetStatePatch {
    static void Prefix(EncounterObjectGameLogic __instance, out EncounterObjectStatus __state) {
      __state = __instance.GetState();
    }

    static void Postfix(EncounterObjectGameLogic __instance, EncounterObjectStatus __state) {
      EncounterObjectStatus currentState = __instance.GetState();
      if (currentState != __state) {
        EncounterObjectStateChangeMessage message = new EncounterObjectStateChangeMessage(__instance.encounterObjectGuid, currentState);
        UnityGameInstance.BattleTechGame.MessageCenter.PublishMessage(message);
      }
    }
  }
}