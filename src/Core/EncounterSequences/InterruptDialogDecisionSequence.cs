using UnityEngine;
using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.UI;
using BattleTech.Framework;
using HBS;

using MissionControl.Logic;
using isogame;

namespace MissionControl.EncounterSequences {
  /// <summary>
  /// Interrupt sequence that shows a DialogueDecisionGameLogic with multiple decision buttons
  /// using the vanilla SGDialogWidget response UI
  /// </summary>
  public class InterruptDialogDecisionSequence : InterruptSequence {
    private SGDialogWidget dialogWidget;
    private DialogueDecisionGameLogic decisionLogic;
    private CombatGameState combat;
    private List<DialogueDecisionOption> visibleOptions;
    private DialogState state;

    private bool isInterruptable;
    private bool isCancelable;

    public override bool IsParallelInterruptable => isInterruptable;
    public override bool IsCancelable => isCancelable;
    public override bool IsComplete => state == DialogState.Finished;

    public InterruptDialogDecisionSequence(
      CombatGameState combat,
      SGDialogWidget dialogWidget,
      DialogueDecisionGameLogic decisionLogic,
      bool isInterruptable = false,
      bool isCancelable = false) : base(combat) {
      this.combat = combat;
      this.dialogWidget = dialogWidget;
      this.decisionLogic = decisionLogic;
      this.isInterruptable = isInterruptable;
      this.isCancelable = isCancelable;
      this.state = DialogState.None;
      this.visibleOptions = new List<DialogueDecisionOption>();
    }

    public override void OnAdded() {
      base.OnAdded();
      SetState(DialogState.Talking);
    }

    private void SetState(DialogState newState) {
      if (state != newState) {
        state = newState;
        switch (state) {
          case DialogState.Talking:
            ShowDecisionDialogue();
            break;
          case DialogState.Finished:
            HideDialogWidget();
            break;
        }
      }
    }

    private void ShowDecisionDialogue() {
      if (decisionLogic.conversationContent == null || decisionLogic.conversationContent.contents.Length == 0) {
        Main.Logger.LogWarning($"[InterruptDialogDecisionSequence] Decision dialogue '{decisionLogic.encounterObjectGuid}' has no content, skipping");
        SetState(DialogState.Finished);
        return;
      }

      // Mark as shown
      decisionLogic.dialogueShownStatus = DialogueShownStatus.Shown;

      // Get first dialogue content
      DialogueContent content = decisionLogic.conversationContent.contents[0];

      // Filter options by Conditionals
      visibleOptions = FilterVisibleOptions();

      if (visibleOptions.Count == 0) {
        Main.Logger.LogWarning($"[InterruptDialogDecisionSequence] No visible options for decision dialogue '{decisionLogic.encounterObjectGuid}', skipping");
        SetState(DialogState.Finished);
        return;
      }

      // Create response data for SGDialogWidget
      List<SimGameConversationManager.ResponseData> responses = CreateResponses();

      // Show dialogue with responses
      dialogWidget.Show(
        content.words,
        UnityGameInstance.BattleTechGame.GetActiveContext(),
        content.CastDef,
        content.emote,
        null, // no continue callback
        OnResponseSelected,
        responses,
        false // not end of convo
      );

      LazySingletonBehavior<UIManager>.Instance.ToggleUINode = false;
      LazySingletonBehavior<UIManager>.Instance.ToggleInWorldNode = false;

      // Play audio if specified
      if (content.HasAudioEvent()) {
        AudioEventManager.DialogSequencePlaying = true;
        DialogueAudioEvent.Play(combat, content, null);
      }
    }

    private List<DialogueDecisionOption> FilterVisibleOptions() {
      List<DialogueDecisionOption> visible = new List<DialogueDecisionOption>();

      foreach (var option in decisionLogic.decisionOptions) {
        // Show if no conditional, or conditional evaluates to true
        if (option.Conditional == null || EvaluateConditional(option.Conditional)) {
          visible.Add(option);
        }
      }

      return visible;
    }

    private bool EvaluateConditional(DesignConditional conditional) {
      try {
        return conditional.Evaluate(null, decisionLogic.encounterObjectGuid);
      } catch (Exception e) {
        Main.Logger.LogError($"[InterruptDialogDecisionSequence] Error evaluating conditional: {e.Message}");
        return false; // Hide button on error
      }
    }

