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
        default:
          Main.Logger.LogError($"[AiOrderBuilder.{contractTypeBuilder.ContractTypeKey}] No valid result was built for '{type}'");
          break;
      }
    }

    private void BuildStayInsideRegionOrder(JObject orderObj) {
      string regionGuid = orderObj["RegionGuid"].ToString();

      StayInsideRegionAIOrder order = ScriptableObject.CreateInstance<StayInsideRegionAIOrder>();
      order.region.EncounterObjectGuid = regionGuid;

      orders.Add(new EncounterAIOrderBox(order));
    }

    private void BuildMagicKnowledgeByTag(JObject orderObj) {
      JArray tags = (JArray)orderObj["Tags"];
      string action = orderObj["Action"].ToString();
      bool mustMatchAllTags = (orderObj.ContainsKey("MustMatchAll")) ? (bool)orderObj["MustMatchAll"] : false;

      AddMagicKnowledgeByTagAIOrder order = ScriptableObject.CreateInstance<AddMagicKnowledgeByTagAIOrder>();
      order.TargetTagSet = new TagSet(tags.ToObject<List<string>>());
      order.AddMagicKnowledge = (action == "Add") ? true : false;
      order.MustMatchAllTags = mustMatchAllTags;

      orders.Add(new EncounterAIOrderBox(order));
    }

    private void BuildPrioritiseTaggedUnit(JObject orderObj) {
      JArray tags = (JArray)orderObj["Tags"];
      int priority = (int)orderObj["Priority"];
      bool mustMatchAllTags = (orderObj.ContainsKey("MustMatchAll")) ? (bool)orderObj["MustMatchAll"] : false;

      TaggedUnitTargetPriorityAIOrder order = ScriptableObject.CreateInstance<TaggedUnitTargetPriorityAIOrder>();
      order.TargetTagSet = new TagSet(tags.ToObject<List<string>>());
      order.Priority = priority;
      order.MustMatchAllTags = mustMatchAllTags;

      orders.Add(new EncounterAIOrderBox(order));
    }
  }
}