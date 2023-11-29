/**
	This result will activate the 'ResultsIfSkipped' results, 'Results' results or 'Cance' states on a DelayResult when triggered
*/
namespace MissionControl.Result {
  public class ActivateDelayTriggerResult : EncounterResult {
    public string Type = "SkipIf";
    public DelayResult DelayResult { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[ActivateDelaySkipResultsResult] Triggering");

      switch (Type) {
        case "SkipIf": {
          DelayResult.UseSkippedState();
          break;
        }
        case "CompleteEarly": {
          DelayResult.UseCompleteEarlyState();
          break;
        }
        case "Cancel": {
          DelayResult.UseCancelState();
          break;
        }
      }
    }
  }
}
