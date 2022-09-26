using Harmony;

using BattleTech;

/*
  This patch is the hook to modify the Core AI behaviour tree
  It allows for additions of new behaviour branches for custom AI
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(BehaviorTree), "InitRootNode")]
  public class BehaviourTreeInitRootNodePatch {
    public static void Postfix(BehaviorTree __instance, AbstractActor ___unit, BehaviorTreeIDEnum ___behaviorTreeIDEnum) {
      if (MissionControl.Instance.CurrentContract != null && MissionControl.Instance.AllowMissionControl() && ___behaviorTreeIDEnum != BehaviorTreeIDEnum.DoNothingTree) {
        Main.LogDebug($"[BehaviourTreeInitRootNodePatch Postfix] Patching InitRootNode for unit '{___unit.DisplayName}' with behaviour tree id '{___behaviorTreeIDEnum}'");
        Init(__instance, ___unit, ___behaviorTreeIDEnum);
      }
    }

    public static void Init(BehaviorTree behaviourTree, AbstractActor unit, BehaviorTreeIDEnum behaviourTreeType) {
      AiManager aiManager = AiManager.Instance;
      if (!(unit is Turret)) {
        aiManager.LoadCustomBehaviourSequences(behaviourTree, behaviourTreeType, unit);
      }
    }
  }
}