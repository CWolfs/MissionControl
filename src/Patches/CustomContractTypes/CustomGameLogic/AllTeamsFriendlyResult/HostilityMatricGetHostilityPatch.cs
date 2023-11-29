using System;

using Harmony;

using BattleTech;

using MissionControl.Result;


namespace MissionControl.Patches {
  [HarmonyPatch(typeof(HostilityMatrix), "GetHostility")]
  [HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
  public class HostilityMatricGetHostilityPatch {
    static bool Prefix(HostilityMatrix __instance, ref Hostility __result) {
      if (UnityGameInstance.BattleTechGame.Combat != null) {
        string enableAllTeamsRelationship = MissionControl.Instance.GetGameLogicData(SetAllTeamsRelationshipResult.ENABLE_ALL_TEAMS_RELATIONSHIP);

        if (enableAllTeamsRelationship != null && enableAllTeamsRelationship == "true") {
          string relationshipRaw = MissionControl.Instance.GetGameLogicData(SetAllTeamsRelationshipResult.ALL_TEAMS_RELATIONSHIP);
          Hostility relationship = (Hostility)Enum.Parse(typeof(Hostility), relationshipRaw.ToUpper());
          __result = relationship;
          return false;
        }
      }

      return true;
    }
  }
}