using System.Linq;
using System.Reflection;

using Harmony;

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

    private AiManager() {}

    public void AddCustomRootLeafBehaviourSequences(BehaviorTree behaviourTree, BehaviorTreeIDEnum behaviourTreeType) {
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
  }
}