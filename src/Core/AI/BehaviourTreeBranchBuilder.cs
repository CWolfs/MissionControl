using System.Collections.Generic;

using Harmony;

using BattleTech;

public enum BehaviourInjectionOrder { BEFORE_SIBLING, AFTER_SIBLING };

namespace MissionControl.AI {
  public abstract class BehaviourTreeBranchBuilder {
    public BehaviourInjectionOrder InjectionOrder { get; private set; }
    public string Path { get; private set; }
    public string[] PathNodeNames { get; private set; }
    public BehaviorTreeIDEnum BehaviourTreeType { get; private set; }

    public BehaviourTreeBranchBuilder(BehaviorTreeIDEnum behaviourTreeType, string path, BehaviourInjectionOrder injectionOrder) {
      this.BehaviourTreeType = behaviourTreeType;
      this.Path = path;
      this.PathNodeNames = this.Path.Split('.');
      this.InjectionOrder = injectionOrder;
    }

    public abstract void Build(BehaviorTree behaviourTree, List<BehaviorNode> siblings, int targetIndex, BehaviorNode target, AbstractActor unit);

    public void Inject(List<BehaviorNode> siblings, int targetIndex, BehaviorNode branchRoot) {
      int injectionIndex = targetIndex;
      if (InjectionOrder == BehaviourInjectionOrder.BEFORE_SIBLING) {
        injectionIndex = targetIndex - 1;
        if (injectionIndex < 0) injectionIndex = 0;
      } else if (InjectionOrder == BehaviourInjectionOrder.AFTER_SIBLING) {
        injectionIndex = targetIndex + 1;
        if (injectionIndex >= siblings.Count) {
          siblings.Add(branchRoot);
          return;
        }
      }

      siblings.Insert(injectionIndex, branchRoot);
    }
  }
}