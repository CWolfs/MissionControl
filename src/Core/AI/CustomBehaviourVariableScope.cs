using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Save.Test;
using BattleTech.Serialization;
using fastJSON;
using HBS.Collections;
using HBS.Util;
using UnityEngine;

namespace MissionControl.AI {
  /*
    This is the custom behaviour variable store / Ai blackboard.
    It's required as the vanilla game relies heavily on enums and this prevents adding additional variables
    As an alternative, it's intentionally string key based
  */
  public class CustomBehaviorVariableScope : IGuid {
    public string GUID { get; private set; }
    private Dictionary<string, BehaviorVariableValue> behaviorVariables;
    public Dictionary<AIMood, CustomBehaviorVariableScope> ScopesByMood;

    public List<string> VariableNames	{
      get {
        return new List<string>(this.behaviorVariables.Keys);
      }
    }

    public CustomBehaviorVariableScope() {
      this.behaviorVariables = new Dictionary<string, BehaviorVariableValue>();
      this.ScopesByMood = new Dictionary<AIMood, CustomBehaviorVariableScope>();
    }

    public void SetVariable(string key, BehaviorVariableValue value) {
      this.behaviorVariables[key] = value;
    }

    public void Reset() {
      this.behaviorVariables.Clear();
    }

    public BehaviorVariableValue GetVariable(string key) {
      if (this.behaviorVariables.ContainsKey(key)) return this.behaviorVariables[key];
      return null;
    }

    public BehaviorVariableValue GetVariableWithMood(string key, AIMood mood)	{
      if (this.ScopesByMood.ContainsKey(mood)) {
        BehaviorVariableValue variable = this.ScopesByMood[mood].GetVariable(key);
        if (variable != null) return variable;
      }
      
      if (this.behaviorVariables.ContainsKey(key)) return this.behaviorVariables[key];
    
      return null;
    }

    public void RemoveVariable(string key) {
      if (this.behaviorVariables.ContainsKey(key)) {
        this.behaviorVariables.Remove(key);
      }
    }

    public bool IssueAIOrder(CustomAIOrder order)	{
      Main.Logger.Log("[IssueAIOrder] Handling order: " + order);
      switch (order.OrderType) {
        case "FollowLance": {
          FollowLanceOrder followLanceOrder = order as FollowLanceOrder;
          /*
          this.SetVariable(BehaviorVariableName.String_RouteGUID, new BehaviorVariableValue(setPatrolRouteAIOrder.routeToFollowGuid));
          this.SetVariable(BehaviorVariableName.Bool_RouteStarted, new BehaviorVariableValue(false));
          this.SetVariable(BehaviorVariableName.Bool_RouteCompleted, new BehaviorVariableValue(false));
          this.SetVariable(BehaviorVariableName.Bool_RouteFollowingForward, new BehaviorVariableValue(setPatrolRouteAIOrder.forward));
          this.SetVariable(BehaviorVariableName.Bool_RouteShouldSprint, new BehaviorVariableValue(setPatrolRouteAIOrder.shouldSprint));
          this.SetVariable(BehaviorVariableName.Bool_RouteStartAtClosestPoint, new BehaviorVariableValue(setPatrolRouteAIOrder.startAtClosestPoint));
          */
          return true;
        }
      }

      // TODO: Add to the above switch a way to allow other modders to register their CustomAIOrders into this system so they can create more orders themselves

      Debug.Log($"[IssueAIOrder] No handling available for order {order}");
      return false;
    }

    public void SetGuid(string guid) {
      this.GUID = guid;
    }

    // TODO: Need to cover vanilla deserialisation for `CustomBehaviorVariableScope` type before this will work
    // TODO: Patch a call into this for the unit to save the data into the save file, or output as a companion save file
    public void Hydrate(SerializableReferenceContainer references)  {
      Dictionary<string, BehaviorVariableValue> itemDictionary = references.GetItemDictionary<string, BehaviorVariableValue>(this, "serializableCustomBehaviorVariables");
      Dictionary<int, CustomBehaviorVariableScope> itemDictionary2 = references.GetItemDictionary<int, CustomBehaviorVariableScope>(this, "serializableCustomScopesByMood");
      this.behaviorVariables = new Dictionary<string, BehaviorVariableValue>(itemDictionary.Count);

      foreach (KeyValuePair<string, BehaviorVariableValue> keyValuePair in itemDictionary) {
        string key = (string)keyValuePair.Key;
        BehaviorVariableValue value = keyValuePair.Value;
        this.behaviorVariables.Add(key, value);
      }

      this.ScopesByMood = new Dictionary<AIMood, CustomBehaviorVariableScope>(itemDictionary2.Count);
      foreach (KeyValuePair<int, CustomBehaviorVariableScope> keyValuePair2 in itemDictionary2) {
        AIMood key2 = (AIMood)keyValuePair2.Key;
        CustomBehaviorVariableScope value2 = keyValuePair2.Value;
        this.ScopesByMood.Add(key2, value2);
      }
    }

    // TODO: Need to cover vanilla serialisation for `CustomBehaviorVariableScope` type before this will work
    // TODO: Patch a call into this for the unit to load the data from the save file, or output as a companion save file
    public void Dehydrate(SerializableReferenceContainer references) {
      Dictionary<string, BehaviorVariableValue> dictionary = new Dictionary<string, BehaviorVariableValue>(this.behaviorVariables.Count);
      Dictionary<int, CustomBehaviorVariableScope> dictionary2 = new Dictionary<int, CustomBehaviorVariableScope>(this.ScopesByMood.Count);
      
      foreach (KeyValuePair<string, BehaviorVariableValue> keyValuePair in this.behaviorVariables) {
        string key = (string)keyValuePair.Key;
        BehaviorVariableValue value = keyValuePair.Value;
        dictionary.Add(key, value);
      }

      foreach (KeyValuePair<AIMood, CustomBehaviorVariableScope> keyValuePair2 in this.ScopesByMood) {
        int key2 = (int)keyValuePair2.Key;
        CustomBehaviorVariableScope value2 = keyValuePair2.Value;
        dictionary2.Add(key2, value2);
      }

      references.AddItemDictionary<string, BehaviorVariableValue>(this, "serializableCustomBehaviorVariables", dictionary);
      references.AddItemDictionary<int, CustomBehaviorVariableScope>(this, "serializableCustomScopesByMood", dictionary2);
    }
  }
}
