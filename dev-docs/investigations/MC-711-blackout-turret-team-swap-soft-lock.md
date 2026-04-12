# MC-711 — Blackout Turret Team-Swap Soft-Lock

**Status:** Open / intermittent / diagnostic logging shipped on `develop`
**Issue:** https://github.com/CWolfs/MissionControl/issues/711

## Symptom

One-off soft-lock on `Blackout_PreparedGrounds` after the **1c Cache Ambush** path. Observed at least once on `develop`; two re-tests failed to reproduce. Preceded by this game-engine warning:

```
CombatLog.RoundSequence [WARNING] No team activation sequence found!
team[EmployerTeam], actorGuid[0329e03f-9d9c-4a5d-99df-ccdb6afe2bb5.0]
```

The actor GUID matches Alpha turret spawn point 2 in `contractTypeBuilds/Blackout/common.jsonc:82`.

## Where the warning comes from

Decompiled game source (1.9.1): `BattleTech.Team.CompleteActivation()` at `Team.cs:1657`:

```csharp
protected void CompleteActivation(string actorGUID, bool isDeferringAction = false)
{
    DeferredThisActivation = isDeferringAction;
    if (ActivationSequence == null)
    {
        string message = $"No team activation sequence found! team[{Name}], actorGuid[{actorGUID}], isDeferringAction[{isDeferringAction}]";
        roundSequenceLogger.LogWarning(message);
    }
    else
    {
        ActivationSequence.ForceComplete = true;
        ActivationSequence = null;
    }
    …
}
```

`ActivationSequence` is only assigned in `Team.TurnActorProcessActivation()` (line 1620) when the team's turn begins in a phase, and is nulled at `CompleteActivation`. So the warning means: a unit's activation-complete message reached `EmployerTeam` without Employer ever having been put through `TurnActorProcessActivation` for that phase — i.e. the turret's activation "completed" on a team that was never officially activating.

## Flow (Blackout 1c Cache Ambush)

1. Alpha turrets spawn on `NeutralToAll`, lance-spawner GUID `223fd528-c333-4367-883c-a817acf24360`, tag `turrets_1b`. Spawn points:
   - `6826832c-83d5-4287-b349-33008dc1fac5`
   - `0329e03f-9d9c-4a5d-99df-ccdb6afe2bb5` ← the one in the warning
   - `e76ef06a-bd08-4c83-85f3-d8863acb3956`
   - `0044f994-2b5b-4185-a7d6-dc3f50f561ba`
2. Player completes first investigate objective → `Trigger_Enable_Chunk_1x` fires `SetStatusAtRandom` over chunks 1a/1b/1c (`common.jsonc:725`).
3. If 1c (ambush) is picked, chunk `9cdc19ff-5b22-402b-aca5-d593a6f3b69c` becomes Active:
   - `OnActiveExecute` plays dialogue `47c75ee8-…` (interrupt).
   - `Trigger_1c_Turret_Activation` fires on the state-change (`common.jsonc:821`) and runs:
     - `SetTeamByLanceSpawnerGuid` → Alpha turrets to `Employer`.
     - `SetTemporaryUnitPhaseInitiativeByTag` → Initiative `1` for `turrets_1b`.

Parallel triggers for the other paths (for reference when regression-checking):

| Path | Trigger | Team | Line |
|---|---|---|---|
| 1b trap | `Trigger_1b_Trap_Team_Swap` | Alpha → Target | 754 |
| 1c ambush (this bug) | `Trigger_1c_Turret_Activation` | Alpha → Employer | 821 |
| 3a enemy | `Trigger_3a_Enemy_Turret_Activation` | Bravo → Target | 1044 |
| 3a employer | `Trigger_3a_Employer_Turret_Activation` | Bravo → Employer | 1078 |

## Team-swap result (context only — not the bug)

`src/Core/EncounterResults/Team/SetTeamByLanceSpawnerGuidResult.cs`:

- Moves unit: `oldTeam.RemoveUnit(actor)` → `actor.AddToTeam(newTeam)` → `newTeam.AddUnit(actor)`.
- Updates spawner `teamDefinitionGuid`, encounter tags, lance membership, HUD, fog of war.
- Publishes `CombatantSwitchedTeams`.
- Does **not** touch `TurnDirector`, `RoundSequence`, or `StackManager.InsertInterruptPhase`.

**This is the same pattern vanilla BT uses** in `BattleTech.Framework.AssignUnitsToLanceResult.TriggerUnit` — so the "no TurnDirector touch" isn't a MissionControl novelty. Worth keeping in mind before we attribute the bug to missing TurnDirector integration.

## What we ruled out

