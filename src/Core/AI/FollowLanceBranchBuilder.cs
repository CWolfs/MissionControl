using System.Collections.Generic;

using Harmony;

using BattleTech;

namespace MissionControl.AI {
  public class TestBranchBuilder  : BehaviourTreeBranchBuilder {
    private BehaviorTree tree;
    private AbstractActor unit;

    public TestBranchBuilder(BehaviorTreeIDEnum behaviourTreeType, string path, BehaviourInjectionOrder injectionOrder) : base(behaviourTreeType, path, injectionOrder) {
      Main.Logger.Log("[TestBranchNode] Created CustomBehaviourTreeBranch");
    }

    public override void Build(BehaviorTree behaviourTree, CompositeBehaviorNode parentNode, int targetIndex, BehaviorNode target, AbstractActor unit) {
      this.tree = behaviourTree;
      this.unit = unit;

      string targetName = (string)AccessTools.Field(typeof(BehaviorNode), "name").GetValue(target);
      Main.Logger.Log($"[{this.GetType().Name}] Injecting custom behaviour branch {InjectionOrder} '{targetName}'");

      SequenceNode testBranchNodeRoot = new SequenceNode("follow_lance_if_following", behaviourTree, unit);
      BuildFirstLevel(testBranchNodeRoot);

      Inject(parentNode, targetIndex, testBranchNodeRoot);
    }

    private void BuildFirstLevel(SequenceNode root) {
      LogMessageNode debugLogToContent1 = new LogMessageNode("logMessageSuccess0000", tree,
        tree.unit, "Log Node", BehaviorNodeState.Success);

      HasFollowLanceTargetNode hasFollowLanceTarget = new HasFollowLanceTargetNode("hasFollowLanceTarget0000", tree, tree.unit);
      MoveToFollowLanceNode moveToFollowLance = new MoveToFollowLanceNode("moveToFollowLance0000", tree, tree.unit, false);

      root.AddChild(debugLogToContent1);
      root.AddChild(hasFollowLanceTarget);
      root.AddChild(moveToFollowLance);
    }
  }
}