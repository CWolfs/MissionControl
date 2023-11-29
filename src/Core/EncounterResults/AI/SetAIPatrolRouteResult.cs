
using UnityEngine;

using BattleTech;

using MissionControl.Data;

/**
This result will issue the SetPatrolRoute AIOrder when triggered
*/
namespace MissionControl.Result {
  public class SetAIPatrolRouteResult : EncounterResult {
    public UnitGroupType UnitGroupType { get; set; } = UnitGroupType.Lance;
    public string UnitTypeGUID { get; set; }
    public string RouteGUID { get; set; }
    public bool FollowForward { get; set; } = true;
    public bool ShouldSprint { get; set; } = false;
    public bool StartAtClosestPoint { get; set; } = true;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetAIPatrolRouteResult] Triggering");

      SetPatrolRouteAIOrder order = ScriptableObject.CreateInstance<SetPatrolRouteAIOrder>();
      order.routeToFollow.EncounterObjectGuid = RouteGUID;
      order.forward = FollowForward;
      order.shouldSprint = ShouldSprint;
      order.startAtClosestPoint = StartAtClosestPoint;

      AiManager.Instance.IssueOrder(UnitGroupType, UnitTypeGUID, order);
    }
  }
}
