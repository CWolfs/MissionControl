using System.Collections.Generic;

using Harmony;

using BattleTech;

namespace MissionControl.AI {
  public class TestBranchBuilder  : BehaviourTreeBranchBuilder {
    public TestBranchBuilder(BehaviorTreeIDEnum behaviourTreeType, string path, BehaviourInjectionOrder injectionOrder) : base(behaviourTreeType, path, injectionOrder) {
      Main.Logger.Log("[TestBranchNode] Created CustomBehaviourTreeBranch");
    }

    public override void Build(BehaviorTree behaviourTree, List<BehaviorNode> siblings, int targetIndex, BehaviorNode target) {
      string targetName = (string)AccessTools.Field(typeof(BehaviorNode), "name").GetValue(target);
      Main.Logger.Log($"[{this.GetType().Name}] Injecting custom behaviour branch {InjectionOrder} '{targetName}'");

      // TODO: This stops the behaviour tree early due to it being a success as the top level of the tree
      LogMessageNode debugLogToContent = new LogMessageNode("logMessageSuccess00001", behaviourTree,
        behaviourTree.unit, "Debug message for the 'TestBranchNode' in the behaviour tree", BehaviorNodeState.Failure);

      Inject(siblings, targetIndex, debugLogToContent);
    }
  }
}