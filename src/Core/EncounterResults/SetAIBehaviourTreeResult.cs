
using UnityEngine;

using MissionControl.Data;

/**
This result will issue the SetBehaviourTree AIOrder when triggered
*/
namespace MissionControl.Result {
  public class SetAIBehaviourTreeResult : EncounterResult {
    public UnitGroupType UnitGroupType { get; set; } = UnitGroupType.Lance;
    public string UnitTypeGUID { get; set; }
    public BehaviorTreeIDEnum BehaviourTree { get; set; } = BehaviorTreeIDEnum.CoreAITree;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetAIBehaviourTreeResult] Triggering using tree " + BehaviourTree.ToString());

      SetBehaviorTreeAIOrder order = ScriptableObject.CreateInstance<SetBehaviorTreeAIOrder>();
      order.BehaviorTreeID = BehaviourTree;

      AiManager.Instance.IssueOrder(UnitGroupType, UnitTypeGUID, order);
    }
  }
}
