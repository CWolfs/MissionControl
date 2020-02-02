using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Conditional {
  public class LanceDetectedConditional : DesignConditional {
    public string TargetLanceGuid { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);
      LanceDetectedMessage lanceDetectedMessage = message as LanceDetectedMessage;

      Main.LogDebug("[LanceIsDetectedConditional] Evaluating");
      if (lanceDetectedMessage != null) {
        Main.LogDebug($"[LanceIsDetectedConditional] Evaluating affectedObjectGuid of '{lanceDetectedMessage.affectedObjectGuid}' against target lance guid of '{this.TargetLanceGuid}'");
      }

      if (lanceDetectedMessage != null && lanceDetectedMessage.affectedObjectGuid.Contains(this.TargetLanceGuid)) {
        Main.LogDebug($"[LanceIsDetectedConditional] Lance guid matches affectedObject guid of message. '{responseName}'");
        return true;
      }
      Main.LogDebug($"[LanceIsDetectedConditional]  Lance guid did NOT match affectedObject guid of message. '{responseName}'");
      return false;
    }
  }
}