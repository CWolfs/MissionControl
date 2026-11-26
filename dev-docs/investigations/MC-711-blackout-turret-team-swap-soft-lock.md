# MC-711 — Blackout Turret Team-Swap Soft-Lock

**Status:** Fixed on `develop`
**Issue:** https://github.com/CWolfs/MissionControl/issues/711

## Symptom

Intermittent soft-lock on `Blackout_PreparedGrounds` after the **1c Cache Ambush** dialogue closed. Preceded by this engine warning:

```
CombatLog.RoundSequence [WARNING] No team activation sequence found!
team[EmployerTeam], actorGuid[0329e03f-9d9c-4a5d-99df-ccdb6afe2bb5.0], isDeferringAction[False]
```

The actor GUID matches Alpha turret spawn point 2 in `contractTypeBuilds/Blackout/common.jsonc:82`.

## Root Cause

When `SetTeamByLanceSpawnerGuidResult` moved the 4 Alpha turrets from `NeutralToAll` to `EmployerTeam`, one of those turrets was already mid-activation under `NeutralToAll`:

1. In `Blackout_PreparedGrounds`, `Chunk_Optional_Initial_Enemy` is disabled so combat starts non-interleaved (`IsInterleaved == false`, Phase 5) with 4 player mechs and 7 `NeutralToAll` turrets (4 Alpha + 3 Bravo).
2. If the move that satisfies `Objective_Investigate_Region` (`NumberOfUnitsToOccupy: 2`, `DurationType: AfterMoveComplete`) also ends `Player 1`'s turn (`AllUnitsDoneMoving == true`), `ObjectiveGameLogic.CheckObjective()` enqueues `ObjectiveSucceeded` followed by `ObjectiveUpdated` into `EncounterLayerParent.messageQueue`.
3. `EncounterLayerParent.UpdateLoadAwareMessageQueue()` only dequeues **one message per frame**, and `TurnDirector.OnTurnActorActivateComplete()` does not wait on that queue. `TurnDirector` immediately hands control to `NeutralToAll`, which starts stepping through its 7 turrets one by one.
4. When `ObjectiveUpdated` dequeues 1–2 frames later, `NeutralToAll` is actively executing an Alpha turret (`NeutralToAll.ActivationSequence` is live on `StackManager.SequenceStack`, with either `AITeam.currentUnit == turret` or `DoneWithActorSequence(turret)` already on the stack).
5. `Trigger_1c_Turret_Activation` runs `SetTeamByLanceSpawnerGuidResult`, moving the 4 Alpha turrets from `NeutralToAll` to `EmployerTeam`. Because `NeutralToAll` still owns the 3 Bravo turrets, `Team.RemoveUnit()` -> `TurnDirector.CheckAndFixNoUnitsCondition()` sees `GetNumUnusedUnitsForCurrentPhase() > 0` and does **not** call `NeutralToAll.SkipActivation()`.
6. `DialogueActivator` pushes `InterruptDialogQueueSequence` onto the top of the stack, pausing the turret's in-flight sequence underneath.
7. Once the player closes the ambush dialogue, `DoneWithActorSequence(turret)` completes -> `AbstractActor.OnActivationEnd()` calls `team.OnTurnActorActivationComplete(GUID)` on the turret's **new** team (`EmployerTeam`), which has `ActivationSequence == null` (logging the warning). `NeutralToAll.CompleteActivation()` is never called, leaving `TeamActivationSequence(NeutralToAll)` stuck at the top of `StackManager.SequenceStack` with `ForceComplete == false` forever.

`SetTeamByTagResult` had the same latent bug when moving `AbstractActor`s mid-activation.

## Fix

- Added `TeamActivationRedirect` (`src/Core/EncounterResults/Team/TeamActivationRedirect.cs`) and called `PrepareActorForTeamSwitch` before detaching units in both `SetTeamByLanceSpawnerGuidResult` and `SetTeamByTagResult`:
  - **In-flight `OrderSequence` on `StackManager` (including `MultiSequence` children and `InterruptSequence.pendingSequences`):** clears `oldAiTeam.pendingInvocations` if applicable, records `(actorGUID -> oldTeam, currentRound)`, and if none of the actor's top-level orders already consume activation, attaches a `CompletedCallback` to the root order to queue `actor.DoneWithActor()` so Mechs still run end-of-turn heat/heat-sink resolution and `oldTeam`'s AI does not issue follow-up orders for `newTeam`'s unit.
  - **`AITeam.currentUnit == actor` with no in-flight order yet:** clears `pendingInvocations`, and either queues `actor.DoneWithActor()` with a redirect if `HasBegunActivation` is already true, or cleanly aborts the pending selection (`isComplete = true`, `SkipActivation()`, `ActivationSequence = null`; or advances/finishes the interrupt phase directly when no other interrupt units remain, avoiding vanilla's `UpdateAloneStatus(null)` NRE) so the actor enters `newTeam` unused.
  - **Non-AI team with `HasBegunActivation && !HasActivatedThisRound` and no in-flight order:** records a redirect and queues `actor.DoneWithActor()` so end-of-turn heat and activation completion resolve cleanly under `oldTeam`.
- Added `TeamOnTurnActorActivationCompletePatch` (`src/Patches/Bug Fixes/TeamOnTurnActorActivationCompletePatch.cs`), a Prefix on `Team.OnTurnActorActivationComplete` that consumes any matching redirect and routes completion back to `originalTeam` (finishing an empty AI interrupt phase if needed, or publishing `TurnActorActivateCompleteMessage` without calling `CompleteActivation` if `originalTeam` has already closed its activation).
- Cleared redirect state in `MissionControl.OnCombatDestroyed()`.
- Removed the temporary `[MC-711]` diagnostic logging and `src/Patches/Debug/TeamCompleteActivationPatch.cs`.
