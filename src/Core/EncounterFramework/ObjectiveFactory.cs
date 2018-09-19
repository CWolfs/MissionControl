using UnityEngine;
using System;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

namespace SpawnVariation.EncounterFramework {
  public class ObjectiveFactory {
    public static DestroyLanceObjective CreateDestroyLanceObjective(GameObject parent, LanceSpawnerRef lanceToDestroy, string title, bool showProgress,
    string progressFormat, string description, int priority, bool displayToUser, ObjectiveMark markUnitsWith) {
      GameObject destroyWholeLanceObjectiveGo = new GameObject("Objective_DestroyLance_CWolf");
      destroyWholeLanceObjectiveGo.transform.parent = parent.transform;
      destroyWholeLanceObjectiveGo.transform.localPosition = Vector3.zero;

      DestroyLanceObjective destroyLanceObjective = destroyWholeLanceObjectiveGo.AddComponent<DestroyLanceObjective>();
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
  }
}