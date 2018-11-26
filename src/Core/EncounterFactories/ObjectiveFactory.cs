using UnityEngine;
using System;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

namespace MissionControl.EncounterFactories {
  public class ObjectiveFactory {
    public static DestroyLanceObjective CreateDestroyLanceObjective(string objectiveGuid, GameObject parent, LanceSpawnerRef lanceToDestroy, string lanceGuid, string title, bool showProgress,
    string progressFormat, string description, int priority, bool displayToUser, ObjectiveMark markUnitsWith) {
      GameObject destroyWholeLanceObjectiveGo = new GameObject($"Objective_DestroyLance_{lanceGuid}");
      destroyWholeLanceObjectiveGo.transform.parent = parent.transform;
      destroyWholeLanceObjectiveGo.transform.localPosition = Vector3.zero;

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

      return destroyLanceObjective;
    }

    public static ContractObjectiveGameLogic CreateDestroyLanceContractObjective(DestroyLanceObjective destroyLanceObjective) {
      ContractObjectiveGameLogic contractObjectiveGameLogic = destroyLanceObjective.transform.parent.gameObject.AddComponent<ContractObjectiveGameLogic>();
      contractObjectiveGameLogic.objectiveRefList.Add(new ObjectiveRef(destroyLanceObjective));
      return contractObjectiveGameLogic;
    }
  }
}