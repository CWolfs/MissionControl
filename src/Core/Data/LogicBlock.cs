namespace MissionControl.Logic {
  public abstract class LogicBlock {
    public enum LogicType { NONE, RESOURCE_REQUEST, CONTRACT_OVERRIDE_MANIPULATION, ENCOUNTER_MANIPULATION, REQUEST_LANCE_COMPLETE, SCENE_MANIPULATION }

    public LogicType Type { get; protected set; } = LogicType.NONE;

    public abstract void Run(RunPayload payload);
  }
}