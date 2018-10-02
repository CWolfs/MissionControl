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
  [HarmonyPatch(typeof(CoreAI_BT), "InitRootNode")]
  public class CoreAIInitRootNodePatch {
    public static void Prefix(BehaviorTree behaviorTree, AbstractActor unit, GameInstance game) {
      Main.Logger.Log($"[CoreAIInitRootNodePatch Postfix] Patching InitRootNode for unit '{unit.DisplayName} | {unit.UnitName} | {unit.Nickname}'");
      Init(behaviorTree, unit, game);
    }

    public static void Init(BehaviorTree behaviourTree, AbstractActor unit, GameInstance game) {
      AiManager aiManager = AiManager.Instance;
      aiManager.AddCustomRootLeafBehaviourSequences(behaviourTree);
    }
  }
}