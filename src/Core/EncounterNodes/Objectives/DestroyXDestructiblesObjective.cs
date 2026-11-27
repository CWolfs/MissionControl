using BattleTech;
using BattleTech.Data;
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

      messageMemory.TrackMethod((MessageCenterMessageType)MessageTypes.OnDestructibleCollapsed, OnDestructibleDestroyed);

      Validate();
    }

    public override void SubscribeToMessages(bool shouldAdd) {
      Main.LogDebug($"[DestroyXDestructiblesObjective.SubscribeToMessages] SubscribeToMessages: {shouldAdd}");

      messageMemory.Subscribe((MessageCenterMessageType)MessageTypes.OnDestructibleCollapsed, OnDestructibleDestroyed, shouldAdd);

      base.SubscribeToMessages(shouldAdd);
    }

    public override void ContractInitialize() {
      base.ContractInitialize();

      RegionRef regionRef = new RegionRef();
      regionRef.EncounterObjectGuid = RegionGuid;
      AttachRegionToObjective(regionRef);
    }

    public override void EncounterResume(LoadRequest loadRequest) {
      base.EncounterResume(loadRequest);

      if (GetState() == EncounterObjectStatus.Active && TrackedDestructibles.Count == 0) {
        if (CollectTrackedDestructibles()) {
          CalculateNumberOfDestructiblesToDestroy();
        }
      }
    }

    public void OnDestroy() {
      Main.LogDebug($"[DestroyXDestructiblesObjective.OnDestroy] OnDestroy");
      UnityGameInstance.BattleTechGame?.MessageCenter?.RemoveSubscriber((MessageCenterMessageType)MessageTypes.OnDestructibleCollapsed, OnDestructibleDestroyed);
    }

    public override void ActivateObjective() {
      base.ActivateObjective();

      if (!CollectTrackedDestructibles()) {
        return;
      }

      CalculateNumberOfDestructiblesToDestroy();
    }

    private bool CollectTrackedDestructibles() {
      TrackedDestructibles.Clear();

      CombatGameState combat = UnityGameInstance.BattleTechGame?.Combat;
      if (combat == null) {
        Main.Logger.LogError("[DestroyXDestructiblesObjective] Combat state is null.");
        return false;
      }

      RegionGameLogic regionGameLogic = combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] Region Not Found for Guid '{RegionGuid}'");
        return false;
      }

      List<DestructibleObject> destructibles = GameObjextExtensions.GetDestructiblesWithLODComponents(new List<string>() { "envPrfGrbl_", "envPrfDeco_" }); // Pick the bigger destructible props
      Main.Logger.Log($"[DestroyXDestructiblesObjective] Found {destructibles.Count} destructibles");

      destructibles.Shuffle();

      EncounterLayerData layerData = GetComponentInParent<EncounterLayerData>() ?? combat.EncounterLayerData ?? MissionControl.Instance.EncounterLayerData;

      foreach (DestructibleObject destructible in destructibles) {
        if (destructible == null) continue;
        bool isDestructibleInRegion = layerData.IsPointInRegion(destructible.transform.position, RegionGuid);
        if (isDestructibleInRegion) {
          TrackedDestructibles.Add(destructible);
        }
      }

      return true;
    }

    private void CalculateNumberOfDestructiblesToDestroy() {
      if (TrackedDestructibles.Count == 0) {
        NumberOfDestructiblesToDestroy = 0;
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] Found 0 destructibles in region '{RegionGuid}'");
        return;
      }

      if (CountType == ObjectiveCountType.Number) {
        NumberOfDestructiblesToDestroy = valueOfDestructiblesToDestroy;
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

        Main.Logger.Log($"[DestroyXDestructiblesObjective] Using percentage mode with value '{valueOfDestructiblesToDestroy}%' the number of destructibles to be destroyed will be {NumberOfDestructiblesToDestroy} / {TrackedDestructibles.Count}");
      }
    }

    public override Vector3 GetBeaconPosition() {
      RegionGameLogic region = base.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);
      if (region == null) {
        return base.GetBeaconPosition();
      }
      Vector3 position = region.Position;
      position.y = base.Combat.MapMetaData.GetLerpedHeightAt(position) + ObjectiveGameLogic.BEACON_OFFSET;
      return position;
    }

    private void Validate() {
      if (RegionGuid == null || RegionGuid == "") {
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] RegionGuid is empty or null");
        return;
      }

      if (valueOfDestructiblesToDestroy <= 0) {
        Main.Logger.LogError($"[DestroyXDestructiblesObjective] valueOfDestructiblesToDestroy must be 1 or higher");
        return;
      }
    }

    public override Text GetProgressText() {
      Text progressText = base.GetProgressText();
      float percentageComplete = NumberOfDestructiblesToDestroy > 0 ? (float)destroyedDestructiblesSoFar / (float)NumberOfDestructiblesToDestroy : 0f;
      int remaining = Mathf.Max(0, NumberOfDestructiblesToDestroy - destroyedDestructiblesSoFar);
      progressText.Replace("[destroyedDestructiblesSoFar]", destroyedDestructiblesSoFar.ToString());
      progressText.Replace("[numberOfDestructiblesToDestroy]", NumberOfDestructiblesToDestroy.ToString());
      progressText.Replace("[percentageComplete]", percentageComplete.ToString("P0", CultureInfo.InvariantCulture));
      progressText.Replace("[destructiblesStillNeededToMeetThreshold]", remaining.ToString());
      return progressText;
    }

    public void OnDestructibleDestroyed(MessageCenterMessage message) {
      if (message is DestructibleCollapsedMessage) {
        QueueCheckObjective();
      }
    }

    public override void UpdateCounts() {
      base.UpdateCounts();
      // Main.Logger.Log($"[DestroyXDestructiblesObjective.UpdateCounts] Checking counts");
      int destructionCount = 0;

      for (int i = 0; i < TrackedDestructibles.Count; i++) {
        DestructibleObject destructible = TrackedDestructibles[i];
        if (destructible == null || destructible.isCollapsed || !destructible.gameObject.activeInHierarchy) {
          // Main.Logger.Log($"[DestroyXDestructiblesObjective.UpdateCounts] Tracked Destructible '{TrackedDestructibles[i].gameObject}' collapsed");
          destructionCount++;
        }
      }

      destroyedDestructiblesSoFar = destructionCount;
    }

    public override bool CheckForSuccess() {
      base.CheckForSuccess();
      if (TrackedDestructibles.Count == 0 || NumberOfDestructiblesToDestroy <= 0) {
        return false;
      }
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