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
      Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 1: Method entered");

      if (decisionLogic.conversationContent == null || decisionLogic.conversationContent.contents.Length == 0) {
        Main.Logger.LogWarning($"[InterruptDialogDecisionSequence] Decision dialogue '{decisionLogic.encounterObjectGuid}' has no content, skipping");
        SetState(DialogState.Finished);
        return;
      }

      Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 2: Content validation passed");

      // Mark as shown
      decisionLogic.dialogueShownStatus = DialogueShownStatus.Shown;

      // Get first dialogue content
      DialogueContent content = decisionLogic.conversationContent.contents[0];
      Main.Logger.Log($"[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 3: Got content, words='{content.words?.Substring(0, Math.Min(50, content.words?.Length ?? 0))}'");

      // Filter options by Conditionals
      visibleOptions = FilterVisibleOptions();
      Main.Logger.Log($"[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 4: Filtered options, visible count={visibleOptions.Count}");

      if (visibleOptions.Count == 0) {
        Main.Logger.LogWarning($"[InterruptDialogDecisionSequence] No visible options for decision dialogue '{decisionLogic.encounterObjectGuid}', skipping");
        SetState(DialogState.Finished);
        return;
      }

      // Create response data for SGDialogWidget
      List<SimGameConversationManager.ResponseData> responses = CreateResponses();
      Main.Logger.Log($"[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 5: Created {responses.Count} responses");

      // Check for null parameters before Show() call
      Main.Logger.Log($"[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 6: Null checks:");
      Main.Logger.Log($"  - content.words: {(content.words == null ? "NULL" : "OK")}");
      Main.Logger.Log($"  - content.CastDef: {(content.CastDef == null ? "NULL" : "OK")}");
      Main.Logger.Log($"  - dialogWidget: {(dialogWidget == null ? "NULL" : "OK")}");
      Main.Logger.Log($"  - responses: {(responses == null ? "NULL" : $"OK ({responses.Count} items)")}");

      // Ensure CastDef is not null (create default if needed)
      CastDef castDef = content.CastDef;
      if (castDef == null) {
        Main.Logger.LogWarning("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 7: CastDef is NULL, creating default");
        castDef = new CastDef();
        castDef.firstName = "Unknown";
        castDef.lastName = "";
        castDef.rank = "";
        castDef.sgCharType = SimGameState.SimGameCharacterType.UNSET;
      } else {
        Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 7: CastDef is valid");
      }

      // Get game context
      GameContext gameContext = UnityGameInstance.BattleTechGame.GetActiveContext();
      Main.Logger.Log($"[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 8: GameContext: {(gameContext == null ? "NULL" : "OK")}");

      // Set Sim to fix null reference crash in Show() (lines 328 & 414)
      // Store original so we can restore it later (dialogWidget is reused)
      originalSim = dialogWidget.Sim;
      var simGame = UnityGameInstance.BattleTechGame.Simulation;
      if (simGame != null) {
        Main.Logger.Log($"[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 8a: Setting dialogWidget.Sim, UXAttached={simGame.UXAttached}");
        dialogWidget.Sim = simGame;

        if (simGame.UXAttached) {
          Main.Logger.LogWarning("[InterruptDialogDecisionSequence.ShowDecisionDialogue] WARNING: Sim.UXAttached is TRUE in combat - animations may use wrong mode!");
        }
      } else {
        Main.Logger.LogWarning("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 8a: Could not get Simulation from BattleTechGame!");
      }

      Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 9: About to call dialogWidget.Show()");

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

      Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 10: dialogWidget.Show() completed successfully");

      LazySingletonBehavior<UIManager>.Instance.ToggleUINode = false;
      LazySingletonBehavior<UIManager>.Instance.ToggleInWorldNode = false;
      Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 11: UI toggles set");

      // Play audio if specified
      if (content.HasAudioEvent()) {
        Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 12: Playing audio");
        AudioEventManager.DialogSequencePlaying = true;
        DialogueAudioEvent.Play(combat, content, null);
      } else {
        Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 12: No audio to play");
      }

      Main.Logger.Log("[InterruptDialogDecisionSequence.ShowDecisionDialogue] STEP 13: Method completed successfully");
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

        responses.Add(responseData);
      }

      return responses;
    }

    private SimGameConversationManager.ConversationState OnResponseSelected(int responseIndex) {
      Main.Logger.Log($"[InterruptDialogDecisionSequence.OnResponseSelected] Button clicked, responseIndex={responseIndex}");

      if (responseIndex < 0 || responseIndex >= visibleOptions.Count) {
        Main.Logger.LogError($"[InterruptDialogDecisionSequence] Invalid response index: {responseIndex}, visibleOptions.Count={visibleOptions.Count}");
        SetState(DialogState.Finished);
        return SimGameConversationManager.ConversationState.NODE;
      }

      DialogueDecisionOption selectedOption = visibleOptions[responseIndex];
      Main.Logger.Log($"[InterruptDialogDecisionSequence.OnResponseSelected] Response selected: '{selectedOption.ResponseText}'");

      // Execute results for the selected option
      if (selectedOption.Results?.Count > 0) {
        Main.Logger.Log($"[InterruptDialogDecisionSequence.OnResponseSelected] Executing {selectedOption.Results.Count} result(s)");
        int resultIndex = 0;
        foreach (var result in selectedOption.Results) {
          try {
            Main.Logger.Log($"[InterruptDialogDecisionSequence.OnResponseSelected] Executing result {resultIndex}: {result.GetType().Name}");
            result.Trigger(null, decisionLogic.encounterObjectGuid);
            Main.Logger.Log($"[InterruptDialogDecisionSequence.OnResponseSelected] Result {resultIndex} executed successfully");
            resultIndex++;
          } catch (Exception e) {
            Main.Logger.LogError($"[InterruptDialogDecisionSequence.OnResponseSelected] Error executing result {resultIndex}: {e.Message}\n{e.StackTrace}");
          }
        }
      } else {
        Main.Logger.Log("[InterruptDialogDecisionSequence.OnResponseSelected] No results to execute");
      }

      // Handle branching
      if (!string.IsNullOrEmpty(selectedOption.NextDialogueGuid)) {
        // Branch to different dialogue
        Main.Logger.Log($"[InterruptDialogDecisionSequence] Branching to dialogue: {selectedOption.NextDialogueGuid}");
        combat.MessageCenter.PublishMessage(new TriggerDialog(selectedOption.NextDialogueGuid, false));
        SetState(DialogState.Finished);
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
      dialogWidget.Hide();
      LazySingletonBehavior<UIManager>.Instance.ToggleUINode = true;
      LazySingletonBehavior<UIManager>.Instance.ToggleInWorldNode = true;
    }

    public override void OnComplete() {
      base.OnComplete();

      // Restore original Sim value (dialogWidget is reused)
      // Do this AFTER button clicks are processed to avoid null reference in ResponseClicked
      if (originalSim != dialogWidget.Sim) {
        dialogWidget.Sim = originalSim;
        Main.Logger.Log("[InterruptDialogDecisionSequence.OnComplete] Restored original Sim value");
      }

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

      // Restore original Sim value when canceled
      if (originalSim != dialogWidget.Sim) {
        dialogWidget.Sim = originalSim;
        Main.Logger.Log("[InterruptDialogDecisionSequence.OnCanceled] Restored original Sim value");
      }

      HideDialogWidget();
      SetState(DialogState.Finished);
    }
  }
}
