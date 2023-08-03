namespace MissionControl.Logic {
  public class GenericTriggerPayload : RunPayload {
    public bool ShouldManuallyInitialise { get; private set; }

    public GenericTriggerPayload(bool shouldManuallyInitialise) {
      ShouldManuallyInitialise = shouldManuallyInitialise;
    }
  }
}