using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;

using Harmony;

using MissionControl;

/*
  AI Logic - Move To Follow Lance
    - Get the lance to follow
    - Find the mechs that are closest to each other - ignoring mechs too far away from the group
    - Find the middle point between the selected mechs - this is the lance centre
    - Move towards lance centre
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

      if (targetActor == null) {
        Main.Logger.LogError("[MoveToFollowLanceNode] Target Actor is null");
        return new BehaviorTreeResults(BehaviorNodeState.Failure);
      }

      Main.Logger.Log($"[MoveToFollowLanceNode] Target to follow is '{targetActor.DisplayName}'");

      bool shouldSprint = this.tree.GetCustomBehaviorVariableValue(FOLLOW_LANCE_SHOULD_SPRINT_KEY).BoolVal;
      shouldSprint = (!this.unit.HasAnyContactWithEnemy);

      MoveType moveType = (shouldSprint) ? MoveType.Sprinting : MoveType.Walking;
      this.unit.Pathing.UpdateAIPath(targetActor.CurrentPosition, targetActor.CurrentPosition, moveType);
      
      Vector3 vectorToTarget = this.unit.Pathing.ResultDestination - this.unit.CurrentPosition;
      float distanceToTarget = vectorToTarget.magnitude;
      if (distanceToTarget > travelDistance) {
        // If the target is out of range, head in the direction of that unit to the maximum possible travel distance for this turn
        vectorToTarget = vectorToTarget.normalized * travelDistance;
      }

      // Ensure the units aren't crowded
      Vector3 targetDestination = RoutingUtil.Decrowd(this.unit.CurrentPosition + vectorToTarget, this.unit);
      targetDestination = RegionUtil.MaybeClipMovementDestinationToStayInsideRegion(this.unit, targetDestination);

      float followLanceZoneRadius = this.unit.BehaviorTree.GetCustomBehaviorVariableValue(FOLLOW_LANCE_ZONE_RADIUS_KEY).FloatVal;

      if (RoutingUtil.AllUnitsInsideRadiusOfPoint(lanceMembers, targetActor.CurrentPosition, followLanceZoneRadius)) {
        Main.Logger.Log($"[MoveToFollowLanceNode] All units are within radius of target lance unit. Radius is '{followLanceZoneRadius}'.");
        return new BehaviorTreeResults(BehaviorNodeState.Failure);  // If within the radius then go onto other behaviours - if outside then pick this back up
      }

      this.unit.Pathing.UpdateAIPath(targetDestination, targetActor.CurrentPosition, (shouldSprint) ? MoveType.Sprinting : MoveType.Walking);
      targetDestination = this.unit.Pathing.ResultDestination;
      float maxCost = this.unit.Pathing.MaxCost;
      PathNodeGrid currentGrid = this.unit.Pathing.CurrentGrid;
      Vector3 targetActorPosition = targetActor.CurrentPosition;

      if ((currentGrid.GetValidPathNodeAt(targetDestination, maxCost) == null || (targetDestination - targetActor.CurrentPosition).magnitude > 1f) && this.unit.Combat.EncounterLayerData.inclineMeshData != null) {
        float maxSlope = Mathf.Tan(0.0174532924f * AIUtil.GetMaxSteepnessForAllLance(this.unit));
        List<AbstractActor> lanceUnits = AIUtil.GetLanceUnits(this.unit.Combat, this.unit.LanceId);
        targetDestination = this.unit.Combat.EncounterLayerData.inclineMeshData.GetDestination(this.unit.CurrentPosition, targetDestination, maxCost, maxSlope, this.unit, shouldSprint, lanceUnits, this.unit.Pathing.CurrentGrid, out targetActorPosition);
      }

      Vector3 currentPosition = this.unit.CurrentPosition;
      AIUtil.LogAI(string.Format("issuing order from [{0} {1} {2}] to [{3} {4} {5}] looking at [{6} {7} {8}]", new object[] {
        currentPosition.x,
        currentPosition.y,
        currentPosition.z,
        targetDestination.x,
        targetDestination.y,
        targetDestination.z,
        targetActorPosition.x,
        targetActorPosition.y,
        targetActorPosition.z
      }), "AI.DecisionMaking");

      return new BehaviorTreeResults(BehaviorNodeState.Success) {
        orderInfo = new MovementOrderInfo(targetDestination, targetActorPosition) {
          IsSprinting = shouldSprint
        },
        debugOrderString = string.Format("{0} moving toward destination: {1} dest: {2}", this.name, targetDestination, targetActor.CurrentPosition)
      };
		}
	}
}