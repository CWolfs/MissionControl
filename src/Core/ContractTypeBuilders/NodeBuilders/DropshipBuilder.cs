using UnityEngine;

using Newtonsoft.Json.Linq;

using System;

using MissionControl.EncounterFactories;
using MissionControl.EncounterNodes.CombatStates;
using MissionControl.Data;
using BattleTech;
using System.Collections.Generic;
using BattleTech.Framework;
using MissionControl.EncounterNodes.Dropship;
using MissionControl.Data.Refs;

namespace MissionControl.ContractTypeBuilders {
  public class DropshipNodeBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject build;

    private GameObject parent;
    private string name;
    private string subType;
    private string guid;

    public DropshipNodeBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject build) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.build = build;

      this.parent = parent;
      this.name = build["Name"].ToString();
      this.subType = build["SubType"].ToString();
      this.guid = build["Guid"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "DropshipLandingSpot": BuildDropshipLandingSpot(); break;
        case "DropshipExtraction": BuildDropshipExtraction(); break;
        default: Main.LogDebug($"[DropshipNodeBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildDropshipLandingSpot() {
      string dropshipStatesRaw = build.ContainsKey("DropshipStates") ? build["DropshipStates"].ToString() : "Landed";
      string useDropshipTypeRaw = build.ContainsKey("UseDropshipType") ? build["UseDropshipType"].ToString() : "Any";
      bool automarkLandingZone = build.ContainsKey("AutoMarkLandingZone") ? (bool)build["AutoMarkLandingZone"] : true;
      string teamGUID = build["Team"].ToString();
      List<string> dropshipGUIDs = ((JArray)build["DropshipGameLogicList"]).ToObject<List<string>>();

      StartingDropshipAnimationState dropshipStates = (StartingDropshipAnimationState)Enum.Parse(typeof(StartingDropshipAnimationState), dropshipStatesRaw);
      CustomDropshipType useDropshipType = (CustomDropshipType)Enum.Parse(typeof(CustomDropshipType), useDropshipTypeRaw);

      List<DropshipRef> dropshipRefs = new List<DropshipRef>();

      foreach (string dropshipGUID in dropshipGUIDs) {
        DropshipRef dropshipRef = new DropshipRef();
        dropshipRef.EncounterObjectGuid = dropshipGUID;
        dropshipRefs.Add(dropshipRef);
      }

      CustomDropshipLandingSpotGameLogic customDropshipLandingSpotGameLogic = DropshipNodeFactory.CreateDropshipLandingSpot(this.parent, this.name, this.guid, dropshipStates, useDropshipType, automarkLandingZone, teamGUID, dropshipRefs);
    }

    private void BuildDropshipExtraction() {
      string dropshipLandingSpotGUID = build["DropshipLandingSpotGuid"].ToString();
      string callDropshipObjectiveGUID = build["CallDropshipObjectiveGuid"].ToString();
      string loadDropshipObjectiveGUID = build["LoadDropshipObjectiveGuid"].ToString();
      List<string> requiredTagsOnLance = build.ContainsKey("RequiredTagsOnLance") ? build["RequiredTagsOnLance"].ToObject<List<string>>() : null;
      bool spawnLancesWhenLanded = build.ContainsKey("SpawnLancesWhenLanded") ? (bool)build["SpawnLancesWhenLanded"] : false;
      bool takeOffImmediately = build.ContainsKey("TakeOffImmediately") ? (bool)build["TakeOffImmediately"] : false;
      bool extractViaDropship = build.ContainsKey("ExtractViaDropship") ? (bool)build["ExtractViaDropship"] : true;

      CustomDropshipLandingSpotRef dropshipLandingSpotRef = new CustomDropshipLandingSpotRef();
      dropshipLandingSpotRef.EncounterObjectGuid = dropshipLandingSpotGUID;

      ObjectiveRef callDropshipObjectiveRef = new ObjectiveRef();
      callDropshipObjectiveRef.EncounterObjectGuid = callDropshipObjectiveGUID;

      ObjectiveRef loadDropshipObjectiveRef = new ObjectiveRef();
      loadDropshipObjectiveRef.EncounterObjectGuid = loadDropshipObjectiveGUID;

      DropshipNodeFactory.CreateDropshipExtraction(this.parent, this.name, this.guid, dropshipLandingSpotRef, callDropshipObjectiveRef, loadDropshipObjectiveRef, requiredTagsOnLance, spawnLancesWhenLanded, takeOffImmediately, extractViaDropship);
    }
  }
}