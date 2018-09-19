using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

namespace SpawnVariation.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "InitializeContract")]
  public class EncounterLayerParentInitializeContractPatch {
    static void Prefix(EncounterLayerParent __instance) {
      SetupEncounterPreReqLogic(__instance);

      EncounterManager encounterManager = EncounterManager.GetInstance();
      encounterManager.CreateDestroyWholeLanceObjective();
    }

    static void SetupEncounterPreReqLogic(EncounterLayerParent encounterLayerParent) {
      Main.Logger.Log($"[EncounterLayerParentInitializeContractPatch Prefix] Patching InitializeContract");
      Contract activeContract = UnityGameInstance.BattleTechGame.Combat.ActiveContract;
			string encounterObjectGuid = activeContract.encounterObjectGuid;

			for (int i = 0; i < encounterLayerParent.EncounterLayerList.Length; i++) {
				encounterLayerParent.EncounterLayerList[i].gameObject.SetActive(false);
			}

			EncounterLayerData selectedEncounterLayerData = encounterLayerParent.GetLayerByGuid(encounterObjectGuid);
			selectedEncounterLayerData.gameObject.SetActive(true);
    }
  }
}