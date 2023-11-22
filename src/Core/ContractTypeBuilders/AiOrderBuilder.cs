using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech;

using HBS.Collections;

using System.Collections.Generic;

using MissionControl.AI;

namespace MissionControl.ContractTypeBuilders {
  public class AiOrderBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JArray ordersArray;
    private List<AIOrderBox> orders;
    private string lanceName;

    public AiOrderBuilder(ContractTypeBuilder contractTypeBuilder, JArray ordersArray, string lanceName) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.ordersArray = ordersArray;
      this.lanceName = lanceName;
    }

    public List<AIOrderBox> Build() {
      orders = new List<AIOrderBox>();

      Main.LogDebug($"[ContractTypeBuild.{contractTypeBuilder.ContractTypeKey}] There are '{ordersArray.Count} AI order(s) defined for lance '{this.lanceName}'");
      foreach (JObject order in ordersArray.Children<JObject>()) {
        BuildOrder(order);
      }

      return orders;
    }

    private void BuildOrder(JObject order) {
      string type = order["Type"].ToString();
      Main.LogDebug($"[ContractTypeBuild.{contractTypeBuilder.ContractTypeKey}] Order is '{type}'");

      switch (type) {
        case "StayInsideRegion": BuildStayInsideRegionOrder(order); break;
        case "MagicKnowledgeByTag": BuildMagicKnowledgeByTag(order); break;
        case "PrioritiseTaggedUnit": BuildPrioritiseTaggedUnit(order); break;
        case "SetPatrolRoute": BuildSetPatrolRoute(order); break;
        default:
          Main.Logger.LogError($"[AiOrderBuilder.{contractTypeBuilder.ContractTypeKey}] No valid result was built for '{type}'");
          break;
      }
    }

    private void BuildStayInsideRegionOrder(JObject orderBuild) {
      string regionGuid = orderBuild["RegionGuid"].ToString();

      StayInsideRegionAIOrder order = ScriptableObject.CreateInstance<StayInsideRegionAIOrder>();
      order.region.EncounterObjectGuid = regionGuid;

      orders.Add(new EncounterAIOrderBox(order));
    }

    private void BuildMagicKnowledgeByTag(JObject orderBuild) {
      JArray tags = (JArray)orderBuild["Tags"];
      string action = orderBuild["Action"].ToString();
      bool mustMatchAllTags = (orderBuild.ContainsKey("MustMatchAll")) ? (bool)orderBuild["MustMatchAll"] : false;

      AddMagicKnowledgeByTagAIOrder order = ScriptableObject.CreateInstance<AddMagicKnowledgeByTagAIOrder>();
      order.TargetTagSet = new TagSet(tags.ToObject<List<string>>());
      order.AddMagicKnowledge = (action == "Add") ? true : false;
      order.MustMatchAllTags = mustMatchAllTags;

      orders.Add(new EncounterAIOrderBox(order));
    }

    private void BuildPrioritiseTaggedUnit(JObject orderBuild) {
      JArray tags = (JArray)orderBuild["Tags"];
      int priority = (int)orderBuild["Priority"];
      bool mustMatchAllTags = (orderBuild.ContainsKey("MustMatchAll")) ? (bool)orderBuild["MustMatchAll"] : false;

      List<string> tagsList = tags.ToObject<List<string>>();

      TaggedUnitTargetPriorityAIOrder order = ScriptableObject.CreateInstance<TaggedUnitTargetPriorityAIOrder>();
      order.TargetTagSet = new TagSet(tagsList);
      order.Priority = priority;
      order.MustMatchAllTags = mustMatchAllTags;
      order.name = string.Join(",", tagsList);

      orders.Add(new EncounterAIOrderBox(order));
    }

    private void BuildSetPatrolRoute(JObject orderBuild) {
      string routeGUID = orderBuild["RouteGuid"].ToString();
      bool followForward = (bool)orderBuild["FollowForward"];
      bool shouldSprint = (bool)orderBuild["ShouldSprint"];
      bool startAtClosestPoint = (bool)orderBuild["StartAtClosestPoint"];

      SetPatrolRouteAIOrder order = ScriptableObject.CreateInstance<SetPatrolRouteAIOrder>();
      order.routeToFollow.EncounterObjectGuid = routeGUID;
      order.forward = followForward;
      order.shouldSprint = shouldSprint;
      order.startAtClosestPoint = startAtClosestPoint;

      orders.Add(new EncounterAIOrderBox(order));
    }
  }
}