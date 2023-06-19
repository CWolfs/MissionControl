using BattleTech;

namespace MissionControl.AI {
  public class HasFollowLanceTargetNode : LeafBehaviorNode {

    public HasFollowLanceTargetNode(string name, BehaviorTree tree, AbstractActor unit) : base(name, tree, unit) { }

    public override BehaviorTreeResults Tick() {
      bool hasFollowLanceTarget = AiUtils.HasFollowLanceTarget(this.unit);
      Main.LogDebug($"[AI] [HasFollowLanceTargetNode] {hasFollowLanceTarget}");
      return new BehaviorTreeResults((hasFollowLanceTarget) ? BehaviorNodeState.Success : BehaviorNodeState.Failure);
    }
  }
}