using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using HBS;
using FogOfWar;

namespace MissionControl.Result {
  public class ResetVisibilityResult : EncounterResult {
    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[s] Resetting visibility for all units.");
      ResetVisibility();
    }

    private void ResetVisibility() {
      // LazySingletonBehavior<FogOfWarView>.Instance.FowSystem.UpdateAllViewers();
      // List<ICombatant> combatants = UnityGameInstance.BattleTechGame.Combat.GetAllCombatants();
      // PilotableActorRepresentation[] pilotableActors = MonoBehaviour.FindObjectsOfType<PilotableActorRepresentation>();
      // foreach (PilotableActorRepresentation pilotableActor in pilotableActors) {
      //   try {
      //     pilotableActor.ClearForcedPlayerVisibilityLevel(combatants);
      //   } catch (ArgumentNullException e) {
      //     Main.Logger.LogWarning($"[ResetVisibilityResult] Unable to clear forced player visibility for {pilotableActor.name} - {e.Message}");
      //   }
      // }
    }
  }
}
