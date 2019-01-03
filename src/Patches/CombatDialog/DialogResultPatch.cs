using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(DialogResult), "Trigger")]
  public class DialogResultPatch {
    static void Prefix(DialogResult __instance, DialogueRef ___dialogueRef) {
      Main.Logger.Log($"[DialogResultPatch Prefix] Patching Trigger");
      UpdateEncounterObjectRef(___dialogueRef);
    }

    static void UpdateEncounterObjectRef(DialogueRef dialogRef) {
			EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
			if (encounterLayerData != null) {
				DialogueGameLogic[] componentsInChildren = encounterLayerData.GetComponentsInChildren<DialogueGameLogic>();
				for (int i = 0; i < componentsInChildren.Length; i++) {
					if (componentsInChildren[i].encounterObjectGuid == dialogRef.EncounterObjectGuid) {
						dialogRef.encounterObject = componentsInChildren[i];
						return;
					}
				}
			}
		}
  }
}