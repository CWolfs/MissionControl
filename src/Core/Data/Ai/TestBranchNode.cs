using BattleTech;

namespace MissionControl.Data {
  public class TestBranchNode  : CustomBehaviourTreeBranch {
    public TestBranchNode(BehaviorTreeIDEnum behaviourTreeType, string path, BEHAVIOUR_INJECTION_ORDER injectionOrder) : base(behaviourTreeType, path, injectionOrder) {
      Main.Logger.Log("[TestBranchNode] Created CustomBehaviourTreeBranch");
    }
  }
}