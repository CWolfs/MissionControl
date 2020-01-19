using Harmony;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Logic;

/*
  This patch is used to inject a custom lance into the target team.
  This allows BT to then request the resources for the additional lance
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ContractOverride), "GenerateUnits")]
  public class ContractOverrideGenerateUnitsPatch {
    static void Prefix(ContractOverride __instance) {
      Main.Logger.Log($"[ContractOveridePatch Prefix] Patching GenerateUnits");

      if (__instance.contract == null) __instance.SetupContract(MissionControl.Instance.CurrentContract);

      RunPayload payload = new ContractOverridePayload(__instance);
      MissionControl.Instance.RunEncounterRules(LogicBlock.LogicType.CONTRACT_OVERRIDE_MANIPULATION, payload);

      if (!MissionControl.Instance.IsSkirmish()) {
        TagSet companyTags = new TagSet(UnityGameInstance.BattleTechGame.Simulation.CompanyTags);
        System.DateTime? date = DataManager.Instance.GetSimGameCurrentDate();

        // This is required because on the 3rd+ contract restart something bugs out and harmony skips the GenerateTeam method but runs pre- and post-fix
        System.Reflection.MethodInfo generateTeamMethod = AccessTools.Method(typeof(ContractOverride), "GenerateTeam");
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.player1Team, date, companyTags });
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.player2Team, date, companyTags });
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.employerTeam, date, companyTags });
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.employersAllyTeam, date, companyTags });
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.targetTeam, date, companyTags });
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.targetsAllyTeam, date, companyTags });
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.neutralToAllTeam, date, companyTags });
        generateTeamMethod.Invoke(__instance, new object[] { MetadataDatabase.Instance, __instance.hostileToAllTeam, date, companyTags });
      }
    }
  }
}