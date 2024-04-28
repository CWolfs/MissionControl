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
    public DespawnFloatieMessage despawnMessage = DespawnFloatieMessage.Escaped;

    public CustomDropshipLandingSpotRef DropshipLandingSpotRef { get; set; } = new CustomDropshipLandingSpotRef();

    // OccupyRegionObjective - Used to call the dropshop to the landing spot on success & if it's offscreen & extractViaDropship is true
    public ObjectiveRef CallDropshipObjectiveRef { get; set; } = new ObjectiveRef();
    // OccupyRegionObjective - Used for getting units in region to despawn them (e.g. load them into the dropship)
    public ObjectiveRef LoadDropshipObjectiveRef { get; set; } = new ObjectiveRef();

    public TagSet RequiredTagsOnLance { get; set; } = new TagSet();

    public bool SpawnLancesWhenLanded { get; set; } = false;
    public bool TakeOffImmediately { get; set; } = false;
    public bool ExtractViaDropship { get; set; } = true;
    public bool ShowEscapeMessage { get; set; } = true;

    public override void OnEnterActive() {
      base.OnEnterActive();

      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = DropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);
      ObjectiveGameLogic callDropshipObjective = CallDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry);
      ObjectiveGameLogic loadDropshipObjective = LoadDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry);

      switch (dropshipLandingSpot.DropshipStates) {
        case StartingDropshipAnimationState.OffScreen:
          callDropshipObjective.SetState(EncounterObjectStatus.Active);
          break;
        case StartingDropshipAnimationState.Landed:
          callDropshipObjective.IgnoreObjective();
          loadDropshipObjective.SetState(EncounterObjectStatus.Active);
          break;
      }

      if (!ShowEscapeMessage) {
        despawnMessage = DespawnFloatieMessage.NoMessage;
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

      if (!ExtractViaDropship) {
        DropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry).TurnOffDropshipLanding();
      }
    }

    public void ApplyExtractionOverride(ExtractionOverride extractionOverride) {
      ExtractViaDropship = extractionOverride.extractViaDropship;
    }

    private void OnObjectiveSucceeded(MessageCenterMessage message) {
      ObjectiveSucceeded obj = message as ObjectiveSucceeded;
      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = DropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);
      List<DropshipGameLogic> dropships = dropshipLandingSpot.DropshipGameLogicList;

      foreach (DropshipGameLogic dropship in dropships) {
        if (obj.ObjectiveGuid == CallDropshipObjectiveRef.EncounterObjectGuid && dropship.currentAnimationState == DropshipAnimationState.OffScreen && ExtractViaDropship) {
          dropship.LandDropship();
        }

        if (obj.ObjectiveGuid == LoadDropshipObjectiveRef.EncounterObjectGuid) {
          DespawnUnits();

          if (dropship.currentAnimationState == DropshipAnimationState.Landed && ExtractViaDropship) {
            dropship.TakeoffDropship();
          }
        }
      }
    }

    private void DespawnUnits() {
      OccupyRegionObjective occupyRegionObjective = LoadDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry) as OccupyRegionObjective;

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
      CustomDropshipLandingSpotGameLogic dropshipLandingSpot = DropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);

      List<string> dropshipGUIDs = dropshipLandingSpot.GetDropshipGUIDs();

      foreach (string dropshipGUID in dropshipGUIDs) {
        if (obj.DropshipGuid == dropshipGUID) {
          if (SpawnLancesWhenLanded) {
            TriggerSpawnLances();
          }

          if (TakeOffImmediately) {
            TriggerDropshipTakeOff();
          } else {
            TriggerWaitToTakeOff();
          }
        }
      }
    }

    public void TriggerSpawnLances() {
      List<ITaggedItem> objectsOfTypeWithTagSet = base.Combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.LanceSpawner, RequiredTagsOnLance);

      for (int i = 0; i < objectsOfTypeWithTagSet.Count; i++) {
        EncounterLayerParent.EnqueueLoadAwareMessage(new TriggerSpawn(objectsOfTypeWithTagSet[i].GUID));
      }
    }

    public void TriggerWaitToTakeOff() {
      (LoadDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry) as OccupyRegionObjective).SetState(EncounterObjectStatus.Active);
    }

    public void TriggerDropshipTakeOff() {
      foreach (ICombatant targetUnit in (CallDropshipObjectiveRef.GetEncounterObject(base.Combat.ItemRegistry) as OccupyRegionObjective).GetTargetUnits()) {
        EncounterLayerParent.EnqueueLoadAwareMessage(new DespawnActorMessage(encounterObjectGuid, targetUnit.GUID, (DeathMethod)despawnMessage));
      }

      CustomDropshipLandingSpotGameLogic encounterObject = DropshipLandingSpotRef.GetEncounterObject(base.Combat.ItemRegistry);
      List<DropshipGameLogic> dropships = encounterObject.DropshipGameLogicList;

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
