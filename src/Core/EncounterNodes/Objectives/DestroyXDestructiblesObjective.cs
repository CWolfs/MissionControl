using BattleTech;
using BattleTech.Framework;

using HBS.Util;

using UnityEngine;

using Localize;

using System.Globalization;
using System.Collections.Generic;

using MissionControl.Data;
using MissionControl.Messages;

namespace MissionControl.EncounterNodes.Objectives {
  public class DestroyXDestructiblesObjective : ObjectiveGameLogic {

    [SerializeField]
    public string RegionGuid { get; set; }

    [SerializeField]
    public ObjectiveCountType CountType { get; set; }

    [SerializeField]
    public int valueOfDestructiblesToDestroy { get; set; } = 1;

    [SerializeField]
    private int NumberOfDestructiblesToDestroy { get; set; } = 1;


    public int destroyedDestructiblesSoFar = 0;

    public List<DestructibleObject> TrackedDestructibles = new List<DestructibleObject>();

    public override bool completeOnMissionSuccess => invertObjective;

    public override void AlwaysInit(CombatGameState combat) {
      base.AlwaysInit(combat);
      Main.LogDebug($"[DestroyXDestructiblesObjective.AlwaysInit] AlwaysInit");

      MessageCenter messageCenter = UnityGameInstance.BattleTechGame.MessageCenter;
      messageCenter.AddSubscriber((MessageCenterMessageType)MessageTypes.OnDestructibleCollapsed, new ReceiveMessageCenterMessage(this.OnDestructibleDestroyed));

      Validate();
    }

    public override void SubscribeToMessages(bool shouldAdd) {
      Main.LogDebug($"[DestroyXDestructiblesObjective.SubscribeToMessages] SubscribeToMessages");

      MessageCenter messageCenter = UnityGameInstance.BattleTechGame.MessageCenter;
      messageCenter.AddSubscriber((MessageCenterMessageType)MessageTypes.OnDestructibleCollapsed, new ReceiveMessageCenterMessage(this.OnDestructibleDestroyed));

      base.SubscribeToMessages(shouldAdd);
    }

    public override void ContractInitialize() {
      base.ContractInitialize();

      RegionRef regionRef = new RegionRef();
      regionRef.EncounterObjectGuid = RegionGuid;
      AttachRegionToObjective(regionRef);
    }

    public void OnDestroy() {
      Main.LogDebug($"[DestroyXDestructiblesObjective.OnDestroy] OnDestroy");
      MessageCenter messageCenter = UnityGameInstance.BattleTechGame.MessageCenter;
      messageCenter.RemoveSubscriber((MessageCenterMessageType)MessageTypes.OnDestructibleCollapsed, new ReceiveMessageCenterMessage(this.OnDestructibleDestroyed));
    }

    public override void ActivateObjective() {
      base.ActivateObjective();

      // Collect the targets from the region
      RegionGameLogic regionGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      List<DestructibleObject> destructibles = GameObjextExtensions.GetDestructiblesWithLODComponents(new List<string>() { "envPrfGrbl_", "envPrfDeco_" }); // Pick the bigger destructible props
      Main.Logger.Log($"[DestroyXDestructiblesObjective] Found {destructibles.Count} destructibles");

      destructibles.Shuffle();

      foreach (DestructibleObject destructible in destructibles) {
        bool isDestructibleInRegion = RegionUtil.PointInRegion(UnityGameInstance.BattleTechGame.Combat, destructible.transform.position, RegionGuid);
        if (isDestructibleInRegion) {
          TrackedDestructibles.Add(destructible);
        }
      }

      if (CountType == ObjectiveCountType.Number) {
        if (TrackedDestructibles.Count < NumberOfDestructiblesToDestroy) {
          Main.Logger.LogWarning("[DestroyXDestructiblesObjective] Couldn't find enough destructibles to track for this objective so setting the NumberOfDestructiblesToDestroy to the max available");
          NumberOfDestructiblesToDestroy = TrackedDestructibles.Count;
        }
        Main.Logger.Log($"[DestroyXDestructiblesObjective] Using number mode with amount of: {NumberOfDestructiblesToDestroy} destructibles");
      } else if (CountType == ObjectiveCountType.Percentage) {
        if (TrackedDestructibles.Count == 1) {
          NumberOfDestructiblesToDestroy = 1;
        } else {
          float multiFactor = (float)valueOfDestructiblesToDestroy / 100f;
          NumberOfDestructiblesToDestroy = (int)((float)TrackedDestructibles.Count * multiFactor);
        }

        if (NumberOfDestructiblesToDestroy <= 0) {
          NumberOfDestructiblesToDestroy = 1;
        }

        Main.Logger.LogWarning($"[DestroyXDestructiblesObjective] Using percentage mode with value '{valueOfDestructiblesToDestroy}%' the number of destructibles to be destroyed will be {NumberOfDestructiblesToDestroy} / {TrackedDestructibles.Count}");
      }
    }

    public override Vector3 GetBeaconPosition() {
      Vector3 position = base.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid).Position;
      position.y = base.Combat.MapMetaData.GetLerpedHeightAt(position) + ObjectiveGameLogic.BEACON_OFFSET;
      return position;
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
      progressText.Replace("[destroyedDestructiblesSoFar]", destroyedDestructiblesSoFar.ToString());
      progressText.Replace("[numberOfDestructiblesToDestroy]", NumberOfDestructiblesToDestroy.ToString());
      progressText.Replace("[percentageComplete]", ((float)destroyedDestructiblesSoFar / (float)NumberOfDestructiblesToDestroy).ToString("P0", CultureInfo.InvariantCulture));
      progressText.Replace("[destructiblesStillNeededToMeetThreshold]", (NumberOfDestructiblesToDestroy - destroyedDestructiblesSoFar).ToString());
      return progressText;
    }

    public void OnDestructibleDestroyed(MessageCenterMessage message) {
      if (message is DestructibleCollapsedMessage) {
        DestructibleCollapsedMessage destructibleCollapsedMessage = (DestructibleCollapsedMessage)message;
        QueueCheckObjective();
      }
    }

    public override void UpdateCounts() {
      base.UpdateCounts();
      // Main.Logger.Log($"[DestroyXDestructiblesObjective.UpdateCounts] Checking counts");
      int destructionCount = 0;

      for (int i = 0; i < TrackedDestructibles.Count; i++) {
        if (TrackedDestructibles[i].isCollapsed) {
          // Main.Logger.Log($"[DestroyXDestructiblesObjective.UpdateCounts] Tracked Destructible '{TrackedDestructibles[i].gameObject}' collapsed");
          destructionCount++;
        }
      }

      destroyedDestructiblesSoFar = destructionCount;
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