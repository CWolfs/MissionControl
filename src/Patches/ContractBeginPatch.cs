using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

/*
  This patch is the start of this mod.
  It allows for the planning on what should be done for this particular encounter / contract.
  Once all the tasks are queued up they will be executed at the correct patch points
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Contract), "BeginRequestResources")]
  public class ContractBeginPatch {
    public static void Prefix(Contract __instance) {
      if (!__instance.Accepted) return;

      PathFinderManager.Instance.FullReset();

      Main.Logger.Log($"[ContractBeginPatch Postfix] Patching Begin");

      if (__instance.ContractTypeValue.Name == "ArenaSkirmish") {
        ContractOverride contractOverride = new ContractOverride();
        contractOverride.player1Team.faction = FactionEnumeration.GetPlayer1sMercUnitFactionValue().Name;
        contractOverride.player2Team.faction = FactionEnumeration.GetPlayer2sMercUnitFactionValue().Name;
        contractOverride.employerTeam.faction = "Locals";

        contractOverride.player1Team.lanceOverrideList.Add(new LanceOverride());

        AccessTools.Property(typeof(Contract), "Override").SetValue(__instance, contractOverride, null);
      }

      Init(__instance);
    }

    public static void Init(Contract contract) {
      MissionControl encounterManager = MissionControl.Instance;
      encounterManager.SetContract(contract);
    }
  }
}