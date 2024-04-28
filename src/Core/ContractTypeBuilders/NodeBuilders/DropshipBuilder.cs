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
        case "LandingSpot": BuildDropshipLandingSpot(); break;
        case "Extraction": BuildDropshipExtraction(); break;
        default: Main.LogDebug($"[DropshipNodeBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildDropshipLandingSpot() {
      string dropshipStatesRaw = build.ContainsKey("DropshipStates") ? build["DropshipStates"].ToString() : "Landed";
      string useDropshipTypeRaw = build.ContainsKey("UseDropshipType") ? build["UseDropshipType"].ToString() : "Any";
      bool automarkLandingZone = build.ContainsKey("AutomarkLandingZone") ? (bool)build["AutomarkLandingZone"] : true;
      string team = build["Team"].ToString();
      List<string> dropshipTags = ((JArray)build["DropshipTags"]).ToObject<List<string>>();

      string teamGUID = TeamUtils.GetTeamGuid(team);
      StartingDropshipAnimationState dropshipStates = (StartingDropshipAnimationState)Enum.Parse(typeof(StartingDropshipAnimationState), dropshipStatesRaw);
      CustomDropshipType useDropshipType = (CustomDropshipType)Enum.Parse(typeof(CustomDropshipType), useDropshipTypeRaw);

      CustomDropshipLandingSpotGameLogic customDropshipLandingSpotGameLogic = DropshipNodeFactory.CreateDropshipLandingSpot(this.parent, this.name, this.guid, dropshipStates, useDropshipType, automarkLandingZone, teamGUID, dropshipTags);
    }

    private void BuildDropshipExtraction() {
      string dropshipLandingSpotGUID = build["DropshipLandingSpotGuid"].ToString();
      string callDropshipObjectiveGUID = build.ContainsKey("CallDropshipObjectiveGuid") ? build["CallDropshipObjectiveGuid"].ToString() : null;
      string loadDropshipObjectiveGUID = build.ContainsKey("LoadDropshipObjectiveGuid") ? build["LoadDropshipObjectiveGuid"].ToString() : null;
      List<string> requiredTagsOnLance = build.ContainsKey("RequiredTagsOnLance") ? build["RequiredTagsOnLance"].ToObject<List<string>>() : new List<string>();
      bool spawnLancesWhenLanded = build.ContainsKey("SpawnLancesWhenLanded") ? (bool)build["SpawnLancesWhenLanded"] : false;
      bool takeOffImmediately = build.ContainsKey("TakeOffImmediately") ? (bool)build["TakeOffImmediately"] : false;
      bool extractViaDropship = build.ContainsKey("ExtractViaDropship") ? (bool)build["ExtractViaDropship"] : true;
      bool showEscapeMessage = build.ContainsKey("ShowEscapeMessage") ? (bool)build["ShowEscapeMessage"] : false;

      CustomDropshipLandingSpotRef dropshipLandingSpotRef = new CustomDropshipLandingSpotRef();
      dropshipLandingSpotRef.EncounterObjectGuid = dropshipLandingSpotGUID;

      ObjectiveRef callDropshipObjectiveRef = new ObjectiveRef();
      callDropshipObjectiveRef.EncounterObjectGuid = callDropshipObjectiveGUID;

      ObjectiveRef loadDropshipObjectiveRef = new ObjectiveRef();
      loadDropshipObjectiveRef.EncounterObjectGuid = loadDropshipObjectiveGUID;

      DropshipNodeFactory.CreateDropshipExtraction(this.parent, this.name, this.guid, dropshipLandingSpotRef, callDropshipObjectiveRef, loadDropshipObjectiveRef, requiredTagsOnLance, spawnLancesWhenLanded, takeOffImmediately, extractViaDropship, showEscapeMessage);
    }
  }
}