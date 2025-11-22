using System;
using System.Collections.Generic;

using BattleTech.Framework;

using MissionControl.Result;

namespace MissionControl.Logic {
  /// <summary>
  /// Represents a single button option in a DialogueDecisionGameLogic
  /// </summary>
  [Serializable]
  public class DialogueDecisionOption {
    /// <summary>
    /// Text displayed on the button
    /// </summary>
    public string ButtonText { get; set; }

    /// <summary>
    /// Results/actions to execute when button is clicked
    /// </summary>
    public List<DesignResult> Results { get; set; }

    /// <summary>
    /// Whether to close the dialogue after clicking this button (default: true)
    /// </summary>
    public bool CloseOnClick { get; set; } = true;

    /// <summary>
    /// Optional conditional for show/hide button logic
    /// If null or evaluates to true, button is shown
    /// If evaluates to false, button is hidden
    /// </summary>
    public DesignConditional Conditional { get; set; }

    /// <summary>
    /// Optional GUID of next dialogue to branch to
    /// If set, triggers this dialogue after executing results
    /// Takes priority over NextContentIndex if both are set
    /// </summary>
    public string NextDialogueGuid { get; set; }

    /// <summary>
    /// Optional content index to jump to within the same dialogue
    /// -1 = sequential (default), 0+ = jump to specific index
    /// Only used if NextDialogueGuid is null or empty
    /// </summary>
    public int NextContentIndex { get; set; } = -1;

    public DialogueDecisionOption() {
      Results = new List<DesignResult>();
    }

    public DialogueDecisionOption(string buttonText, List<DesignResult> results, bool closeOnClick = true) {
      ButtonText = buttonText;
      Results = results ?? new List<DesignResult>();
      CloseOnClick = closeOnClick;
    }
  }
}
