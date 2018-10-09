using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Harmony;

using MissionControl.Data;
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

    private Dictionary<BehaviorTreeIDEnum, List<BranchInjectionData>> injectionBranchRoots = new Dictionary<BehaviorTreeIDEnum, List<BranchInjectionData>>();

    private AiManager() {
      // Test
      // AddCustomBehaviourBranch(BehaviorTreeIDEnum.CoreAITree, new string[] { "test " }, new SequenceNode("test_sequence_0000", ))
    }

    public void LoadCustomBehaviourSequences(BehaviorTree behaviourTree, BehaviorTreeIDEnum behaviourTreeType) {
      BehaviorNode rootNode = behaviourTree.RootNode;

      if (behaviourTreeType == BehaviorTreeIDEnum.CoreAITree) {
        SelectorNode coreAiRoot = (SelectorNode)rootNode;
        coreAiRoot.Children.FindIndex(child => {
          string name = (string)AccessTools.Field(typeof(BehaviorNode), "name").GetValue(child);
          Main.Logger.Log($"[AddCustomRootLeafBehaviourSequences] BehaviourNode name is '{name}'");
          return false;
        });
      }
    }

    public void AddCustomBehaviourBranch(BehaviorTreeIDEnum behaviourTreeType, string[] path, CompositeBehaviorNode compositeNode) {
      if (!injectionBranchRoots.ContainsKey(behaviourTreeType)) injectionBranchRoots.Add(behaviourTreeType, new List<BranchInjectionData>());
      injectionBranchRoots[behaviourTreeType].Add(new BranchInjectionData(path, compositeNode));
    }
  }
}