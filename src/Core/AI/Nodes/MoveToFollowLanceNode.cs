using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;

using Harmony;

using MissionControl;

/*
  AI Logic - Move To Follow Lance
    - Get the lance to follow
    - Get heaviest unit in lance
    - Move towards target unit until reaching set radius around it
*/
namespace MissionControl.AI {
  public class MoveToFollowLanceNode : LeafBehaviorNode {
    public const string FOLLOW_LANCE_TARGET_GUID_KEY = "String_FollowLanceTargetGuid";
    public const string FOLLOW_LANCE_SHOULD_SPRINT_KEY = "Bool_ShouldFollowLanceSprint";
    public const string FOLLOW_LANCE_ZONE_RADIUS_KEY = "Float_FollowLanceZoneRadius";

    private bool waitForLance = false;

    public MoveToFollowLanceNode(string name, BehaviorTree tree, AbstractActor unit, bool waitForLance) : base(name, tree, unit) {
      this.waitForLance = waitForLance;
    }

    protected override BehaviorTreeResults Tick() {
      if (unit.HasMovedThisRound) return new BehaviorTreeResults(BehaviorNodeState.Failure);

      BehaviorVariableValue targetLanceGuidValue = this.tree.GetCustomBehaviorVariableValue(FOLLOW_LANCE_TARGET_GUID_KEY);
      if (targetLanceGuidValue == null) return new BehaviorTreeResults(BehaviorNodeState.Failure);

      string targetLanceGuid = targetLanceGuidValue.StringVal;
      Lance targetLance = DestinationUtil.FindLanceByGUID(this.tree, targetLanceGuid);
      if (targetLance == null) return new BehaviorTreeResults(BehaviorNodeState.Failure);

      AbstractActor closestEnemy = null;
      if (Main.Settings.AiSettings.FollowAiSettings.StopWhen == "OnEnemyVisible") {
        Main.LogDebug($"[MoveToFollowLanceNode] Looking for closest visible enemy.");
        closestEnemy = AiUtils.GetClosestVisibleEnemy(this.unit, targetLance);
        if (closestEnemy != null) Main.LogDebug($"[MoveToFollowLanceNode] Closest visible enemy is: '{closestEnemy.DisplayName}'");
      } else { // OnEnemyDetected
        Main.LogDebug($"[MoveToFollowLanceNode] Looking for closest detected enemy.");
        closestEnemy = AiUtils.GetClosestDetectedEnemy(this.unit, targetLance);
        if (closestEnemy != null) Main.LogDebug($"[MoveToFollowLanceNode] Closest detected enemy is: '{closestEnemy.DisplayName}'");
      }

      if (closestEnemy != null) {
        if (Main.Settings.AiSettings.FollowAiSettings.StopWhen != "WhenNotNeeded") {
          Main.LogDebug($"[MoveToFollowLanceNode] Detected enemy. No longer following player mech.");
          return new BehaviorTreeResults(BehaviorNodeState.Failure);
        } else {
          Main.LogDebug($"[MoveToFollowLanceNode] Enemies detected but keeping tight formation still. Following player mech.");
        }
      } else {
        Main.LogDebug($"[MoveToFollowLanceNode] No enemies detected. Following player mech.");
      }


      List<AbstractActor> lanceMembers = AIUtil.GetLanceUnits(this.unit.Combat, this.unit.LanceId);
      float travelDistance = Mathf.Max(this.unit.MaxSprintDistance, this.unit.MaxWalkDistance);

      if (this.waitForLance) {
        for (int i = 0; i < lanceMembers.Count; i++) {
          AbstractActor abstractActor = lanceMembers[i] as AbstractActor;

          if (abstractActor != null) {
            float lanceMemberTravelDistance = Mathf.Max(abstractActor.MaxWalkDistance, abstractActor.MaxSprintDistance);
            travelDistance = Mathf.Min(travelDistance, lanceMemberTravelDistance);
          }
        }
      }

      AbstractActor targetActor = GetMechToFollow(targetLance);

      if (targetActor == null) {
        Main.Logger.LogError("[MoveToFollowLanceNode] Target Actor is null");
        return new BehaviorTreeResults(BehaviorNodeState.Failure);
      }

      Main.LogDebug($"[MoveToFollowLanceNode] Target to follow is '{targetActor.DisplayName} {targetActor.VariantName}'");

      bool shouldSprint = this.tree.GetCustomBehaviorVariableValue(FOLLOW_LANCE_SHOULD_SPRINT_KEY).BoolVal;
      Main.LogDebug($"[MoveToFollowLanceNode] Should sprint by behaviour value being set? '{shouldSprint}'");

      shouldSprint = (!this.unit.HasAnyContactWithEnemy);

      Main.LogDebug($"[MoveToFollowLanceNode] Should sprint by contact with enemy? '{shouldSprint}'");

      shouldSprint = (this.unit.CurrentPosition - targetActor.CurrentPosition).magnitude > Main.Settings.AiSettings.FollowAiSettings.MaxDistanceFromTargetBeforeSprinting; // sprint if the unit is over 200 metres away
      Main.LogDebug($"[MoveToFollowLanceNode] Is the follow target further than 200m? Should sprint? '{shouldSprint}'");

      Vector3 lookDirection = (closestEnemy == null) ? targetActor.CurrentPosition : closestEnemy.CurrentPosition;
      MoveType moveType = (shouldSprint) ? MoveType.Sprinting : MoveType.Walking;
      this.unit.Pathing.UpdateAIPath(targetActor.CurrentPosition, lookDirection, moveType);

      Vector3 vectorToTarget = this.unit.Pathing.ResultDestination - this.unit.CurrentPosition;
      float distanceToTarget = vectorToTarget.magnitude;
      if (distanceToTarget > travelDistance) {
        Main.LogDebug($"[MoveToFollowLanceNode] Can't reach follow target in one go so will go as far as I can");
        // If the target is out of range, head in the direction of that unit to the maximum possible travel distance for this turn
        vectorToTarget = vectorToTarget.normalized * travelDistance;
      }

      // Ensure the units aren't crowded
      Vector3 targetDestination = RoutingUtil.Decrowd(this.unit.CurrentPosition + vectorToTarget, this.unit);
      targetDestination = RegionUtil.MaybeClipMovementDestinationToStayInsideRegion(this.unit, targetDestination);

      float followLanceZoneRadius = this.unit.BehaviorTree.GetCustomBehaviorVariableValue(FOLLOW_LANCE_ZONE_RADIUS_KEY).FloatVal;
      Main.LogDebug($"[MoveToFollowLanceNode] My follow zone radius is '{followLanceZoneRadius}'");
      if (RoutingUtils.IsUnitInsideRadiusOfPoint(this.unit, targetActor.CurrentPosition, followLanceZoneRadius)) {
        Main.LogDebug($"[MoveToFollowLanceNode] ...and I am inside that zone.");
        return new BehaviorTreeResults(BehaviorNodeState.Failure);
      } else {
        Main.LogDebug($"[MoveToFollowLanceNode] ...and I am NOT inside that zone.");
      }

      Vector3 lookPosition = targetDestination;

      if (Main.Settings.AiSettings.FollowAiSettings.Pathfinding == "Alternative") {
        Main.LogDebug($"[MoveToFollowLanceNode] Using 'Alternative' pathfinding");
        this.unit.Pathing.UpdateAIPath(targetDestination, targetDestination, moveType);
        Vector3 resultDestination = this.unit.Pathing.ResultDestination;
        float maxCost = this.unit.Pathing.MaxCost;
        PathNodeGrid currentGrid = this.unit.Pathing.CurrentGrid;

        float floatVal = 10f;

        if (currentGrid.GetValidPathNodeAt(resultDestination, maxCost) == null || (resultDestination - targetDestination).magnitude > floatVal) {
          List<AbstractActor> lanceUnits = AIUtil.GetLanceUnits(this.unit.Combat, this.unit.LanceId);
          List<Vector3> dynamicPathToDestination = DynamicLongRangePathfinder.GetDynamicPathToDestination(targetDestination, maxCost, this.unit, shouldSprint, lanceUnits, currentGrid, 0f);

          if (dynamicPathToDestination == null || dynamicPathToDestination.Count == 0) {
            return new BehaviorTreeResults(BehaviorNodeState.Failure);
          }

          resultDestination = dynamicPathToDestination[dynamicPathToDestination.Count - 1];
          Vector2 a = new Vector2(targetDestination.x, targetDestination.z);
          float num2 = float.MaxValue;
          Vector3? vector4 = null;
          for (int j = 0; j < dynamicPathToDestination.Count; j++) {
            Vector3 vector5 = dynamicPathToDestination[j];

            if (RoutingUtils.IsUnitInsideRadiusOfPoint(this.unit, vector5, followLanceZoneRadius)) {
              float sqrMagnitude = (a - new Vector2(vector5.x, vector5.z)).sqrMagnitude;
              if (sqrMagnitude < num2) {
                num2 = sqrMagnitude;
                vector4 = new Vector3?(vector5);
              }
            }
          }

          if (vector4 != null) {
            resultDestination = vector4.Value;
          }
        }
      } else {
        Main.LogDebug($"[MoveToFollowLanceNode] Using 'Original' pathfinding");
        this.unit.Pathing.UpdateAIPath(targetDestination, lookDirection, moveType);
        targetDestination = this.unit.Pathing.ResultDestination;
        float maxCost = this.unit.Pathing.MaxCost;
        PathNodeGrid currentGrid = this.unit.Pathing.CurrentGrid;
        Vector3 targetActorPosition = targetActor.CurrentPosition;

        // This method seems to get called all the time - this is meant to be a last resort method I think. I wonder why the other AI pathfinding methods don't work?
        if ((currentGrid.GetValidPathNodeAt(targetDestination, maxCost) == null || (targetDestination - targetActor.CurrentPosition).magnitude > 1f) && this.unit.Combat.EncounterLayerData.inclineMeshData != null) {
          float maxSlope = Mathf.Tan(0.0174532924f * AIUtil.GetMaxSteepnessForAllLance(this.unit));
          List<AbstractActor> lanceUnits = AIUtil.GetLanceUnits(this.unit.Combat, this.unit.LanceId);
          targetDestination = this.unit.Combat.EncounterLayerData.inclineMeshData.GetDestination(this.unit.CurrentPosition, targetDestination, maxCost, maxSlope, this.unit, shouldSprint, lanceUnits, this.unit.Pathing.CurrentGrid, out targetActorPosition);
        }
      }

      Vector3 currentPosition = this.unit.CurrentPosition;
      AIUtil.LogAI(string.Format("issuing order from [{0} {1} {2}] to [{3} {4} {5}] looking at [{6} {7} {8}]", new object[] {
        currentPosition.x,
        currentPosition.y,
        currentPosition.z,
        targetDestination.x,
        targetDestination.y,
        targetDestination.z,
        lookPosition.x,
        lookPosition.y,
        lookPosition.z
      }), "AI.DecisionMaking");

      // TODO: Factor in jump mechs
      return new BehaviorTreeResults(BehaviorNodeState.Success) {
        orderInfo = new MovementOrderInfo(targetDestination, lookPosition) {
          IsSprinting = shouldSprint
        },
        debugOrderString = string.Format("{0} moving toward destination: {1} dest: {2}", this.name, targetDestination, targetActor.CurrentPosition)
      };
    }

