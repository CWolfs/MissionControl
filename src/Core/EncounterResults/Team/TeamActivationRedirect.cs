using System;
using System.Collections.Generic;

using BattleTech;

namespace MissionControl.Result {
  public static class TeamActivationRedirect {
    private struct RedirectEntry {
      public Team OriginalTeam;
      public int Round;
    }

    private static readonly Dictionary<string, RedirectEntry> PendingRedirects = new Dictionary<string, RedirectEntry>();

    public static void Clear() {
      PendingRedirects.Clear();
    }

    public static void PrepareActorForTeamSwitch(CombatGameState combat, AbstractActor actor, Team oldTeam, Team newTeam) {
      if (combat == null || actor == null || oldTeam == null || newTeam == null || oldTeam == newTeam) {
        return;
      }

      int currentRound = combat.TurnDirector?.CurrentRound ?? 0;

      // Preserve the original activating team if this actor is swapped more than once during the same activation.
      if (PendingRedirects.TryGetValue(actor.GUID, out RedirectEntry existing)) {
        if (existing.Round == currentRound) {
          if (newTeam == existing.OriginalTeam) {
            PendingRedirects.Remove(actor.GUID);
          }
          return;
        }
        PendingRedirects.Remove(actor.GUID);
      }

      List<OrderSequence> inFlightOrders = GetInFlightOrderSequences(combat.StackManager, actor);
      bool hasInFlightOrder = inFlightOrders.Count > 0;
      bool hasBegunActivation = actor.HasBegunActivation && !actor.HasActivatedThisRound;

      AITeam oldAiTeam = oldTeam as AITeam;
      bool oldTeamIsActivating = oldTeam.IsActive || oldTeam.ActivationSequence != null || (oldAiTeam != null && oldAiTeam.runningInterruptPhase);

      if (hasInFlightOrder) {
        if (oldAiTeam != null && oldTeamIsActivating && oldAiTeam.currentUnit == actor) {
          oldAiTeam.pendingInvocations?.Clear();
        }

        if (oldTeamIsActivating) {
          PendingRedirects[actor.GUID] = new RedirectEntry {
            OriginalTeam = oldTeam,
            Round = currentRound
          };

          // If none of the actor's top-level in-flight orders already consume activation (e.g. a walk
          // before firing, or an attack with Move After Shooting), queue DoneWithActor once the root
          // order finishes so Mechs still run end-of-turn heat/heat-sink resolution before completing
          // activation under oldTeam, and oldTeam's AI never issues follow-up orders for newTeam's unit.
          bool hasConsumingTopLevelOrder = false;
          OrderSequence rootOrderToFinish = null;
          for (int i = 0; i < inFlightOrders.Count; i++) {
            OrderSequence order = inFlightOrders[i];
            if (order.hasOwningSequence) {
              continue;
            }
            if (order.ConsumesActivation) {
              hasConsumingTopLevelOrder = true;
            } else if (rootOrderToFinish == null || order.SequenceGUID == order.RootSequenceGUID) {
              rootOrderToFinish = order;
            }
          }

          if (!hasConsumingTopLevelOrder && rootOrderToFinish != null) {
            CombatGameState capturedCombat = combat;
            AbstractActor capturedActor = actor;
            rootOrderToFinish.CompletedCallback = (SequenceFinished)Delegate.Combine(
              rootOrderToFinish.CompletedCallback,
              (SequenceFinished)(() => {
                if (!capturedActor.HasActivatedThisRound && !capturedActor.IsCompletingActivation) {
                  if (capturedActor.IsDead) {
                    capturedActor.OnActivationEnd(capturedActor.GUID, -1);
                  } else {
                    IStackSequence doneSequence = capturedActor.DoneWithActor();
                    capturedCombat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(doneSequence));
                  }
                }
              }));
          }

          Main.LogDebug($"[TeamActivationRedirect] Actor '{actor.LogDisplayName}' has {inFlightOrders.Count} in-flight order(s) under '{oldTeam.Name}'. Redirecting completion from '{newTeam.Name}' back to '{oldTeam.Name}'.");
        }
        return;
      }

      if (oldAiTeam != null && oldTeamIsActivating && oldAiTeam.currentUnit == actor) {
        oldAiTeam.pendingInvocations?.Clear();

        if (hasBegunActivation) {
          // Queue the normal DoneWithActor sequence so Mechs still run end-of-turn heat/heat sinks,
          // and redirect its completion back to oldTeam.
          if (!oldAiTeam.runningInterruptPhase) {
            oldAiTeam.isComplete = true;
          }
          PendingRedirects[actor.GUID] = new RedirectEntry {
            OriginalTeam = oldTeam,
            Round = currentRound
          };
          IStackSequence doneSequence = actor.DoneWithActor();
          combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(doneSequence));
          Main.LogDebug($"[TeamActivationRedirect] Queued DoneWithActor for partially activated '{actor.LogDisplayName}' on '{oldTeam.Name}' before switching to '{newTeam.Name}'.");
        } else {
          oldAiTeam.currentUnit = null;
          if (oldAiTeam.runningInterruptPhase) {
            AbortOrAdvanceInterruptPhase(combat, oldAiTeam, actor);
          } else {
            oldAiTeam.isComplete = true;
            oldAiTeam.SkipActivation();
            oldAiTeam.ActivationSequence = null;
          }
          Main.LogDebug($"[TeamActivationRedirect] Aborted pending AI selection of '{actor.LogDisplayName}' on '{oldTeam.Name}' before switching to '{newTeam.Name}'.");
        }
        return;
      }

