using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Harmony;

using BattleTech;

using MissionControl.AI;
using MissionControl.Utils;

namespace MissionControl {
  public class AiManager {
    private static AiManager instance;
    public static AiManager Instance {
      get {
        if (instance == null) instance = new AiManager();
        return instance;
      }
    }

    private Dictionary<BehaviorTreeIDEnum, List<BehaviourTreeBranchBuilder>> injectionBranchRoots = new Dictionary<BehaviorTreeIDEnum, List<BehaviourTreeBranchBuilder>>();
    private Dictionary<string, Dictionary<string, CustomBehaviorVariableScope>> customBehaviourVariables = new Dictionary<string, Dictionary<string, CustomBehaviorVariableScope>>();

    private AiManager() {
      // Test
      BehaviourTreeBranchBuilder customBehaviourBranch = new FollowLanceBranchBuilder(BehaviorTreeIDEnum.CoreAITree,
        "comply_with_stay_inside_region_order", BehaviourInjectionOrder.AFTER_SIBLING);
      AddCustomBehaviourBranch(customBehaviourBranch.BehaviourTreeType, customBehaviourBranch.Path, customBehaviourBranch);
    }

    public void LoadCustomBehaviourSequences(BehaviorTree behaviourTree, BehaviorTreeIDEnum behaviourTreeType, AbstractActor unit) {
      if (injectionBranchRoots.ContainsKey(behaviourTreeType)) {
        List<BehaviourTreeBranchBuilder> customBehaviourBranches = injectionBranchRoots[behaviourTreeType];
        foreach (BehaviourTreeBranchBuilder customBehaviourBranch in customBehaviourBranches) {
          NodeSearchResult nodeSearchResults = FindNode(behaviourTree, customBehaviourBranch);
          if (nodeSearchResults != null) {
            AddCustomBehaviourVariableScopes(unit);
            customBehaviourBranch.Build(behaviourTree, nodeSearchResults.ParentNode, nodeSearchResults.NodeIndex, nodeSearchResults.Node, unit);
          }
        }
      }
    }

    private NodeSearchResult FindNode(BehaviorTree behaviourTree, BehaviourTreeBranchBuilder customBranch) {
      BehaviorNode rootNode = behaviourTree.RootNode;
      string[] pathNodeNames = customBranch.PathNodeNames;

      if (customBranch.BehaviourTreeType == BehaviorTreeIDEnum.CoreAITree) {
        BehaviorNode parent = rootNode;
        NodeSearchResult nodeSearchResults = null;
        for (int i = 0; i < pathNodeNames.Length; i++) {
          nodeSearchResults = FindNode(parent, pathNodeNames[i]);
          if (nodeSearchResults != null) {
            parent = nodeSearchResults.Node;
          }
        }

        if (nodeSearchResults != null) {
          string name = (string)AccessTools.Field(typeof(BehaviorNode), "name").GetValue(nodeSearchResults.Node);
          Main.LogDebug($"[FindNode] Found target from path '{customBranch.Path}'. Target found was '{name}'");
          return nodeSearchResults;
        }
      }

      return null;
    }

    private NodeSearchResult FindNode(BehaviorNode parent, string nodeName) {
      if (parent is CompositeBehaviorNode) {
        CompositeBehaviorNode compositeParent = (CompositeBehaviorNode)parent;
        int index = compositeParent.Children.FindIndex(child => {
          string childName = (string)AccessTools.Field(typeof(BehaviorNode), "name").GetValue(child);
          if (childName == nodeName) return true;
          return false;
        });

        if (index != -1) return new NodeSearchResult(compositeParent, compositeParent.Children, index, compositeParent.Children[index]);
        return null;
      }

      string name = (string)AccessTools.Field(typeof(BehaviorNode), "name").GetValue(parent);
      Main.Logger.LogError($"[FindNode] BehaviourNode parent '{name}' is not a composite node. Paths for custom behaviour branches can only contain composite nodes.");
      return null;
    }

    public void AddCustomBehaviourBranch(BehaviorTreeIDEnum behaviourTreeType, string path, BehaviourTreeBranchBuilder customBranch) {
      if (!injectionBranchRoots.ContainsKey(behaviourTreeType)) injectionBranchRoots.Add(behaviourTreeType, new List<BehaviourTreeBranchBuilder>());
      injectionBranchRoots[behaviourTreeType].Add(customBranch);
    }

    public void AddCustomBehaviourVariableScopes(AbstractActor unit) {
      AddCustomBehaviourVariableScope("UNIT", unit.GUID);
      if (unit.lance != null) AddCustomBehaviourVariableScope("LANCE", unit.lance.GUID);
      if (unit.team != null) AddCustomBehaviourVariableScope("TEAM", unit.team.GUID);
      // TODO: Support global scopes [pilot personality, unit role, ai skill] on a later release
    }

    public void AddCustomBehaviourVariableScope(string type, string ownerGuid) {
      if (!customBehaviourVariables.ContainsKey(type)) customBehaviourVariables[type] = new Dictionary<string, CustomBehaviorVariableScope>();
      if (!customBehaviourVariables[type].ContainsKey(ownerGuid)) {
        // Main.Logger.Log($"[AddCustomBehaviourVariableScope] Adding for {type} and {ownerGuid}");
        customBehaviourVariables[type][ownerGuid] = new CustomBehaviorVariableScope();
      }
    }

    public BehaviorVariableValue GetBehaviourVariableValue(AbstractActor unit, string key) {
      BehaviorVariableValue behaviourVariableValue = GetBehaviourVariableValue("UNIT", unit.GUID, key);
      if (behaviourVariableValue != null) return behaviourVariableValue;

      behaviourVariableValue = GetBehaviourVariableValue("LANCE", unit.lance.GUID, key);
      if (behaviourVariableValue != null) return behaviourVariableValue;

      behaviourVariableValue = GetBehaviourVariableValue("TEAM", unit.team.GUID, key);
      if (behaviourVariableValue != null) return behaviourVariableValue;

      return null;
    }

    public BehaviorVariableValue GetBehaviourVariableValue(string type, AbstractActor unit, string key) {
      return GetBehaviourVariableValue(type, unit.GUID, key);
    }

    // TODO: Support global scopes [pilot personality, unit role, ai skill] on a later release
    // NOTE: Only supporting unit and lance to start with
    public BehaviorVariableValue GetBehaviourVariableValue(string type, string ownerGuid, string key) {
      if (this.customBehaviourVariables.ContainsKey(type)) {
        Dictionary<string, CustomBehaviorVariableScope> typeScopes = this.customBehaviourVariables[type];
        if (typeScopes.ContainsKey(ownerGuid)) {
          CustomBehaviorVariableScope customScope = typeScopes[ownerGuid];

          BehaviorVariableValue behaviourVariableValue = customScope.GetVariable(key);
          if (behaviourVariableValue != null) return behaviourVariableValue;
        }
      }
      return null;
    }

    public void ResetCustomBehaviourVariableScopes() {
      customBehaviourVariables.Clear();
    }

    public void IssueAiOrder(string type, string ownerGuid, CustomAIOrder aiOrder) {
      if (this.customBehaviourVariables.ContainsKey(type)) {
        Dictionary<string, CustomBehaviorVariableScope> typeScopes = this.customBehaviourVariables[type];
        if (typeScopes.ContainsKey(ownerGuid)) {
          CustomBehaviorVariableScope customScope = typeScopes[ownerGuid];
          customScope.IssueAIOrder(aiOrder);
        }
      }
    }
  }
}