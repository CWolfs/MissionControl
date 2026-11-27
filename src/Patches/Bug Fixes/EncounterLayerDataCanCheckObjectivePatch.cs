using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using BattleTech;
using BattleTech.Designed;

using Harmony;

using MissionControl.LogicComponents.Spawners;
using MissionControl.Utils;

namespace MissionControl.Patches {
  // If a player drops with fewer eligible units than an OccupyRegionObjective requires (e.g. dropping
  // 1 mech into Blackout where Phase 1 requires 2 units), OccupyRegionObjective.CheckForFailure
  // immediately fails the objective and autofails the contract. Run once per fresh contract the first
  // time EncounterLayerData.CanCheckObjective() returns true (after OnEncounterBegin and opening
  // spawns settle, before the first objective check) and clamp numberOfUnitsToOccupy to the actual
  // eligible unit count (minimum 1). Because this runs once at deployment and skips loaded saves
  // (where OccupyRegionObjective.Save/Load preserves the value), later combat losses or team swaps
  // are never mistaken for a short drop.
  [HarmonyPatch(typeof(EncounterLayerData), "CanCheckObjective")]
  public static class EncounterLayerDataCanCheckObjectivePatch {
    private static readonly ConditionalWeakTable<EncounterLayerData, object> EvaluatedLayers =
      new ConditionalWeakTable<EncounterLayerData, object>();

    public static void Postfix(EncounterLayerData __instance, bool __result) {
      if (!__result || __instance == null) return;
      if (EvaluatedLayers.TryGetValue(__instance, out _)) return;

      CombatGameState combat = __instance.Combat;
      if (combat == null || combat.IsLoadingFromSave) return;

      EvaluatedLayers.Add(__instance, new object());

      if (combat.WasFromSave || combat.ItemRegistry == null) return;
      if (!IsObjectiveClampingAllowed()) return;

      OccupyRegionObjective[] objectives = __instance.GetComponentsInChildren<OccupyRegionObjective>();
      if (objectives == null || objectives.Length == 0) return;

      foreach (OccupyRegionObjective objective in objectives) {
        ClampObjectiveIfNeeded(combat, objective);
      }
    }

    private static void ClampObjectiveIfNeeded(CombatGameState combat, OccupyRegionObjective objective) {
      if (objective == null || objective.numberOfUnitsToOccupy <= 1 || objective.invertObjective) return;
      if (objective.IsComplete || objective.IsState(EncounterObjectStatus.Finished)) return;

      if (objective.requiredLance != null && objective.requiredLance.HasGuid) {
        LanceSpawnerGameLogic spawner = combat.ItemRegistry.GetItemByGUID<LanceSpawnerGameLogic>(objective.requiredLance.EncounterObjectGuid);
        if (spawner == null || spawner.teamDefinitionGuid != TeamUtils.PLAYER_TEAM_ID) return;

        // Only clamp inactive later-phase lance objectives when tied to a player drop lance spawner
        // (not a generic spawner that scripted logic could trigger multiple times). For inactive
        // objectives, cap by the deployed lance size so mid-mission tag additions are not undercut.
        bool isPlayerDropLance = (spawner is PlayerLanceSpawnerGameLogic) || (spawner is CustomPlayerLanceSpawnerGameLogic);
        if (!isPlayerDropLance && !objective.IsInProgress) return;

        if (objective.IsInProgress) {
          ClampToTargetUnits(objective);
        } else {
          Lance lance = spawner.GetLance();
          int deployedLanceUnits = lance?.unitGuids?.Count ?? 0;
          if (deployedLanceUnits > 0 && deployedLanceUnits < objective.numberOfUnitsToOccupy) {
            ApplyClamp(objective, deployedLanceUnits);
          }
        }
        return;
      }

      // For tag-only objectives, require an active-at-start objective with an explicit player tag
      // and verified player-team combatants (no hostiles) so unrelated or later-reinforced objectives
      // are left unchanged.
      if (!objective.IsInProgress) return;
      if (!HasExplicitPlayerTag(objective)) return;

      List<ICombatant> targetUnits = objective.GetTargetUnits();
      if (targetUnits == null || targetUnits.Count == 0 || targetUnits.Count >= objective.numberOfUnitsToOccupy) return;
      if (!HasPlayerUnitAndNoHostiles(combat, targetUnits)) return;

      ApplyClamp(objective, targetUnits.Count);
    }

    private static void ClampToTargetUnits(OccupyRegionObjective objective) {
      List<ICombatant> targetUnits = objective.GetTargetUnits();
      if (targetUnits == null || targetUnits.Count == 0 || targetUnits.Count >= objective.numberOfUnitsToOccupy) return;

      ApplyClamp(objective, targetUnits.Count);
    }

    private static void ApplyClamp(OccupyRegionObjective objective, int eligibleUnitCount) {
      int clampedCount = Math.Max(1, eligibleUnitCount);
      Main.Logger.Log($"[EncounterLayerDataCanCheckObjectivePatch] Clamping '{objective.name}' numberOfUnitsToOccupy from {objective.numberOfUnitsToOccupy} to {clampedCount} to match deployed eligible units.");
      objective.numberOfUnitsToOccupy = clampedCount;
    }

    private static bool IsObjectiveClampingAllowed() {
      MissionControl mc = MissionControl.Instance;
      if (mc == null || mc.CurrentContract == null || mc.IsLoadingFromSave) return false;

      // Mirror AllowMissionControl() without letting Bigger Drops-only flags modify story/flashpoint objectives.
      bool isStory = mc.IsAnyStoryContract();
      bool isFlashpoint = mc.IsAnyFlashpointContract();
      if (!isStory && !isFlashpoint) return true;

      return (isStory && Main.Settings.EnableStoryOverrides)
        || (isFlashpoint && (Main.Settings.EnableFlashpointOverrides || Main.Settings.ActiveContractSettings.Enabled));
    }

    private static bool HasExplicitPlayerTag(OccupyRegionObjective objective) {
      return objective.requiredTagsOnUnit != null
        && (objective.requiredTagsOnUnit.Contains("Player 1") || objective.requiredTagsOnUnit.Contains("player_unit"));
    }

    private static bool HasPlayerUnitAndNoHostiles(CombatGameState combat, List<ICombatant> targetUnits) {
      bool hasPlayerUnit = false;
      Team localPlayerTeam = combat.LocalPlayerTeam;
      for (int i = 0; i < targetUnits.Count; i++) {
        ICombatant combatant = targetUnits[i];
        if (combatant == null) continue;

        AbstractActor actor = combatant as AbstractActor;
        if (actor != null && actor.TeamId == TeamUtils.PLAYER_TEAM_ID) {
          hasPlayerUnit = true;
        } else if (localPlayerTeam != null && combatant.team != null && combatant.team.IsEnemy(localPlayerTeam)) {
          return false;
        }
      }
      return hasPlayerUnit;
    }
  }
}
