using UnityEngine;

using BattleTech;

using HBS.Util;

using BattleTech.Framework;

using System.Collections.Generic;
using BattleTech.Data;

namespace MissionControl.EncounterNodes.Dropship {
  public class CustomDropshipLandingSpotGameLogic : EncounterObjectGameLogic {
    public override TaggedObjectType Type => TaggedObjectType.DropshipLandingSpot;

    public DropshipType useDropshipType = DropshipType.Leopard;
    public StartingDropshipAnimationState dropshipStates;
    public bool autoMarkDropshipLandingZone = true;
    public string teamDefinitionGuid = string.Empty;
    public List<DropshipRef> dropshipGameLogicList { get; set; } = new List<DropshipRef>();

    public override void BuildItemRegistry(CombatGameState combat) {
      base.BuildItemRegistry(combat);
      List<DropshipRef> dropshipGameLogicList = this.dropshipGameLogicList;
      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        GetDropship(dropshipGameLogicList[i]).BuildItemRegistry(combat);
      }
    }

    public List<string> GetDropshipGUIDs() {
      List<string> dropshipGUIDs = new List<string>();

      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        dropshipGUIDs.Add(dropshipGameLogicList[i].EncounterObjectGuid);
      }

      return dropshipGUIDs;
    }

    public List<DropshipGameLogic> GetDropships() {
      List<DropshipGameLogic> dropships = new List<DropshipGameLogic>();

      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        dropships.Add(GetDropship(dropshipGameLogicList[i]));
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
      autoMarkDropshipLandingZone = false;

      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        DropshipGameLogic dropship = GetDropship(dropshipGameLogicList[i]);
        dropship.autoMarkDropshipLandingZone = false;
        dropship.MarkDropshipLandingZone(markAsDropshipLandingZone: false);
      }
    }

    public override void ContractInitialize() {
      base.ContractInitialize();
      List<DropshipRef> dropshipGameLogicList = this.dropshipGameLogicList;

      foreach (DropshipRef dropshipRef in dropshipGameLogicList) {
        DropshipGameLogic dropshipGameLogic = GetDropship(dropshipRef);

        if (!base.Combat.IsLoadingFromSave) {
          dropshipGameLogic.ApplyHeraldry(teamDefinitionGuid);

          if (dropshipGameLogic.dropshipType != useDropshipType) {
            dropshipGameLogic.IgnoreDropship();
            continue;
          }

          dropshipGameLogic.startingAnimationState = (DropshipAnimationState)dropshipStates;
          dropshipGameLogic.encounterTags.AddRange(encounterTags);
        }

        dropshipGameLogic.ContractInitialize();
      }
    }

    public override void EncounterResume(LoadRequest loadRequest) {
      base.EncounterResume(loadRequest);
      List<DropshipRef> dropshipGameLogicList = this.dropshipGameLogicList;

      foreach (DropshipRef dropshipRef in dropshipGameLogicList) {
        DropshipGameLogic dropshipGameLogic = GetDropship(dropshipRef);

        dropshipGameLogic.ApplyHeraldry(teamDefinitionGuid);
      }
    }

    public override void EncounterStart() {
      base.EncounterStart();
      List<DropshipRef> dropshipGameLogicList = this.dropshipGameLogicList;

      for (int i = 0; i < dropshipGameLogicList.Count; i++) {
        GetDropship(dropshipGameLogicList[i]).EncounterStart();
      }
    }

    public override void LoadComplete() {
      base.LoadComplete();
      List<DropshipRef> dropshipGameLogicList = this.dropshipGameLogicList;
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
        LogWarning("Tried to land the dropship when it was dead. Ignoring command.");
      }
    }

    public void LandDropship(DropshipRef dropshipRef) {
      if (IsDropshipAlive(dropshipRef)) {
        GetDropship(dropshipRef).LandDropship();
      } else {
        LogWarning("Tried to land the dropship when it was dead. Ignoring command.");
      }
    }

    public void TakeoffDropship(DropshipGameLogic dropship) {
      if (IsDropshipAlive(dropship)) {
        dropship.TakeoffDropship();
      } else {
        LogWarning("Tried to takeoff the dropship when it was dead. Ignoring command.");
      }
    }

    public void TakeoffDropship(DropshipRef dropshipRef) {
      if (IsDropshipAlive(dropshipRef)) {
        GetDropship(dropshipRef).TakeoffDropship();
      } else {
        LogWarning("Tried to takeoff the dropship when it was dead. Ignoring command.");
      }
    }

    public void FlybyDropship(DropshipGameLogic dropship) {
      if (IsDropshipAlive(dropship)) {
        dropship.StartDropoff();
      } else {
        LogWarning("Tried to flyby the dropship when it was dead. Ignoring command.");
      }
    }

    public void FlybyDropship(DropshipRef dropshipRef) {
      if (IsDropshipAlive(dropshipRef)) {
        GetDropship(dropshipRef).StartDropoff();
      } else {
        LogWarning("Tried to flyby the dropship when it was dead. Ignoring command.");
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
