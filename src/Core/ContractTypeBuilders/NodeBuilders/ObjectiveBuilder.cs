using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Data;
using MissionControl.Logic;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

using Harmony;

namespace MissionControl.ContractTypeBuilders {
  public class ObjectiveBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject objective;

    private GameObject parent;
    private string name;
    private string subType;
    private string guid;
    private bool isPrimaryObjectve;
    private string title;
    private int priority;
    private bool displayToUser;
    private string contractObjectiveGuid;

    public ObjectiveBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject objective) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.objective = objective;

      this.parent = parent;
      this.name = objective["Name"].ToString();
      this.subType = objective["SubType"].ToString();
      this.guid = objective["Guid"].ToString();
      this.isPrimaryObjectve = (bool)objective["IsPrimaryObjective"];
      this.title = objective["Title"].ToString();
      this.priority = (int)objective["Priority"];
      this.displayToUser = objective.ContainsKey("DisplayToUser") ? (bool)objective["DisplayToUser"] : true;
      this.contractObjectiveGuid = objective.ContainsKey("ContractObjectiveGuid") ? objective["ContractObjectiveGuid"].ToString() : "";
    }

    public override void Build() {
      switch (subType) {
        case "Generic": BuildGenericObjective(parent, objective, name, title, guid, isPrimaryObjectve, priority, displayToUser, contractObjectiveGuid); break;
        case "DestroyLance": BuildDestroyWholeLanceObjective(parent, objective, name, title, guid, isPrimaryObjectve, priority, displayToUser, contractObjectiveGuid); break;
        case "OccupyRegion": BuildOccupyRegionObjective(parent, objective, name, title, guid, isPrimaryObjectve, priority, displayToUser, contractObjectiveGuid); break;
        case "DefendXUnits": BuildDefendXUnitsObjective(parent, objective, name, title, guid, isPrimaryObjectve, priority, displayToUser, contractObjectiveGuid); break;
        case "DestroyXUnits": BuildDestroyXUnitsObjective(parent, objective, name, title, guid, isPrimaryObjectve, priority, displayToUser, contractObjectiveGuid); break;
        case "DestroyXDestructibles": BuildDestroyXDestructiblesObjective(parent, objective, name, title, guid, isPrimaryObjectve, priority, displayToUser, contractObjectiveGuid); break;
        default: Main.LogDebug($"[ObjectiveBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildGenericObjective(GameObject parent, JObject objective, string name, string title, string guid,
      bool isPrimaryObjectve, int priority, bool displayToUser, string contractObjectiveGuid) {

      string description = (objective.ContainsKey("Description") ? objective["Description"].ToString() : title);

      EmptyObjectiveObjective objectiveLogic = ObjectiveFactory.CreateEmptyObjective(guid, parent, contractObjectiveGuid, name, title, description, isPrimaryObjectve, priority, displayToUser);
    }

    private void BuildDestroyWholeLanceObjective(GameObject parent, JObject objective, string name, string title, string guid,
      bool isPrimaryObjectve, int priority, bool displayToUser, string contractObjectiveGuid) {

      DestroyWholeLanceChunk destroyWholeLanceChunk = parent.GetComponent<DestroyWholeLanceChunk>();
      string lanceToDestroyGuid = objective["LanceToDestroyGuid"].ToString();
      Dictionary<string, float> rewards = (objective.ContainsKey("Rewards")) ? objective["Rewards"].ToObject<Dictionary<string, float>>() : new Dictionary<string, float>();
      bool showProgress = true;

      LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
      lanceSpawnerRef.EncounterObjectGuid = lanceToDestroyGuid;

      DestroyLanceObjective objectiveLogic = ObjectiveFactory.CreateDestroyLanceObjective(
        guid,
        parent,
        lanceSpawnerRef,
        lanceToDestroyGuid,
        title,
        showProgress,
        ChunkLogic.ProgressFormat.PERCENTAGE_COMPLETE,
        "The primary objective to destroy the enemy lance",
        priority,
        displayToUser,
        ObjectiveMark.AttackTarget,
        contractObjectiveGuid,
        rewards,
        rewards.Count > 0
      );

      if (isPrimaryObjectve) {
        objectiveLogic.primary = true;
      } else {
        objectiveLogic.primary = false;
      }

      DestroyLanceObjectiveRef destroyLanceObjectiveRef = new DestroyLanceObjectiveRef();
      destroyLanceObjectiveRef.encounterObject = objectiveLogic;

      if (destroyWholeLanceChunk != null) {
        destroyWholeLanceChunk.lanceSpawner = lanceSpawnerRef;
        destroyWholeLanceChunk.destroyObjective = destroyLanceObjectiveRef;
      }
    }

    private void BuildOccupyRegionObjective(GameObject parent, JObject objective, string name, string title, string guid,
      bool isPrimaryObjectve, int priority, bool displayToUser, string contractObjectiveGuid) {

      string lanceToUseRegionGuid = objective["LanceToUseRegionGuid"].ToString();
      string regionGuid = objective["RegionGuid"].ToString();
      string progressFormat = (objective.ContainsKey("ProgressFormat")) ? objective["ProgressFormat"].ToString() : "";
      string description = objective["Description"].ToString();
      int numberOfUnitsToOccupy = (objective.ContainsKey("NumberOfUnitsToOccupy")) ? (int)objective["NumberOfUnitsToOccupy"] : 0;
      int durationToOccupy = (objective.ContainsKey("DurationToOccupy")) ? (int)objective["DurationToOccupy"] : 0;
      string durationTypeStr = (objective.ContainsKey("DurationType")) ? objective["DurationType"].ToString() : "Rounds";
      DurationType durationType = (DurationType)Enum.Parse(typeof(DurationType), durationTypeStr);

      bool useDropship = (objective.ContainsKey("UseDropship")) ? (bool)objective["UseDropship"] : false;
      string[] requiredTagsOnUnit = (objective.ContainsKey("RequiredTagsOnUnit")) ? ((JArray)objective["RequiredTagsOnUnit"]).ToObject<string[]>() : null;
      string[] requiredTagsOnOpposingUnits = (objective.ContainsKey("RequiredTagsOpposingUnits")) ? ((JArray)objective["RequiredTagsOpposingUnits"]).ToObject<string[]>() : null;

      OccupyRegionObjective occupyRegionObjective = ObjectiveFactory.CreateOccupyRegionObjective(
        guid,
        parent,
        contractObjectiveGuid,
        lanceToUseRegionGuid,
        regionGuid,
        this.name,
        title,
        progressFormat,
        description,
        numberOfUnitsToOccupy,
        durationToOccupy,
        durationType,
        useDropship,
        requiredTagsOnUnit,
        requiredTagsOnOpposingUnits
      );
    }

    private void BuildDefendXUnitsObjective(GameObject parent, JObject objective, string name, string title, string guid,
      bool isPrimaryObjectve, int priority, bool displayToUser, string contractObjectiveGuid) {

      string[] requiredTagsOnUnit = (objective.ContainsKey("RequiredTagsOnUnit")) ? ((JArray)objective["RequiredTagsOnUnit"]).ToObject<string[]>() : null;
      int numberOfUnitsToDefend = (objective.ContainsKey("NumberOfUnitsToDefend")) ? ((int)objective["NumberOfUnitsToDefend"]) : 1;
      int durationToDefend = (objective.ContainsKey("DurationToDefend")) ? (int)objective["DurationToDefend"] : 0;
      string durationTypeStr = (objective.ContainsKey("DurationType")) ? objective["DurationType"].ToString() : "Rounds";
      string progressFormat = (objective.ContainsKey("ProgressFormat")) ? objective["ProgressFormat"].ToString() : "";
      string description = objective["Description"].ToString();

      if (durationToDefend <= 0) {
        ObjectiveFactory.CreateDefendXUnitsForeverObjective(guid, parent, contractObjectiveGuid, name, title, priority, progressFormat, description, requiredTagsOnUnit, numberOfUnitsToDefend);
      } else {
        DurationType durationType = (DurationType)Enum.Parse(typeof(DurationType), durationTypeStr);
        ObjectiveFactory.CreateDefendXUnitsObjective(guid, parent, contractObjectiveGuid, name, title, priority, progressFormat, description, requiredTagsOnUnit, numberOfUnitsToDefend, durationToDefend, durationType);
      }
    }

    private void BuildDestroyXUnitsObjective(GameObject parent, JObject objective, string name, string title, string guid,
      bool isPrimaryObjectve, int priority, bool displayToUser, string contractObjectiveGuid) {

      string[] requiredTagsOnUnit = (objective.ContainsKey("RequiredTagsOnUnit")) ? ((JArray)objective["RequiredTagsOnUnit"]).ToObject<string[]>() : null;
      int numberOfUnitsToDestroy = (objective.ContainsKey("NumberOfUnitsToDestroy")) ? ((int)objective["NumberOfUnitsToDestroy"]) : 1;
      string progressFormat = (objective.ContainsKey("ProgressFormat")) ? objective["ProgressFormat"].ToString() : "";
      string description = objective["Description"].ToString();

      ObjectiveFactory.CreateDestroyXUnitsObjective(guid, parent, contractObjectiveGuid, name, title, priority, progressFormat, description, requiredTagsOnUnit, numberOfUnitsToDestroy);

      // Test
      // GameObject templateBuildingGo = GameObject.Find("PlotParent/Plot2/plotVariant_facility_med_military_airControlBase (1)/envNstSets_militaryTowerAirControlGroupingA");
      // if (templateBuildingGo == null) Main.Logger.Log("Template building GO is null");
      // GameObject buildingGo = GameObject.Instantiate(templateBuildingGo, new Vector3(-336, 175, 53), templateBuildingGo.transform.rotation, MissionControl.Instance.EncounterLayerData.gameObject.transform);

      GameObject customfacility = GameObject.Find("CustomFacility");
      if (customfacility == null) {
        Vector3 facilityPosition = new Vector3(-336, 175, 53);
        GameObject facilityGo = BuildingFactory.CreateFacility("CustomFacility", facilityPosition);
        // facilityGo.transform.position = new Vector3(-336, 175, 53); // in the region
      }

      // facilityGo.transform.position = new Vector3(-400, 160, 340); // in the road near spawn
    }

    private void BuildDestroyXDestructiblesObjective(GameObject parent, JObject objective, string name, string title, string guid,
      bool isPrimaryObjectve, int priority, bool displayToUser, string contractObjectiveGuid) {

      string regionGuid = objective["RegionGuid"].ToString();
      string countType = (objective.ContainsKey("CountType")) ? objective["CountType"].ToString() : "Number";
      int valueOfDestructiblesToDestroy = (objective.ContainsKey("ValueOfDestructiblesToDestroy")) ? ((int)objective["ValueOfDestructiblesToDestroy"]) : 1;
      string progressFormat = (objective.ContainsKey("ProgressFormat")) ? objective["ProgressFormat"].ToString() : "";
      string description = objective["Description"].ToString();

      ObjectiveCountType countTypeEnum = (ObjectiveCountType)Enum.Parse(typeof(ObjectiveCountType), countType);


      ObjectiveFactory.CreateDestroyXDestructiblesObjective(guid, parent, contractObjectiveGuid, name, title, priority, progressFormat, description, regionGuid, countTypeEnum, valueOfDestructiblesToDestroy);
    }
  }
}