    public AbstractActor GetMechToFollow(Lance targetLance) {
      if (Main.Settings.AiSettings.FollowAiSettings.Target == "LanceOrder") {
        return GetFirstLanceMember(targetLance);
      } else {  // HeaviestMech
        return GetHeaviestMech(targetLance);
      }
    }

    public AbstractActor GetFirstLanceMember(Lance targetLance) {
      for (int i = 0; i < targetLance.unitGuids.Count; i++) {
        ITaggedItem itemByGUID = this.unit.Combat.ItemRegistry.GetItemByGUID(targetLance.unitGuids[i]);

        if (itemByGUID != null) {
          AbstractActor abstractActor = itemByGUID as AbstractActor;

          if (abstractActor != null && !abstractActor.IsDead) {
            return abstractActor;
          }
        }
      }
      return null;
    }

    public AbstractActor GetHeaviestMech(Lance targetLance) {
      AbstractActor targetActor = null;
      float targetTonnage = 0;
      for (int i = 0; i < targetLance.unitGuids.Count; i++) {
        ITaggedItem itemByGUID = this.unit.Combat.ItemRegistry.GetItemByGUID(targetLance.unitGuids[i]);

        if (itemByGUID != null) {
          AbstractActor abstractActor = itemByGUID as AbstractActor;

          if (abstractActor != null && !abstractActor.IsDead) {
            if (abstractActor is Mech) {
              Mech mech = (Mech)abstractActor;
              if (mech.tonnage > targetTonnage) {
                targetActor = mech;
                targetTonnage = mech.tonnage;
              }
            } else if (abstractActor is Vehicle) {
              Vehicle vehicle = (Vehicle)abstractActor;
              if (vehicle.tonnage > targetTonnage) {
                targetActor = vehicle;
                targetTonnage = vehicle.tonnage;
              }
            }
          }
        }
      }
      return targetActor;
    }
  }
}