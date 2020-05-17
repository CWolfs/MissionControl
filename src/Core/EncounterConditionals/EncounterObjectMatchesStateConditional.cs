using BattleTech;
using BattleTech.Framework;

using MissionControl.Messages;

namespace MissionControl.Conditional {
  public class EncounterObjectMatchesStateConditional : DesignConditional {
    public string EncounterGuid { get; set; }
    public EncounterObjectStatus State { get; set; } = EncounterObjectStatus.Nothing;

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);
      EncounterObjectStateChangeMessage encounterMessage = message as EncounterObjectStateChangeMessage;

      // Main.LogDebug("[EncounterObjectMatchesStateConditional] Evaluating...");

      if (State == EncounterObjectStatus.Nothing) {
        Main.Logger.LogError($"[EncounterObjectMatchesStateConditional] Trying to use this conditional without setting State to check against. You must set state.");
        return false;
      }

      if (encounterMessage != null && encounterMessage.EncounterGuid == this.EncounterGuid) {
        base.LogEvaluationPassed("[EncounterObjectMatchesStateConditional] Encounter guid matches guid of message.", responseName);

        if (encounterMessage.State == this.State) {
          Main.LogDebug($"[EncounterObjectMatchesStateConditional] Encounter guid and State matched for '{responseName}'");
          return true;
        }
      }

      // Main.LogDebug($"[EncounterObjectMatchesStateConditional] Encounter guid and/or State did NOT match for '{responseName}'");
      base.LogEvaluationFailed("[EncounterObjectMatchesStateConditional] Encounter guid did NOT match guid of message.", responseName);
      return false;
    }
  }
}