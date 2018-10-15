using BattleTech.Framework;

using MissionControl.Logic;

/*
	Results are used by the Encounter Layer Data after a Trigger has successfully matched its event call and Condition.
*/
namespace MissionControl.Result {
  public abstract class EncounterResult : DesignResult {
    public EncounterResult() { }
  }
}