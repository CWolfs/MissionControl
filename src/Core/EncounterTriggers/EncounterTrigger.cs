using MissionControl.Logic;

/*
	Triggers are used by the Encounter Layer Data to listen to message event (e.g OnEncounterBegin), check a Condition
  and if that condition is met it will do two things
    - Trigger all the associated Results
    - Transmit a signal messages to associated GUIDs
*/
namespace MissionControl.Trigger {
  public abstract class EncounterTrigger : LogicBlock {
    public EncounterTrigger() {
      this.Type = LogicType.ENCOUNTER_MANIPULATION;
    }
  }
}