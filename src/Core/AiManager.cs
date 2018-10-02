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

    public void AddCustomRootLeafBehaviourSequences(BehaviorTree behaviourTree) {
      
    }
  }
}