- **`IsInterrupt: true` on dialogues** — has always defaulted to `true` in MissionControl (`DialogueActivator.cs:16`, `DialogueBuilder.cs:67`, `ResultsBuilder.cs:109,338`). Designer now emits it explicitly; runtime behaviour is unchanged.
- **`WatchFromContractStart` SkipIf wrapper** — new JSONC syntax affects the phase-3 Delay (`common.jsonc:961,984`), not the phase-1 1c trigger.
- **Recent PRs (#708 dialogue/PureRandom, #693 SetRelationship, "Use fewer patches"):** none touch `SetTeamByLanceSpawnerGuid`, the initiative result, or the turn-system code paths. Blackout does not use `SetRelationship`.
- **`SetStatusAtRandomResult` infinite-loop fix (`f7862b7d`)** — picks the 1a/1b/1c chunk; the fix switched to a `remainingGuids` copy, which only affects infinite-loop edge cases, not the happy path we're tracing.
- **Master↔develop bisection** — 359 commits, user explicitly ruled out as too large.

## Why it's intermittent

Best theory: the bug needs a specific alignment of the TurnDirector's phase/round state when `Trigger_1c_Turret_Activation` fires. RNG in:

- Which chunk `SetStatusAtRandom` picks (only 1c is affected — 1b and 1a go different paths).
- **When** in the round the player enters the region that triggers the state change.
- Which team is mid-activation at that exact moment.

On a normal run the turret is "done" for the round (NeutralToAll doesn't really activate — it just emits `ends activation due to Done` each phase), then next round both teams rebuild their activation sequences from current membership and everything works. On the failing alignment the turret ends up in a completion path for `EmployerTeam` without Employer having been through `TurnActorProcessActivation` in that phase — producing the warning.

Unproven. That's why we shipped diagnostics instead of a speculative fix.

## Diagnostic instrumentation shipped (commit on `develop`)

Three changes, all log-only, no behavioural impact:

1. `SetTeamByLanceSpawnerGuidResult.cs` — logs round/phase/activeTurnActor, both teams' state, and per-actor state on every swap.
2. `SetTemporaryUnitPhaseInitiativeByTagResult.cs` — logs per-actor initiative context.
3. `src/Patches/Debug/TeamCompleteActivationPatch.cs` — Harmony Prefix on `Team.CompleteActivation`. Guards on `ActivationSequence == null` so it only fires when the warning itself fires; dumps the full `Combat.Teams` snapshot plus actor + TurnDirector state.

Tags in logs: `[MC-711]`. Grep for that to find relevant entries.

## Next session (when repro returns)

1. Grep the mod.log for `[MC-711]`.
2. Find the `TeamCompleteActivationPatch` dump — this is the warning moment with full context.
3. Walk backwards to the most recent `[SetTeamByLanceSpawnerGuid][MC-711]` to see the turn state when the swap fired.
4. Walk backwards to `[SetTemporaryUnitPhaseInitiativeByTagResult][MC-711]` to see initiative at swap time.

Likely candidate fixes once we see the data:
- Defer the swap to `OnRoundBegin` when the new team has no active sequence and the unit hasn't activated this round.
- After the swap, call something equivalent to `StackManager.InsertInterruptPhase(newTeam.GUID, …)` if the new team isn't currently scheduled for this phase.
- Mark the transferred actor `DoneWithActor` for the current round if the new team's schedule has already passed — cleanest if it works.

## Key files

| File | Role |
|---|---|
| `contractTypeBuilds/Blackout/common.jsonc` | Contract type build; lines 67–94 (turret spawners), 821–851 (1c trigger) |
| `src/Core/EncounterResults/Team/SetTeamByLanceSpawnerGuidResult.cs` | Team-swap result — now instrumented |
| `src/Core/EncounterResults/Modify/SetTemporaryUnitPhaseInitiativeByTagResult.cs` | Initiative-change result — now instrumented |
| `src/Patches/Debug/TeamCompleteActivationPatch.cs` | New debug Prefix patch (diagnostic only) |
| `src/Core/EncounterRules/Blackout/BlackoutEncounterRules.cs` | Blackout-specific rules (spawn/boundary) — not involved in the bug |
| `overrides/contracts/blackout/Blackout_PreparedGrounds.json` | Contract override for the tested map — no logic changes |

## Reference

- Game source (decompiled): `F:\ProtonDrive\My files\Battletech Modding\Source\1.9.1\Assembly-CSharp\BattleTech\Team.cs:1657` (warning), `Team.cs:1620` (ActivationSequence set), `…\BattleTech.Framework\AssignUnitsToLanceResult.cs:34` (vanilla equivalent of our swap).
