using System.Collections.Generic;

using BattleTech;

namespace MissionControl.Logic {
  /// <summary>
  /// A dialogue type that presents multiple decision buttons to the player.
  /// Each button can trigger different results when clicked.
  /// Inherits all standard DialogueGameLogic behavior including:
  /// - ApplyContractOverride() for text from contract JSON
  /// - conversationContent for dialogue display
  /// - showOnlyOnce and dialogueShownStatus
  /// - TriggerDialogue() to send TriggerDialog message
  /// </summary>
  public class DialogueDecisionGameLogic : DialogueGameLogic {
    public List<DialogueDecisionOption> decisionOptions;

    public DialogueDecisionGameLogic() {
      decisionOptions = new List<DialogueDecisionOption>();
    }

    public override TaggedObjectType Type => TaggedObjectType.Dialogue;
  }
}
