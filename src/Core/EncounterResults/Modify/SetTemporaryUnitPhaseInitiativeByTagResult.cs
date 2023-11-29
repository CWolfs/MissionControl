using BattleTech;

using System;
using System.Collections.Generic;

using HBS.Collections;

using BattleTech.Framework;

namespace MissionControl.Result {
  public class SetTemporaryUnitPhaseInitiativeByTagResult : EncounterResult {
    public int Initiative { get; set; }
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetTemporaryUnitPhaseInitiativeByTagResult] Setting Initiative '{Initiative}' on units with tags '{String.Concat(Tags)}'");
      List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(UnityGameInstance.BattleTechGame.Combat, new TagSet(Tags));

      Main.LogDebug($"[SetTemporaryUnitPhaseInitiativeByTagResult] Found '{combatants.Count}' units");
      foreach (ICombatant combatant in combatants) {
        AbstractActor actor = combatant as AbstractActor;
        if (actor != null) {
          int oldInitiative = actor.Initiative;
          int initiativeDiff = Initiative - oldInitiative;
          actor.Initiative = Initiative;
          UnityGameInstance.BattleTechGame.Combat.MessageCenter.PublishMessage(new ActorPhaseInfoChanged(actor.GUID));
          actor.StatCollection.Set<int>(AbstractActorConstants.STAT_PHASEMOD, initiativeDiff);
        }
      }
    }
  }
}
