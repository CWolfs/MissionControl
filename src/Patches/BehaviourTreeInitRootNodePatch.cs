using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

/*
  This patch is the hook to modify the Core AI behaviour tree
  It allows for additions of new behaviour branches for custom AI
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(BehaviorTree), "InitRootNode")]
  public class BehaviourTreeInitRootNodePatch {
    public static void Postfix(BehaviorTree __instance, AbstractActor ___unit, BehaviorTreeIDEnum ___behaviorTreeIDEnum) {
      if (___behaviorTreeIDEnum != BehaviorTreeIDEnum.DoNothingTree) {
        if (Main.Settings.DebugMode) Main.Logger.Log($"[BehaviourTreeInitRootNodePatch Postfix] Patching InitRootNode for unit '{___unit.DisplayName}' with behaviour tree id '{___behaviorTreeIDEnum}'");
        Init(__instance, ___unit, ___behaviorTreeIDEnum);
      }
    }

    public static void Init(BehaviorTree behaviourTree, AbstractActor unit, BehaviorTreeIDEnum behaviourTreeType) {
      AiManager aiManager = AiManager.Instance;
      aiManager.LoadCustomBehaviourSequences(behaviourTree, behaviourTreeType, unit);
    }
  }
}