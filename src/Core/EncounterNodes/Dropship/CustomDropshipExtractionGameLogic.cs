using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Collections;
using HBS.Util;

using MissionControl.Data.Refs;

using System.Collections.Generic;

namespace MissionControl.EncounterNodes.Dropship {
  public class CustomDropshipExtractionGameLogic : EncounterObjectGameLogic {
    public override TaggedObjectType Type => TaggedObjectType.Logic;

    public CustomDropshipLandingSpotRef dropshipLandingSpotRef = new CustomDropshipLandingSpotRef();

    // OccupyRegionObjective - Used to call the dropshop to the landing spot on success & if it's offscreen & extractViaDropship is true
    public ObjectiveRef callDropshipObjectiveRef = new ObjectiveRef();
    // OccupyRegionObjective - Used for getting units in region to despawn them (e.g. load them into the dropship)
    public ObjectiveRef loadDropshipObjectiveRef = new ObjectiveRef();

    public DespawnFloatieMessage despawnMessage = DespawnFloatieMessage.Escaped;
    public TagSet requiredTagsOnLance = new TagSet();

    public bool spawnLancesWhenLanded = false;
    public bool takeOffImmediately = false;
    public bool extractViaDropship = true;

    public override void OnEnterActive() {
      base.OnEnterActive();

      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = dropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);
      ObjectiveGameLogic callDropshipObjective = callDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry);
      ObjectiveGameLogic loadDropshipObjective = loadDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry);

      switch (dropshipLandingSpot.DropshipStates) {
        case StartingDropshipAnimationState.OffScreen:
          callDropshipObjective.SetState(EncounterObjectStatus.Active);
          break;
        case StartingDropshipAnimationState.Landed:
          callDropshipObjective.IgnoreObjective();
          loadDropshipObjective.SetState(EncounterObjectStatus.Active);
          break;
      }
    }

    public override void AlwaysInit(CombatGameState combat) {
      base.AlwaysInit(combat);

      messageMemory.TrackMethod(MessageCenterMessageType.OnObjectiveSucceeded, OnObjectiveSucceeded);
      messageMemory.TrackMethod(MessageCenterMessageType.OnDropshipLanded, OnDropshipLanded);
    }

    public override void SubscribeToMessages(bool shouldAdd) {
      base.SubscribeToMessages(shouldAdd);

      messageMemory.Subscribe(MessageCenterMessageType.OnObjectiveSucceeded, OnObjectiveSucceeded, shouldAdd);
      messageMemory.Subscribe(MessageCenterMessageType.OnDropshipLanded, OnDropshipLanded, shouldAdd);
    }

    public override void ContractInitialize() {
      base.ContractInitialize();

      if (!extractViaDropship) {
        dropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry).TurnOffDropshipLanding();
      }
    }

    public void ApplyExtractionOverride(ExtractionOverride extractionOverride) {
      extractViaDropship = extractionOverride.extractViaDropship;
    }

    private void OnObjectiveSucceeded(MessageCenterMessage message) {
      ObjectiveSucceeded obj = message as ObjectiveSucceeded;
      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = dropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);
      List<DropshipGameLogic> dropships = dropshipLandingSpot.GetDropships();

      foreach (DropshipGameLogic dropship in dropships) {
        if (obj.ObjectiveGuid == callDropshipObjectiveRef.EncounterObjectGuid && dropship.currentAnimationState == DropshipAnimationState.OffScreen && extractViaDropship) {
          dropship.LandDropship();
        }

        if (obj.ObjectiveGuid == loadDropshipObjectiveRef.EncounterObjectGuid) {
          DespawnUnits();

          if (dropship.currentAnimationState == DropshipAnimationState.Landed && extractViaDropship) {
            dropship.TakeoffDropship();
          }
        }
      }
    }

    private void DespawnUnits() {
      OccupyRegionObjective occupyRegionObjective = loadDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry) as OccupyRegionObjective;

      if (occupyRegionObjective != null) {
        foreach (ICombatant targetUnit in occupyRegionObjective.GetTargetUnits()) {
          if (targetUnit.IsInRegion(occupyRegionObjective.occupyTargetRegion.EncounterObjectGuid) && targetUnit.UnitType != UnitType.Building) {
            EncounterLayerParent.EnqueueLoadAwareMessage(new DespawnActorMessage(encounterObjectGuid, targetUnit.GUID, DeathMethod.DespawnedEscaped));
          }
        }
      }
    }

    private void OnDropshipLanded(MessageCenterMessage message) {
      DropshipLandedMessage obj = message as DropshipLandedMessage;
      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = dropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);

      List<string> dropshipGUIDs = dropshipLandingSpot.GetDropshipGUIDs();

      foreach (string dropshipGUID in dropshipGUIDs) {
        if (obj.DropshipGuid == dropshipGUID) {
          if (spawnLancesWhenLanded) {
            TriggerSpawnLances();
          }

          if (takeOffImmediately) {
            TriggerDropshipTakeOff();
          } else {
            TriggerWaitToTakeOff();
          }
        }
      }
    }

    private void TriggerSpawnLances() {
      List<ITaggedItem> objectsOfTypeWithTagSet = base.Combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.LanceSpawner, requiredTagsOnLance);

      for (int i = 0; i < objectsOfTypeWithTagSet.Count; i++) {
        EncounterLayerParent.EnqueueLoadAwareMessage(new TriggerSpawn(objectsOfTypeWithTagSet[i].GUID));
      }
    }

    private void TriggerWaitToTakeOff() {
      (loadDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry) as OccupyRegionObjective).SetState(EncounterObjectStatus.Active);
    }

    public void TriggerDropshipTakeOff() {
      foreach (ICombatant targetUnit in (callDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry) as OccupyRegionObjective).GetTargetUnits()) {
        EncounterLayerParent.EnqueueLoadAwareMessage(new DespawnActorMessage(encounterObjectGuid, targetUnit.GUID, (DeathMethod)despawnMessage));
      }

      CustomDropshipLandingSpotGameLogic encounterObject = dropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);
      List<DropshipGameLogic> dropships = encounterObject.GetDropships();

      foreach (DropshipGameLogic dropship in dropships) {
        dropship.TakeoffDropship();
      }
    }

    public override int Size() {
      return base.Size();
    }

    public override bool ShouldSave() {
      return true;
    }

    public override void Save(SerializationStream stream) {
      base.Save(stream);
    }

    public override void Load(SerializationStream stream) {
      base.Load(stream);
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON(this);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON(new DropshipExtractionChunkGameLogic());
    }
  }
}
