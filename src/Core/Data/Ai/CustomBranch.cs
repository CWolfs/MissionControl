using System.Collections.Generic;

using Harmony;

using BattleTech;

public enum BEHAVIOUR_INJECTION_ORDER { BEFORE_SIBLING, AFTER_SIBLING };

namespace MissionControl.Data {
  public abstract class CustomBehaviourTreeBranch {
    public BEHAVIOUR_INJECTION_ORDER InjectionOrder { get; private set; }
    public string Path { get; private set; }
    public string[] PathNodeNames { get; private set; }
    public BehaviorTreeIDEnum BehaviourTreeType { get; private set; }

    public CustomBehaviourTreeBranch(BehaviorTreeIDEnum behaviourTreeType, string path, BEHAVIOUR_INJECTION_ORDER injectionOrder) {
      this.BehaviourTreeType = behaviourTreeType;
      this.Path = path;
      this.PathNodeNames = this.Path.Split('.');
      this.InjectionOrder = injectionOrder;
    }

    public abstract void Inject(BehaviorTree behaviourTree, List<BehaviorNode> siblings, int targetIndex, BehaviorNode target);
  }
}