    private List<SimGameConversationManager.ResponseData> CreateResponses() {
      List<SimGameConversationManager.ResponseData> responses = new List<SimGameConversationManager.ResponseData>();

      for (int i = 0; i < visibleOptions.Count; i++) {
        DialogueDecisionOption option = visibleOptions[i];

        // Create ResponseData for SGDialogWidget
        SimGameConversationManager.ResponseData responseData = new SimGameConversationManager.ResponseData();
        responseData.index = i;
        responseData.link = new ConversationLink();
        responseData.link.responseText = option.ButtonText;
        responseData.link.idRef = new IDRef(); // Empty IDRef

        responses.Add(responseData);
      }

      return responses;
    }

    private SimGameConversationManager.ConversationState OnResponseSelected(int responseIndex) {
      if (responseIndex < 0 || responseIndex >= visibleOptions.Count) {
        Main.Logger.LogError($"[InterruptDialogDecisionSequence] Invalid response index: {responseIndex}");
        SetState(DialogState.Finished);
        return SimGameConversationManager.ConversationState.NODE;
      }

      DialogueDecisionOption selectedOption = visibleOptions[responseIndex];
      Main.Logger.Log($"[InterruptDialogDecisionSequence] Response selected: {selectedOption.ButtonText}");

      // Execute results for the selected option
      if (selectedOption.Results != null) {
        foreach (var result in selectedOption.Results) {
          try {
            result.Trigger(null, decisionLogic.encounterObjectGuid);
          } catch (Exception e) {
            Main.Logger.LogError($"[InterruptDialogDecisionSequence] Error executing result: {e.Message}\n{e.StackTrace}");
          }
        }
      }

      // Handle branching
      bool shouldClose = selectedOption.CloseOnClick;

      if (!string.IsNullOrEmpty(selectedOption.NextDialogueGuid)) {
        // Branch to different dialogue
        Main.Logger.Log($"[InterruptDialogDecisionSequence] Branching to dialogue: {selectedOption.NextDialogueGuid}");
        combat.MessageCenter.PublishMessage(new TriggerDialog(selectedOption.NextDialogueGuid, false));
        shouldClose = true; // Always close when branching to another dialogue
      } else if (selectedOption.NextContentIndex >= 0 && selectedOption.NextContentIndex < decisionLogic.conversationContent.contents.Length) {
        // Jump to specific content index within same dialogue
        Main.Logger.Log($"[InterruptDialogDecisionSequence] Jumping to content index: {selectedOption.NextContentIndex}");

        // Show the next content
        DialogueContent nextContent = decisionLogic.conversationContent.contents[selectedOption.NextContentIndex];
        dialogWidget.Show(
          nextContent.words,
          UnityGameInstance.BattleTechGame.GetActiveContext(),
          nextContent.CastDef,
          nextContent.emote,
          AfterJumpedContentHide,
          null,
          null,
          true // end of convo
        );

        return SimGameConversationManager.ConversationState.NODE;
      }
      // else: sequential (default) - just close and complete

      // Close dialogue if specified
      if (shouldClose) {
        SetState(DialogState.Finished);
      }

      return SimGameConversationManager.ConversationState.NODE;
    }

    private SimGameConversationManager.ConversationState AfterJumpedContentHide() {
      SetState(DialogState.Finished);
      return SimGameConversationManager.ConversationState.NODE;
    }

    private void HideDialogWidget() {
      dialogWidget.Hide();
      LazySingletonBehavior<UIManager>.Instance.ToggleUINode = true;
      LazySingletonBehavior<UIManager>.Instance.ToggleInWorldNode = true;
    }

    public override void OnComplete() {
      base.OnComplete();
      AudioEventManager.DialogSequencePlaying = false;
      combat.MessageCenter.PublishMessage(new DialogComplete(decisionLogic.encounterObjectGuid));
    }

    public override void OnSuspend() {
      base.OnSuspend();
      HideDialogWidget();
    }

    public override void OnResume() {
      base.OnResume();
      ShowDecisionDialogue();
    }

    public override void OnCanceled() {
      base.OnCanceled();
      HideDialogWidget();
      SetState(DialogState.Finished);
    }
  }
}