      if (hasBegunActivation && oldTeamIsActivating) {
        PendingRedirects[actor.GUID] = new RedirectEntry {
          OriginalTeam = oldTeam,
          Round = currentRound
        };
        IStackSequence doneSequence = actor.DoneWithActor();
        combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(doneSequence));
        Main.LogDebug($"[TeamActivationRedirect] Queued DoneWithActor for partially activated '{actor.LogDisplayName}' on '{oldTeam.Name}' before switching to '{newTeam.Name}'.");
      }
    }

    public static bool TryConsumeRedirect(string actorGuid, Team currentTeam, out Team originalTeam) {
      originalTeam = null;
      if (string.IsNullOrEmpty(actorGuid) || currentTeam == null) {
        return false;
      }

      if (!PendingRedirects.TryGetValue(actorGuid, out RedirectEntry entry)) {
        return false;
      }

      PendingRedirects.Remove(actorGuid);

      int currentRound = currentTeam.Combat?.TurnDirector?.CurrentRound ?? 0;
      if (entry.Round != currentRound || entry.OriginalTeam == null || entry.OriginalTeam == currentTeam) {
        return false;
      }

      originalTeam = entry.OriginalTeam;
      return true;
    }

    public static void CompleteEmptyInterruptPhaseIfNeeded(AITeam aiTeam) {
      if (aiTeam == null || !aiTeam.runningInterruptPhase) {
        return;
      }

      if (aiTeam.GetUnusedInterruptUnits().Count == 0) {
        FinishInterruptPhase(aiTeam.Combat, aiTeam);
      }
    }

    private static void AbortOrAdvanceInterruptPhase(CombatGameState combat, AITeam oldAiTeam, AbstractActor excludedActor) {
      List<AbstractActor> unusedInterruptUnits = oldAiTeam.GetUnusedInterruptUnits();
      for (int i = 0; i < unusedInterruptUnits.Count; i++) {
        if (unusedInterruptUnits[i] != excludedActor) {
          oldAiTeam.ClearInterruptUnit();
          return;
        }
      }

      // Vanilla AITeam.OnInterruptUpdate() calls UpdateAloneStatus(null) and throws NRE when
      // selectCurrentInterruptUnit() returns null. Finish the interrupt phase directly instead.
      FinishInterruptPhase(combat, oldAiTeam);
    }

    private static void FinishInterruptPhase(CombatGameState combat, AITeam aiTeam) {
      aiTeam.runningInterruptPhase = false;
      aiTeam.currentUnit = null;
      aiTeam.isComplete = true;
      CompleteInterruptPhaseSequences(combat?.StackManager, aiTeam.GUID);
    }

    private static void CompleteInterruptPhaseSequences(StackManager stackManager, string teamGuid) {
      List<IStackSequence> stack = stackManager?.SequenceStack;
      if (stack == null) {
        return;
      }

      for (int i = 0; i < stack.Count; i++) {
        CompleteMatchingInterruptPhaseSequence(stack[i], teamGuid);
      }
    }

    private static void CompleteMatchingInterruptPhaseSequence(IStackSequence sequence, string teamGuid) {
      if (sequence == null) {
        return;
      }

      if (sequence is InterruptPhaseSequence interruptPhase && interruptPhase.teamGuid == teamGuid) {
        interruptPhase.isComplete = true;
      }

      if (sequence is MultiSequence multiSequence && multiSequence.childSequences != null) {
        for (int i = 0; i < multiSequence.childSequences.Count; i++) {
          CompleteMatchingInterruptPhaseSequence(multiSequence.childSequences[i], teamGuid);
        }
      }

      if (sequence is InterruptSequence interruptSequence && interruptSequence.pendingSequences != null) {
        for (int i = 0; i < interruptSequence.pendingSequences.Count; i++) {
          CompleteMatchingInterruptPhaseSequence(interruptSequence.pendingSequences[i], teamGuid);
        }
      }
    }

    private static List<OrderSequence> GetInFlightOrderSequences(StackManager stackManager, AbstractActor actor) {
      List<OrderSequence> results = new List<OrderSequence>();
      if (stackManager == null || actor == null) {
        return results;
      }

      List<IStackSequence> stack = stackManager.SequenceStack;
      if (stack != null) {
        for (int i = 0; i < stack.Count; i++) {
          CollectActorOrderSequences(stack[i], actor, results);
        }
      }

      return results;
    }

    private static void CollectActorOrderSequences(IStackSequence sequence, AbstractActor actor, List<OrderSequence> results) {
      if (sequence == null) {
        return;
      }

      if (sequence is OrderSequence orderSequence && orderSequence.owningActor == actor) {
        results.Add(orderSequence);
      }

      if (sequence is MultiSequence multiSequence && multiSequence.childSequences != null) {
        for (int i = 0; i < multiSequence.childSequences.Count; i++) {
          CollectActorOrderSequences(multiSequence.childSequences[i], actor, results);
        }
      }

      if (sequence is InterruptSequence interruptSequence && interruptSequence.pendingSequences != null) {
        for (int i = 0; i < interruptSequence.pendingSequences.Count; i++) {
          CollectActorOrderSequences(interruptSequence.pendingSequences[i], actor, results);
        }
      }
    }
  }
}
