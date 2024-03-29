using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Collections;

namespace MissionControl.EncounterFactories {
  public class ObjectiveFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject((name == null) ? "Objective" : name);
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;
      return go;
    }

    private static void AttachRequiredReferences(ObjectiveGameLogic objectiveGameLogic, string contractObjectiveGuid) {
      if (contractObjectiveGuid != null) {
        ContractObjectiveGameLogic contractObjectiveGameLogic = MissionControl.Instance.EncounterLayerData.GetContractObjectiveGameLogicByGUID(contractObjectiveGuid);
        ObjectiveRef objectiveRef = new ObjectiveRef(objectiveGameLogic);
        contractObjectiveGameLogic.objectiveRefList.Add(objectiveRef);
      }

      objectiveGameLogic.onSuccessDialogue = new DialogueRef();
      objectiveGameLogic.onFailureDialogue = new DialogueRef();
    }

    public static ContractObjectiveGameLogic CreateContractObjective(ObjectiveGameLogic objective) {
      ContractObjectiveGameLogic contractObjectiveGameLogic = objective.transform.parent.gameObject.AddComponent<ContractObjectiveGameLogic>();
      contractObjectiveGameLogic.encounterObjectGuid = Guid.NewGuid().ToString();
      contractObjectiveGameLogic.objectiveRefList.Add(new ObjectiveRef(objective));
      return contractObjectiveGameLogic;
    }

    public static EmptyObjectiveObjective CreateEmptyObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, string description,
      bool isPrimaryObjectve, int priority, bool displayToUser) {

      GameObject emptyObjectiveGo = CreateGameObject(parent, objectName);

      EmptyObjectiveObjective emptyObjective = emptyObjectiveGo.AddComponent<EmptyObjectiveObjective>();
      emptyObjective.title = title;
      emptyObjective.description = description;
      emptyObjective.priority = priority;
      emptyObjective.displayToUser = displayToUser;

      AttachRequiredReferences(emptyObjective, contractObjectiveGuid);

      return emptyObjective;
    }

    public static DestroyLanceObjective CreateDestroyLanceObjective(string objectiveGuid, GameObject parent, LanceSpawnerRef lanceToDestroy, string lanceGuid, string title, bool showProgress,
      string progressFormat, string description, int priority, bool displayToUser, ObjectiveMark markUnitsWith, string contractObjectiveGameLogicGuid, Dictionary<string, float> rewards, bool createObjectiveOverride = true) {

      // TODO: Probably want to split out these two main chunks into their own methods
      // OBJECTIVE OBJECTIVE GAME LOGIC
      GameObject destroyWholeLanceObjectiveGo = CreateGameObject(parent, $"Objective_DestroyLance_{lanceGuid}");

      DestroyLanceObjective destroyLanceObjective = destroyWholeLanceObjectiveGo.AddComponent<DestroyLanceObjective>();
      destroyLanceObjective.encounterObjectGuid = objectiveGuid;
      destroyLanceObjective.title = title;
      destroyLanceObjective.showProgress = showProgress;
      destroyLanceObjective.progressFormat = progressFormat;
      destroyLanceObjective.description = description;
      destroyLanceObjective.priority = priority;
      destroyLanceObjective.displayToUser = displayToUser;
      destroyLanceObjective.markUnitsWith = markUnitsWith;
      destroyLanceObjective.lanceToDestroy = lanceToDestroy;

      // Rewards
      List<SimGameEventResult> onSuccessResults = new List<SimGameEventResult>();
      foreach (KeyValuePair<string, float> reward in rewards) {
        onSuccessResults.Add(CreateRewardResult(reward.Key, reward.Value));
      }

      destroyLanceObjective.OnSuccessResults = onSuccessResults;
      destroyLanceObjective.onSuccessDialogue = new DialogueRef();
      destroyLanceObjective.onFailureDialogue = new DialogueRef();

      ContractObjectiveGameLogic contractObjectiveGameLogic = null;
      if (contractObjectiveGameLogicGuid == null) {
        // For ease of user we track objectives, by default, against the first contract objective
        contractObjectiveGameLogic = MissionControl.Instance.EncounterLayerData.GetComponent<ContractObjectiveGameLogic>();
      } else {
        contractObjectiveGameLogic = MissionControl.Instance.EncounterLayerData.GetContractObjectiveGameLogicByGUID(contractObjectiveGameLogicGuid);
      }

      if (contractObjectiveGameLogic == null) {
        Main.Logger.LogError($"[CreateDestroyLanceObjective] Contract Objective is null!");
      }

      ObjectiveRef objectiveRef = new ObjectiveRef(destroyLanceObjective);
      contractObjectiveGameLogic.objectiveRefList.Add(objectiveRef);

      // OBJECTIVE OVERRIDE - This is needed otherwise the results don't apply in the 'End Contract' screen
      // IF - the objective override is not specified in the contract override .json
      if (createObjectiveOverride) {
        ObjectiveOverride objectiveOverride = new ObjectiveOverride(destroyLanceObjective);
        objectiveOverride.title = destroyLanceObjective.title;
        objectiveOverride.description = "MC" + destroyLanceObjective.description;  // Important and used for objective cleanup
        objectiveOverride.OnSuccessResults = destroyLanceObjective.OnSuccessResults;
        objectiveOverride.OnFailureResults = destroyLanceObjective.OnFailureResults;
        objectiveOverride.OnSuccessDialogueGUID = destroyLanceObjective.onSuccessDialogue.EncounterObjectGuid;
        objectiveOverride.OnFailureDialogueGUID = destroyLanceObjective.onFailureDialogue.EncounterObjectGuid;
        MissionControl.Instance.CurrentContract.Override.objectiveList.Add(objectiveOverride);
      }

      return destroyLanceObjective;
    }

    public static SimGameEventResult CreateRewardResult(string type, float value) {
      SimGameEventResult rewardResult = new SimGameEventResult();
      rewardResult.Scope = EventScope.Company;

      SimGameStat rewardStat = new SimGameStat(type, value);
      rewardResult.Stats = new SimGameStat[] { rewardStat };

      return rewardResult;
    }

    public static OccupyRegionObjective CreateOccupyRegionObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string requiredLanceSpawnerGuid, string regionGameLogicGuid,
    string objectName, string title, string progressFormat, string description, int numberOfUnitsToOccupy, int durationToOccupy, DurationType durationType, bool useDropship, string[] requiredTagsOnUnit, string[] requiredTagsOnOpposingUnits) {
      GameObject occupyRegionObjectiveGo = CreateGameObject(parent, objectName);

      OccupyRegionObjective occupyRegionObjective = occupyRegionObjectiveGo.AddComponent<OccupyRegionObjective>();
      occupyRegionObjective.title = occupyRegionObjectiveGo.name;
      occupyRegionObjective.encounterObjectGuid = objectiveGuid;
      occupyRegionObjective.requiredTagsOnUnit = (requiredTagsOnUnit == null) ? new TagSet(new string[] { "player_unit" }) : new TagSet(requiredTagsOnUnit);

      LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
      lanceSpawnerRef.EncounterObjectGuid = requiredLanceSpawnerGuid;
      occupyRegionObjective.requiredLance = lanceSpawnerRef;

      occupyRegionObjective.durationType = durationType;
      occupyRegionObjective.durationToOccupy = durationToOccupy;

      occupyRegionObjective.numberOfUnitsToOccupy = numberOfUnitsToOccupy;

      occupyRegionObjective.applyTagsWhen = ApplyTagsWhen.OnCompleteObjective;

      occupyRegionObjective.requiredTagsOnOpposingUnits = (requiredTagsOnOpposingUnits == null) ? new TagSet(new string[] { "opposing_unit" }) : new TagSet(requiredTagsOnOpposingUnits);

      RegionRef regionRef = new RegionRef();
      regionRef.EncounterObjectGuid = regionGameLogicGuid;
      occupyRegionObjective.occupyTargetRegion = regionRef;

      occupyRegionObjective.triggerDropshipFlybyPickupOnSuccess = useDropship;
      occupyRegionObjective.extractViaDropship = useDropship;
      occupyRegionObjective.title = title;
      occupyRegionObjective.showProgress = true;
      occupyRegionObjective.progressFormat = progressFormat;
      occupyRegionObjective.description = description;
      occupyRegionObjective.priority = 3;
      occupyRegionObjective.displayToUser = true;
      occupyRegionObjective.checkObjectiveFlag = false;
      occupyRegionObjective.useBeacon = true;
      occupyRegionObjective.markUnitsWith = ObjectiveMark.None;
      occupyRegionObjective.enableObjectiveLogging = true;

      AttachRequiredReferences(occupyRegionObjective, contractObjectiveGuid);

      return occupyRegionObjective;
    }

    public static DefendXUnitsObjective CreateDefendXUnitsObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, int priority,
      string progressFormat, string description, string[] requiredTagsOnUnit, int numberOfUnitsToDefend, int durationToDefend, DurationType durationType) {

      GameObject defendXUnitsObjectiveGo = CreateGameObject(parent, objectName);

      DefendXUnitsObjective defendXUnitsObjective = defendXUnitsObjectiveGo.AddComponent<DefendXUnitsObjective>();
      defendXUnitsObjective.title = defendXUnitsObjectiveGo.name;
      defendXUnitsObjective.encounterObjectGuid = objectiveGuid;
      defendXUnitsObjective.requiredTagsOnUnit = new TagSet(requiredTagsOnUnit);

      defendXUnitsObjective.numberOfUnitsToDefend = numberOfUnitsToDefend;
      defendXUnitsObjective.durationToOccupy = durationToDefend;
      defendXUnitsObjective.durationType = durationType;

      defendXUnitsObjective.title = title;
      defendXUnitsObjective.showProgress = true;
      defendXUnitsObjective.progressFormat = progressFormat;
      defendXUnitsObjective.description = description;
      defendXUnitsObjective.priority = priority;

      defendXUnitsObjective.displayToUser = true;
      defendXUnitsObjective.checkObjectiveFlag = false; // maybe true?
      defendXUnitsObjective.useBeacon = true;
      defendXUnitsObjective.markUnitsWith = ObjectiveMark.DefendTarget;
      defendXUnitsObjective.enableObjectiveLogging = true;

      AttachRequiredReferences(defendXUnitsObjective, contractObjectiveGuid);

      return defendXUnitsObjective;
    }

    public static DefendXUnitsForeverObjective CreateDefendXUnitsForeverObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, int priority,
      string progressFormat, string description, string[] requiredTagsOnUnit, int numberOfUnitsToDefend) {

      GameObject defendXUnitsObjectiveGo = CreateGameObject(parent, objectName);

      DefendXUnitsForeverObjective defendXUnitsObjective = defendXUnitsObjectiveGo.AddComponent<DefendXUnitsForeverObjective>();
      defendXUnitsObjective.title = defendXUnitsObjectiveGo.name;
      defendXUnitsObjective.encounterObjectGuid = objectiveGuid;
      defendXUnitsObjective.requiredTagsOnUnit = new TagSet(requiredTagsOnUnit);

      defendXUnitsObjective.numberOfUnitsToDefend = numberOfUnitsToDefend;

      defendXUnitsObjective.title = title;
      defendXUnitsObjective.showProgress = true;
      defendXUnitsObjective.progressFormat = progressFormat;
      defendXUnitsObjective.description = description;
      defendXUnitsObjective.priority = priority;

      defendXUnitsObjective.displayToUser = true;
      defendXUnitsObjective.checkObjectiveFlag = false; // maybe true?
      defendXUnitsObjective.useBeacon = false;
      defendXUnitsObjective.markUnitsWith = ObjectiveMark.DefendTarget;
      defendXUnitsObjective.enableObjectiveLogging = true;

      AttachRequiredReferences(defendXUnitsObjective, contractObjectiveGuid);

      return defendXUnitsObjective;
    }
  }
}