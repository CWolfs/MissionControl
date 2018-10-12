using MissionControl.Logic;

namespace MissionControl.Trigger {
  public abstract class EncounterTrigger : LogicBlock {
    public EncounterTrigger() {
      this.Type = LogicType.ENCOUNTER_MANIPULATION;
    }
  }
}