using System.Collections.Generic;

using Harmony;

using BattleTech;

namespace MissionControl.Data {
  public class TestBranchNode  : CustomBehaviourTreeBranch {
    public TestBranchNode(BehaviorTreeIDEnum behaviourTreeType, string path, BEHAVIOUR_INJECTION_ORDER injectionOrder) : base(behaviourTreeType, path, injectionOrder) {
      Main.Logger.Log("[TestBranchNode] Created CustomBehaviourTreeBranch");
    }

    public override void Inject(BehaviorTree behaviourTree, List<BehaviorNode> siblings, int targetIndex, BehaviorNode target) {
      string targetName = (string)AccessTools.Field(typeof(BehaviorNode), "name").GetValue(target);
      Main.Logger.Log($"[{this.GetType().Name}] Injecting custom behaviour branch {InjectionOrder} '{targetName}'");

      // TODO: This stops the behaviour tree early due to it being a success as the top level of the tree
      LogMessageSuccess debugLogToContent = new LogMessageSuccess("logMessageSuccess00001", behaviourTree, behaviourTree.unit, "Debug message for the 'TestBranchNode' in the behaviour tree");

      // TODO: Move the injection code into the common superclass
      int injectionIndex = targetIndex;
      if (InjectionOrder == BEHAVIOUR_INJECTION_ORDER.BEFORE_SIBLING) {
        injectionIndex = targetIndex - 1;
        if (injectionIndex < 0) injectionIndex = 0;
      } else if (InjectionOrder == BEHAVIOUR_INJECTION_ORDER.AFTER_SIBLING) {
        injectionIndex = targetIndex + 1;
        if (injectionIndex >= siblings.Count) {
          siblings.Add(debugLogToContent);
          return;
        }
      }

      siblings.Insert(injectionIndex, debugLogToContent);
    }
  }
}