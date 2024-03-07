using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Data;
using MissionControl.EncounterNodes.Objectives;

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

        if (contractObjectiveGuid == null) {
          Main.Logger.LogWarning($"[MissionControl.ObjectiveFactory] [{objectiveGameLogic.Name}] Contract objective of GUID '{contractObjectiveGuid}' could not be found. If you want the objective to be linked to a contract objective you need to specify the GUID and ensure the contract override (contract json) sets the same GUID under the 'contractObjectiveList' section");
        }

        if (contractObjectiveGuid == "") {
          Main.Logger.LogWarning($"[MissionControl.ObjectiveFactory] [{objectiveGameLogic.Name}] Contract objective of GUID was empty so it cannot link to a contract object. If you want the objective to be linked to a contract objective you need to specify the GUID and ensure the contract override (contract json) sets the same GUID under the 'contractObjectiveList' section");
        }

        ObjectiveRef objectiveRef = new ObjectiveRef(objectiveGameLogic);
        if (contractObjectiveGameLogic != null) contractObjectiveGameLogic.objectiveRefList.Add(objectiveRef);
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
      string progressFormat, string description, bool isPrimaryObjectve, int priority, bool displayToUser, ObjectiveMark markUnitsWith, string contractObjectiveGameLogicGuid, Dictionary<string, float> rewards, bool createObjectiveOverride = true) {

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

      destroyLanceObjective.primary = isPrimaryObjectve;

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
    string objectName, string title, bool isPrimaryObjectve, string progressFormat, string description, int numberOfUnitsToOccupy, int durationToOccupy, DurationType durationType, bool useDropship, string[] requiredTagsOnUnit, string[] requiredTagsOnOpposingUnits) {
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

      occupyRegionObjective.primary = isPrimaryObjectve;

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

    public static DefendXUnitsObjective CreateDefendXUnitsObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, bool isPrimaryObjectve, int priority,
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

      defendXUnitsObjective.primary = isPrimaryObjectve;

      defendXUnitsObjective.displayToUser = true;
      defendXUnitsObjective.checkObjectiveFlag = false; // maybe true?
      defendXUnitsObjective.useBeacon = true;
      defendXUnitsObjective.markUnitsWith = ObjectiveMark.DefendTarget;
      defendXUnitsObjective.enableObjectiveLogging = true;

      AttachRequiredReferences(defendXUnitsObjective, contractObjectiveGuid);

      return defendXUnitsObjective;
    }

    public static DestroyXUnitsObjective CreateDestroyXUnitsObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, bool isPrimaryObjectve, int priority,
      string progressFormat, string description, string[] requiredTagsOnUnit, int numberOfUnitsToDestroy) {

      GameObject destroyXUnitsObjectiveGo = CreateGameObject(parent, objectName);

      DestroyXUnitsObjective destroyXUnitsObjective = destroyXUnitsObjectiveGo.AddComponent<DestroyXUnitsObjective>();
      destroyXUnitsObjective.title = destroyXUnitsObjectiveGo.name;
      destroyXUnitsObjective.encounterObjectGuid = objectiveGuid;
      destroyXUnitsObjective.requiredTagsOnUnit = new TagSet(requiredTagsOnUnit);

      destroyXUnitsObjective.numberOfUnitsToKill = numberOfUnitsToDestroy;

      destroyXUnitsObjective.title = title;
      destroyXUnitsObjective.showProgress = true;
      destroyXUnitsObjective.progressFormat = progressFormat;
      destroyXUnitsObjective.description = description;
      destroyXUnitsObjective.priority = priority;

      destroyXUnitsObjective.primary = isPrimaryObjectve;

      destroyXUnitsObjective.displayToUser = true;
      destroyXUnitsObjective.checkObjectiveFlag = false; // maybe true?
      destroyXUnitsObjective.useBeacon = false;
      destroyXUnitsObjective.markUnitsWith = ObjectiveMark.AttackTarget;
      destroyXUnitsObjective.enableObjectiveLogging = true;

      AttachRequiredReferences(destroyXUnitsObjective, contractObjectiveGuid);

      return destroyXUnitsObjective;
    }


    public static DestroyXDestructiblesObjective CreateDestroyXDestructiblesObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, bool isPrimaryObjectve, int priority,
      string progressFormat, string description, string regionGuid, ObjectiveCountType countType, int valueOfDestructiblesToDestroy) {

      GameObject destroyXUnitsObjectiveGo = CreateGameObject(parent, objectName);

      DestroyXDestructiblesObjective destroyXDestructiblesObjective = destroyXUnitsObjectiveGo.AddComponent<DestroyXDestructiblesObjective>();
      destroyXDestructiblesObjective.title = destroyXUnitsObjectiveGo.name;
      destroyXDestructiblesObjective.encounterObjectGuid = objectiveGuid;
      destroyXDestructiblesObjective.RegionGuid = regionGuid;

      destroyXDestructiblesObjective.CountType = countType;
      destroyXDestructiblesObjective.valueOfDestructiblesToDestroy = valueOfDestructiblesToDestroy;

      destroyXDestructiblesObjective.title = title;
      destroyXDestructiblesObjective.showProgress = true;
      destroyXDestructiblesObjective.progressFormat = progressFormat;
      destroyXDestructiblesObjective.description = description;
      destroyXDestructiblesObjective.priority = priority;

      destroyXDestructiblesObjective.primary = isPrimaryObjectve;

      destroyXDestructiblesObjective.displayToUser = true;
      destroyXDestructiblesObjective.checkObjectiveFlag = false; // maybe true?
      destroyXDestructiblesObjective.useBeacon = true;
      destroyXDestructiblesObjective.markUnitsWith = ObjectiveMark.None;
      destroyXDestructiblesObjective.enableObjectiveLogging = true;

      AttachRequiredReferences(destroyXDestructiblesObjective, contractObjectiveGuid);

      return destroyXDestructiblesObjective;
    }

    public static DefendXUnitsForeverObjective CreateDefendXUnitsForeverObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, bool isPrimaryObjectve, int priority,
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

      defendXUnitsObjective.primary = isPrimaryObjectve;

      defendXUnitsObjective.displayToUser = true;
      defendXUnitsObjective.checkObjectiveFlag = false; // maybe true?
      defendXUnitsObjective.useBeacon = false;
      defendXUnitsObjective.markUnitsWith = ObjectiveMark.DefendTarget;
      defendXUnitsObjective.enableObjectiveLogging = true;

      AttachRequiredReferences(defendXUnitsObjective, contractObjectiveGuid);

      return defendXUnitsObjective;
    }

    public static TimerObjective CreateTimerObjective(string objectiveGuid, GameObject parent, string contractObjectiveGuid, string objectName, string title, bool isPrimaryObjectve, int priority,
      string progressFormat, string description, DurationType durationType, int durationToCount, int repeatCount, DurationCompleteType durationToComplete, TimerStatusType startingStatus) {

      GameObject timerObjectiveGo = CreateGameObject(parent, objectName);

      TimerObjective timerObjective = timerObjectiveGo.AddComponent<TimerObjective>();
      timerObjective.title = timerObjectiveGo.name;
      timerObjective.encounterObjectGuid = objectiveGuid;

      timerObjective.title = title;
      timerObjective.showProgress = true;
      timerObjective.progressFormat = progressFormat;
      timerObjective.description = description;
      timerObjective.priority = priority;

      timerObjective.primary = isPrimaryObjectve;

      timerObjective.displayToUser = true;
      timerObjective.checkObjectiveFlag = false; // maybe true?
      timerObjective.useBeacon = true;
      timerObjective.markUnitsWith = ObjectiveMark.None;
      timerObjective.enableObjectiveLogging = true;

      timerObjective.durationType = durationType;
      timerObjective.durationToCount = durationToCount;
      timerObjective.restartTimerCount = repeatCount;
      timerObjective.durationCompleteAction = durationToComplete;
      timerObjective.timerStatus = startingStatus;

      AttachRequiredReferences(timerObjective, contractObjectiveGuid);

      return timerObjective;
    }
  }
}