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
      ___dialogueRef.UpdateEncounterObjectRef();
    }
  }
}