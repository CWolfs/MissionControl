using BattleTech;
using BattleTech.Framework;

using HBS.Util;

using UnityEngine;

using Localize;

using System.Globalization;
using System.Collections.Generic;

namespace MissionControl.LogicComponents.Objectives {
  public class DestroyXDestructiblesObjective : ObjectiveGameLogic {

    [SerializeField]
    public string RegionGuid { get; set; }

    [SerializeField]
    public int NumberOfDestructiblesToDestroy { get; set; } = 1;

    public int destroyedDestructiblesSoFar = 0;

    public List<DestructibleObject> TrackedDestructibles = new List<DestructibleObject>();

    public override bool completeOnMissionSuccess => invertObjective;

    public override void AlwaysInit(CombatGameState combat) {
      base.AlwaysInit(combat);
      // messageMemory.TrackMethod(MessageCenterMessageType.OnActorDestroyed, OnActorDestroyed);

      Validate();
    }

    public override void SubscribeToMessages(bool shouldAdd) {
      // messageMemory.Subscribe(MessageCenterMessageType.OnActorDestroyed, OnActorDestroyed, shouldAdd);
      base.SubscribeToMessages(shouldAdd);
    }

    public override void ActivateObjective() {
      base.ActivateObjective();

      // Collect the targets from the region
      RegionGameLogic regionGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      List<DestructibleObject> destructibles = GameObjextExtensions.GetDestructiblesWithLODComponents();
      Main.Logger.Log($"[DestroyXDestructiblesObjective] Found {destructibles.Count} destructibles");

      destructibles.Shuffle();

      if (destructibles.Count < NumberOfDestructiblesToDestroy) {
        Main.Logger.LogWarning("[DestroyXDestructiblesObjective] Couldn't find enough destructibles to track for this objective so setting the NumberOfDestructiblesToDestroy to the max available");
        NumberOfDestructiblesToDestroy = destructibles.Count;
      }

      for (int i = 0; i < NumberOfDestructiblesToDestroy; i++) {
        TrackedDestructibles.Add(destructibles[i]);
      }

      Main.Logger.Log($"[DestroyXDestructiblesObjective] Now tracking {TrackedDestructibles.Count} destructibles");
      foreach (DestructibleObject destructible in TrackedDestructibles) {
        Main.Logger.Log("[DestroyXDestructiblesObjective] Tracking... " + destructible.gameObject.name);
      }
    }

    private void Validate() {
      if (RegionGuid == null || RegionGuid == "") {
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] RegionGuid is empty or null");
        return;
      }

      if (NumberOfDestructiblesToDestroy <= 0) {
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] NumberOfDestructiblesToDestroy must be 1 or higher'");
        return;
      }
    }

    public override Text GetProgressText() {
      Text progressText = base.GetProgressText();
      progressText.Replace("[killedUnitsSoFar]", destroyedDestructiblesSoFar.ToString());
      progressText.Replace("[numberOfUnitsToKill]", NumberOfDestructiblesToDestroy.ToString());
      progressText.Replace("[percentageComplete]", ((float)destroyedDestructiblesSoFar / (float)NumberOfDestructiblesToDestroy).ToString("P0", CultureInfo.InvariantCulture));
      progressText.Replace("[unitsStillNeededToMeetThreshold]", (NumberOfDestructiblesToDestroy - destroyedDestructiblesSoFar).ToString());
      return progressText;
    }

    // public override List<ICombatant> GetTargetUnits() {
    //   return ObjectiveGameLogic.GetTaggedCombatants(base.Combat, requiredTagsOnUnit);
    // }

    public override void UpdateCounts() {
      // base.UpdateCounts();
      // List<ICombatant> targetUnits = GetTargetUnits();
      // int destructionCount = 0;
      // for (int i = 0; i < targetUnits.Count; i++) {
      //   if (targetUnits[i].IsDead) {
      //     destructionCount++;
      //   }
      // }
      // killedDestructiblesSoFar = destructionCount;
    }

    public override bool CheckForSuccess() {
      base.CheckForSuccess();
      if (destroyedDestructiblesSoFar >= NumberOfDestructiblesToDestroy) {
        LogObjective($"Objective Successful: destroyedDestructiblesSoFar[{destroyedDestructiblesSoFar}] >= NumberOfDestructiblesToDestroy[{NumberOfDestructiblesToDestroy}]");
        return true;
      }
      return false;
    }

    public override bool CheckForFailure() {
      base.CheckForFailure();
      return false;
    }

    // TODO: Check if there's a OnDestructibleDestroyed message, otherwise make one and use it
    // private void OnActorDestroyed(MessageCenterMessage message) {
    //   QueueCheckObjective();
    // }

    // TODO: Maybe in the future support the Save/Load methods but it's a lot of work considering it's not supported by any/many mods

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<DestroyXDestructiblesObjective>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<DestroyXDestructiblesObjective>(new DestroyXDestructiblesObjective());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<DestroyXDestructiblesObjective>(this);
    }
  }
}