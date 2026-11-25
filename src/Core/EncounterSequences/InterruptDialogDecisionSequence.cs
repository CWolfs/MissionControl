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
    private SimGameState originalSim;
    private bool hasOverriddenSim;
    private bool hasShownWidget;
    private int currentContentIndex = -1;
    private int pendingContentIndex = -1;
    private string pendingNextDialogueGuid;

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

      if (dialogWidget.ResponseOptions != null && visibleOptions.Count > dialogWidget.ResponseOptions.Count) {
        Main.Logger.LogWarning($"[InterruptDialogDecisionSequence] Decision dialogue '{decisionLogic.encounterObjectGuid}' has {visibleOptions.Count} visible options but SGDialogWidget only supports {dialogWidget.ResponseOptions.Count}; clamping");
        visibleOptions = visibleOptions.GetRange(0, dialogWidget.ResponseOptions.Count);
      }

      // Create response data for SGDialogWidget
      List<SimGameConversationManager.ResponseData> responses = CreateResponses();

      // Ensure CastDef is not null (create default if needed)
      CastDef castDef = GetOrCreateCastDef(content);

      // Get game context
      GameContext gameContext = UnityGameInstance.BattleTechGame.GetActiveContext();

      // Set Sim to fix null reference crash in Show() (lines 328 & 414)
      // Store original once so we can restore it later (dialogWidget is reused)
      if (!hasOverriddenSim) {
        originalSim = dialogWidget.Sim;
        hasOverriddenSim = true;
      }

      var simGame = UnityGameInstance.BattleTechGame.Simulation;
      if (simGame != null) {
        dialogWidget.Sim = simGame;

        if (simGame.UXAttached) {
          Main.Logger.LogWarning("[InterruptDialogDecisionSequence] Sim.UXAttached is TRUE in combat - animations may use wrong mode!");
        }
      }

      if (dialogWidget.Sim == null) {
        Main.Logger.LogWarning($"[InterruptDialogDecisionSequence] Simulation is null (e.g. Skirmish mode) - cannot show response buttons for '{decisionLogic.encounterObjectGuid}', skipping");
        SetState(DialogState.Finished);
        return;
      }

      // Show dialogue with responses
      dialogWidget.Show(
        content.words,
        gameContext,
        castDef,
        content.emote,
        null, // no continue callback
        OnResponseSelected,
        responses,
        false // not end of convo
      );
      hasShownWidget = true;

      LazySingletonBehavior<UIManager>.Instance.ToggleUINode = false;
      LazySingletonBehavior<UIManager>.Instance.ToggleInWorldNode = false;

      // Play audio if specified
      if (content.HasAudioEvent()) {
        AudioEventManager.DialogSequencePlaying = true;
        DialogueAudioEvent.Play(combat, content, null);
      }
    }

    private CastDef GetOrCreateCastDef(DialogueContent content) {
      if (content.CastDef != null) {
        return content.CastDef;
      }

      Main.Logger.LogWarning("[InterruptDialogDecisionSequence] CastDef is NULL, creating default");
      CastDef castDef = new CastDef();
      castDef.firstName = "Unknown";
      castDef.lastName = "";
      castDef.rank = "";
      castDef.sgCharType = SimGameState.SimGameCharacterType.UNSET;
      return castDef;
    }

    private void ShowJumpedContent(int contentIndex) {
      if (decisionLogic.conversationContent == null || contentIndex < 0 || contentIndex >= decisionLogic.conversationContent.contents.Length) {
        Main.Logger.LogWarning($"[InterruptDialogDecisionSequence] Invalid jumped content index {contentIndex} for '{decisionLogic.encounterObjectGuid}', finishing");
        SetState(DialogState.Finished);
        return;
      }

      DialogueContent nextContent = decisionLogic.conversationContent.contents[contentIndex];
      CastDef castDef = GetOrCreateCastDef(nextContent);

      dialogWidget.Show(
        nextContent.words,
        UnityGameInstance.BattleTechGame.GetActiveContext(),
        castDef,
        nextContent.emote,
        AfterJumpedContentHide,
        null,
        null,
        true // end of convo
      );
      hasShownWidget = true;

      LazySingletonBehavior<UIManager>.Instance.ToggleUINode = false;
      LazySingletonBehavior<UIManager>.Instance.ToggleInWorldNode = false;

      if (nextContent.HasAudioEvent()) {
        AudioEventManager.DialogSequencePlaying = true;
        DialogueAudioEvent.Play(combat, nextContent, null);
      }
    }

    public override void OnUpdate() {
      base.OnUpdate();

      if (state == DialogState.Talking && pendingContentIndex >= 0) {
        int targetIndex = pendingContentIndex;
        pendingContentIndex = -1;
        currentContentIndex = targetIndex;
        ShowJumpedContent(targetIndex);
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

    private bool EvaluateConditional(GenericCompoundConditional conditional) {
      try {
        // GenericCompoundConditional.Evaluate() handles ANY/ALL/NONE logic based on whichMustBeTrue
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
        responseData.link.responseText = option.ResponseText;
        responseData.link.idRef = new IDRef(); // Empty IDRef
        responseData.link.idRef.id = Guid.NewGuid().ToString(); // Unique ID to avoid conflicts

        responses.Add(responseData);
      }

      return responses;
    }

    private SimGameConversationManager.ConversationState OnResponseSelected(int responseIndex) {
      Main.Logger.Log($"[InterruptDialogDecisionSequence] Button clicked, responseIndex={responseIndex}");

      if (responseIndex < 0 || responseIndex >= visibleOptions.Count) {
        Main.Logger.LogError($"[InterruptDialogDecisionSequence] Invalid response index: {responseIndex}, visibleOptions.Count={visibleOptions.Count}");
        SetState(DialogState.Finished);
        return SimGameConversationManager.ConversationState.NODE;
      }

      DialogueDecisionOption selectedOption = visibleOptions[responseIndex];
      Main.Logger.Log($"[InterruptDialogDecisionSequence] Response selected: '{selectedOption.ResponseText}'");

      // Execute results for the selected option
      if (selectedOption.Results?.Count > 0) {
        for (int i = 0; i < selectedOption.Results.Count; i++) {
          try {
            selectedOption.Results[i].Trigger(null, decisionLogic.encounterObjectGuid);
          } catch (Exception e) {
            Main.Logger.LogError($"[InterruptDialogDecisionSequence] Error executing result {i}: {e.Message}\n{e.StackTrace}");
          }
        }
      }

      // Handle branching (deferred until after SGDialogOption.OnClick -> ResponseClicked finishes)
      if (!string.IsNullOrEmpty(selectedOption.NextDialogueGuid)) {
        // Branch to different dialogue on OnComplete so ResponseClicked / HideDialogWidget don't close it
        Main.Logger.Log($"[InterruptDialogDecisionSequence] Branching to dialogue: {selectedOption.NextDialogueGuid}");
        pendingNextDialogueGuid = selectedOption.NextDialogueGuid;
        SetState(DialogState.Finished);
      } else if (selectedOption.NextContentIndex >= 0 && selectedOption.NextContentIndex < decisionLogic.conversationContent.contents.Length) {
        // Defer Show() to next frame in OnUpdate() so SGDialogWidget.ResponseClicked() doesn't immediately hide it
        Main.Logger.Log($"[InterruptDialogDecisionSequence] Queueing jump to content index: {selectedOption.NextContentIndex}");
        pendingContentIndex = selectedOption.NextContentIndex;
        return SimGameConversationManager.ConversationState.NODE;
      } else {
        // Sequential (default) - close and complete
        SetState(DialogState.Finished);
      }

      return SimGameConversationManager.ConversationState.NODE;
    }

    private SimGameConversationManager.ConversationState AfterJumpedContentHide() {
      SetState(DialogState.Finished);
      return SimGameConversationManager.ConversationState.NODE;
    }

    private void HideDialogWidget() {
      if (!hasShownWidget) return;
      hasShownWidget = false;
      dialogWidget.Hide();
      LazySingletonBehavior<UIManager>.Instance.ToggleUINode = true;
      LazySingletonBehavior<UIManager>.Instance.ToggleInWorldNode = true;
    }

    private void RestoreSim() {
      if (hasOverriddenSim) {
        dialogWidget.Sim = originalSim;
        hasOverriddenSim = false;
      }
    }

    public override void OnComplete() {
      base.OnComplete();

      // Restore original Sim value (dialogWidget is reused)
      // Do this AFTER button clicks are processed to avoid null reference in ResponseClicked
      RestoreSim();

      AudioEventManager.DialogSequencePlaying = false;
      combat.MessageCenter.PublishMessage(new DialogComplete(decisionLogic.encounterObjectGuid));

      if (!string.IsNullOrEmpty(pendingNextDialogueGuid)) {
        string nextDialogueGuid = pendingNextDialogueGuid;
        pendingNextDialogueGuid = null;
        combat.MessageCenter.PublishMessage(new TriggerDialog(nextDialogueGuid, false));
      }
    }

    public override void OnSuspend() {
      base.OnSuspend();
      HideDialogWidget();
    }

    public override void OnResume() {
      base.OnResume();
      if (state == DialogState.Finished) {
        return;
      }

      if (currentContentIndex >= 0) {
        ShowJumpedContent(currentContentIndex);
      } else {
        ShowDecisionDialogue();
      }
    }

    public override void OnCanceled() {
      base.OnCanceled();

      RestoreSim();
      pendingContentIndex = -1;
      pendingNextDialogueGuid = null;

      HideDialogWidget();
      SetState(DialogState.Finished);
    }
  }
}
