/**
	This result will activate the 'ResultsIfSkipped' results on a DelayResult when triggered
*/
namespace MissionControl.Result {
  public class ActivateDelaySkipResultsResult : EncounterResult {
    public DelayResult DelayResult { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[ActivateDelaySkipResultsResult] Triggering");
      DelayResult.SetUseSkippedState(true);
    }
  }
}
