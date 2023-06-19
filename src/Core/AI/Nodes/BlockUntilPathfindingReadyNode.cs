using System;
using BattleTech;
using UnityEngine;

namespace MissionControl.AI {
  public class BlockUntilPathfindingReadyNode : LeafBehaviorNode {
    private float startTime;
    private int tickCount;
    private float TIME_TO_WAIT_FOR_PATHFINDING = Main.Settings.AiSettings.FollowAiSettings.TimeToWaitForPathfinding; // Seconds
    private int TICKS_TO_WAIT_FOR_PATHFINDING = Main.Settings.AiSettings.FollowAiSettings.TicksToWaitForPathfinding;

    public BlockUntilPathfindingReadyNode(string name, BehaviorTree tree, AbstractActor unit) : base(name, tree, unit) { }

    public override void OnStart() {
      this.startTime = Time.realtimeSinceStartup;
      this.tickCount = 0;
      Main.LogDebug($"[AI] [BlockUntilPathfindingReadyNode] Block until pathfinding ready started at {this.startTime}");
      base.LogAI("Block until pathfinding ready started at " + this.startTime, "AI.BehaviorNodes");
      if (this.unit.Pathing != null && !this.unit.Pathing.ArePathGridsComplete) {
        this.unit.Combat.PathingManager.AddNewBlockingPath(this.unit.Pathing);
      }
    }

    public override BehaviorTreeResults Tick() {
      if (this.unit.Pathing == null) {
        return BehaviorTreeResults.BehaviorTreeResultsFromBoolean(false);
      }
      this.tickCount++;
      if (this.unit.Pathing.ArePathGridsComplete) {
        Main.LogDebug($"[AI] [BlockUntilPathfindingReadyNode] Block until pathfinding completing with grids complete");
        base.LogAI("Block until pathfinding completing with grids complete", "AI.BehaviorNodes");
        return new BehaviorTreeResults(BehaviorNodeState.Success);
      }
      float num = Time.realtimeSinceStartup - this.startTime;
      if (num > 60f && this.tickCount > 20) {
        Main.LogDebug($"[AI] [BlockUntilPathfindingReadyNode] Block until pathfinding failing, having timed out with too long a time '{num}' '{this.tickCount}'");
        base.LogAI(string.Format("Block until pathfinding failing, having timed out with too long a time {0} {1}", num, this.tickCount), "AI.BehaviorNodes");
        return new BehaviorTreeResults(BehaviorNodeState.Failure);
      }
      Main.LogDebug($"[AI] [BlockUntilPathfindingReadyNode] Block until pathfinding waiting for pathing");
      base.LogAI("Block until pathfinding waiting for pathing", "AI.BehaviorNodes");
      return new BehaviorTreeResults(BehaviorNodeState.Running);
    }
  }
}