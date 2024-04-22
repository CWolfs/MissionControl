using BattleTech;
using BattleTech.Data;

using HBS.Util;

using System.Collections.Generic;

using MissionControl.Data;
using HBS.Collections;

namespace MissionControl.EncounterNodes.Dropship {
  public class CustomDropshipLandingSpotGameLogic : EncounterObjectGameLogic {
    public override TaggedObjectType Type => TaggedObjectType.DropshipLandingSpot;

    public CustomDropshipType UseDropshipType { get; set; } = CustomDropshipType.Any;
    public StartingDropshipAnimationState DropshipStates { get; set; } = StartingDropshipAnimationState.Landed;
    public bool AutoMarkDropshipLandingZone { get; set; } = true;
    public string TeamGUID { get; set; } = string.Empty;

    public List<string> DropshipTags { get; set; } = new List<string>();

    public List<DropshipGameLogic> DropshipGameLogicList { get; set; } = new List<DropshipGameLogic>();

    public override void BuildItemRegistry(CombatGameState combat) {
      base.BuildItemRegistry(combat);
    }

    private void PopulateDropships() {
      List<ITaggedItem> potentialDropships = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.ObstructionGameLogic, new TagSet(DropshipTags));

      foreach (ITaggedItem potentialDropship in potentialDropships) {
        DropshipGameLogic dropshipGameLogic = potentialDropship as DropshipGameLogic;
        if (dropshipGameLogic != null) {
          DropshipGameLogicList.Add(dropshipGameLogic);
          Main.Logger.LogDebug("[CustomDropshipLandingSpotGameLogic.PopulateDropships] Found dropship: " + dropshipGameLogic.encounterObjectGuid);
        }
      }
    }

    public override void ContractInitialize() {
      base.ContractInitialize();

      PopulateDropships();

      foreach (DropshipGameLogic dropshipGameLogic in DropshipGameLogicList) {
        if (!base.Combat.IsLoadingFromSave) {
          if (TeamGUID != string.Empty) {
            dropshipGameLogic.ApplyHeraldry(TeamGUID);
          }

          if (UseDropshipType != CustomDropshipType.Any && dropshipGameLogic.dropshipType.ToString() != UseDropshipType.ToString()) {
            Main.Logger.LogDebug("[CustomDropshipLandingSpotGameLogic.ContractInitialize] Ignoring dropship: " + dropshipGameLogic.encounterObjectGuid);
            dropshipGameLogic.IgnoreDropship();
            continue;
          }

          dropshipGameLogic.startingAnimationState = (DropshipAnimationState)DropshipStates;
          dropshipGameLogic.encounterTags.AddRange(encounterTags);
        }

        dropshipGameLogic.ContractInitialize();
      }
    }

    public override void EncounterStart() {
      base.EncounterStart();
      for (int i = 0; i < DropshipGameLogicList.Count; i++) {
        DropshipGameLogicList[i].EncounterStart();
      }
    }

    public override void EncounterResume(LoadRequest loadRequest) {
      base.EncounterResume(loadRequest);

      foreach (DropshipGameLogic dropshipGameLogic in DropshipGameLogicList) {
        if (TeamGUID != string.Empty) {
          dropshipGameLogic.ApplyHeraldry(TeamGUID);
        }
      }
    }

    public override void LoadComplete() {
      base.LoadComplete();

      for (int i = 0; i < DropshipGameLogicList.Count; i++) {
        DropshipGameLogicList[i].LoadComplete();
      }
    }

    public List<string> GetDropshipGUIDs() {
      List<string> dropshipGUIDs = new List<string>();

      for (int i = 0; i < DropshipGameLogicList.Count; i++) {
        dropshipGUIDs.Add(DropshipGameLogicList[i].encounterObjectGuid);
      }

      return dropshipGUIDs;
    }

    private List<DropshipGameLogic> FindDropships() {
      return DropshipGameLogicList;
    }

    public override void BuildEncounterObjectDictionary(Dictionary<string, EncounterObjectGameLogic> encounterObjectDictionary) {
      base.BuildEncounterObjectDictionary(encounterObjectDictionary);
    }

    public void TurnOffDropshipLanding() {
      AutoMarkDropshipLandingZone = false;

      for (int i = 0; i < DropshipGameLogicList.Count; i++) {
        DropshipGameLogic dropship = DropshipGameLogicList[i];
        dropship.autoMarkDropshipLandingZone = false;
        dropship.MarkDropshipLandingZone(markAsDropshipLandingZone: false);
      }
    }


    public bool IsDropshipAlive(DropshipGameLogic dropship) {
      return dropship.IsBuildingAlive();
    }

    public void LandDropship(DropshipGameLogic dropship) {
      if (IsDropshipAlive(dropship)) {
        dropship.LandDropship();
      } else {
        Main.Logger.LogWarning("[CustomDropshipLandingSpotGameLogic] Tried to land the dropship when it was dead. Ignoring command.");
      }
    }

    public void TakeoffDropship(DropshipGameLogic dropship) {
      if (IsDropshipAlive(dropship)) {
        dropship.TakeoffDropship();
      } else {
        Main.Logger.LogWarning("Tried to takeoff the dropship when it was dead. Ignoring command.");
      }
    }

    public void FlybyDropship(DropshipGameLogic dropship) {
      if (IsDropshipAlive(dropship)) {
        dropship.StartDropoff();
      } else {
        Main.Logger.LogWarning("Tried to flyby the dropship when it was dead. Ignoring command.");
      }
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON(this);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON(new CustomDropshipLandingSpotGameLogic());
    }
  }
}
