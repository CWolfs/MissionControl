using BattleTech;
using BattleTech.Data;

using HBS.Util;

using BattleTech.Framework;

using System.Collections.Generic;

using MissionControl.Data;

namespace MissionControl.EncounterNodes.Dropship {
  public class CustomDropshipLandingSpotGameLogic : EncounterObjectGameLogic {
    public override TaggedObjectType Type => TaggedObjectType.DropshipLandingSpot;

    public CustomDropshipType UseDropshipType = CustomDropshipType.Any;
    public StartingDropshipAnimationState DropshipStates { get; set; } = StartingDropshipAnimationState.Landed;
    public bool AutoMarkDropshipLandingZone { get; set; } = true;
    public string TeamGUID { get; set; } = string.Empty;
    public List<DropshipRef> DropshipGameLogicList { get; set; } = new List<DropshipRef>();

    public override void BuildItemRegistry(CombatGameState combat) {
      base.BuildItemRegistry(combat);
      List<DropshipRef> dropshipGameLogicList = this.DropshipGameLogicList;

      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        GetDropship(dropshipGameLogicList[i]).BuildItemRegistry(combat);
      }
    }

    public List<string> GetDropshipGUIDs() {
      List<string> dropshipGUIDs = new List<string>();

      for (int i = 0; i < DropshipGameLogicList.Count; i++) {
        dropshipGUIDs.Add(DropshipGameLogicList[i].EncounterObjectGuid);
      }

      return dropshipGUIDs;
    }

    public List<DropshipGameLogic> GetDropships() {
      List<DropshipGameLogic> dropships = new List<DropshipGameLogic>();

      for (int i = 0; i < DropshipGameLogicList.Count; i++) {
        dropships.Add(GetDropship(DropshipGameLogicList[i]));
      }

      return dropships;
    }

    public DropshipGameLogic GetDropship(DropshipRef dropshipRef) {
      return dropshipRef.GetEncounterObject(base.Combat.ItemRegistry);
    }

    public override void BuildEncounterObjectDictionary(Dictionary<string, EncounterObjectGameLogic> encounterObjectDictionary) {
      base.BuildEncounterObjectDictionary(encounterObjectDictionary);
    }

    public void TurnOffDropshipLanding() {
      AutoMarkDropshipLandingZone = false;

      for (int i = 0; i < DropshipGameLogicList.Count; i++) {
        DropshipGameLogic dropship = GetDropship(DropshipGameLogicList[i]);
        dropship.autoMarkDropshipLandingZone = false;
        dropship.MarkDropshipLandingZone(markAsDropshipLandingZone: false);
      }
    }

    public override void ContractInitialize() {
      base.ContractInitialize();
      List<DropshipRef> dropshipGameLogicList = this.DropshipGameLogicList;

      foreach (DropshipRef dropshipRef in dropshipGameLogicList) {
        DropshipGameLogic dropshipGameLogic = GetDropship(dropshipRef);

        if (!base.Combat.IsLoadingFromSave) {
          if (TeamGUID != string.Empty) {
            dropshipGameLogic.ApplyHeraldry(TeamGUID);
          }

          if (UseDropshipType != CustomDropshipType.Any && dropshipGameLogic.dropshipType.ToString() != UseDropshipType.ToString()) {
            dropshipGameLogic.IgnoreDropship();
            continue;
          }

          dropshipGameLogic.startingAnimationState = (DropshipAnimationState)DropshipStates;
          dropshipGameLogic.encounterTags.AddRange(encounterTags);
        }

        dropshipGameLogic.ContractInitialize();
      }
    }

    public override void EncounterResume(LoadRequest loadRequest) {
      base.EncounterResume(loadRequest);
      List<DropshipRef> dropshipGameLogicList = this.DropshipGameLogicList;

      foreach (DropshipRef dropshipRef in dropshipGameLogicList) {
        DropshipGameLogic dropshipGameLogic = GetDropship(dropshipRef);

        if (TeamGUID != string.Empty) {
          dropshipGameLogic.ApplyHeraldry(TeamGUID);
        }
      }
    }

    public override void EncounterStart() {
      base.EncounterStart();
      List<DropshipRef> dropshipGameLogicList = this.DropshipGameLogicList;

      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        GetDropship(dropshipGameLogicList[i]).EncounterStart();
      }
    }

    public override void LoadComplete() {
      base.LoadComplete();
      List<DropshipRef> dropshipGameLogicList = this.DropshipGameLogicList;
      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        dropshipGameLogicList[i].LoadComplete();
      }
    }

    public bool IsDropshipAlive(DropshipGameLogic dropship) {
      return dropship.IsBuildingAlive();
    }

    public bool IsDropshipAlive(DropshipRef dropshipRef) {
      return GetDropship(dropshipRef).IsBuildingAlive();
    }

    public void LandDropship(DropshipGameLogic dropship) {
      if (IsDropshipAlive(dropship)) {
        dropship.LandDropship();
      } else {
        Main.Logger.LogWarning("[CustomDropshipLandingSpotGameLogic] Tried to land the dropship when it was dead. Ignoring command.");
      }
    }

    public void LandDropship(DropshipRef dropshipRef) {
      if (IsDropshipAlive(dropshipRef)) {
        GetDropship(dropshipRef).LandDropship();
      } else {
        Main.Logger.LogWarning("Tried to land the dropship when it was dead. Ignoring command.");
      }
    }

    public void TakeoffDropship(DropshipGameLogic dropship) {
      if (IsDropshipAlive(dropship)) {
        dropship.TakeoffDropship();
      } else {
        Main.Logger.LogWarning("Tried to takeoff the dropship when it was dead. Ignoring command.");
      }
    }

    public void TakeoffDropship(DropshipRef dropshipRef) {
      if (IsDropshipAlive(dropshipRef)) {
        GetDropship(dropshipRef).TakeoffDropship();
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

    public void FlybyDropship(DropshipRef dropshipRef) {
      if (IsDropshipAlive(dropshipRef)) {
        GetDropship(dropshipRef).StartDropoff();
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
