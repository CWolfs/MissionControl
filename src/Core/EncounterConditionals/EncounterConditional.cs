using BattleTech.Framework;

using MissionControl.Logic;

/*
	Conditionals are used by Triggers. If the condition is used on a Trigger and that condition is met
  then the Trigger can continue to process itself.
*/
namespace MissionControl.Conditional {
  public abstract class EncounterConditional : DesignConditional {
    public EncounterConditional() { }
  }
}