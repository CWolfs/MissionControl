using System;

using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl;

namespace MissionControl.Conditional {
  public class ObjectiveStatusesConditional : DesignConditional {

    public Dictionary<string, ObjectiveStatusEvaluationType> Statuses {
      get {
        return statuses;
      }
      set {
        statuses = value;
        SetCompletedStatuses();
      }
    }
    public bool NotInContractObjectivesAreSuccesses { get; set; }

    private Dictionary<string, ObjectiveStatusEvaluationType> statuses;
    private Dictionary<string, bool> completedStatus = new Dictionary<string, bool>();


    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      List<string> uncompletedObjectives = completedStatus.Where(status => status.Value == false).Select(status => status.Key).ToList();
      Main.LogDebug($"[ObjectiveStatusesConditional] Found '{uncompletedObjectives.Count} / {Statuses.Count}' uncompleted objectives");

      foreach (string objectiveGuid in uncompletedObjectives) {
        Main.LogDebug($"[ObjectiveStatusesConditional] Evaluting uncompleted objective '{objectiveGuid}'");
        EvaluateChild(objectiveGuid, responseName);
      }

      if (completedStatus.All(status => status.Value == true)) {
        Main.LogDebug($"[ObjectiveStatusesConditional] All objectives are completed. Turning success.");
        return true;
      }
      return false;
    }

    private void SetCompletedStatuses() {
      foreach (KeyValuePair<string, ObjectiveStatusEvaluationType> status in Statuses) {
        completedStatus[status.Key] = false;
      }
    }

    private void EvaluateChild(string guid, string responseName) {
      ObjectiveGameLogic objectiveGameLogic = this.combat.ItemRegistry.GetItemByGUID<ObjectiveGameLogic>(guid);
      ObjectiveStatusEvaluationType objectiveStatus = Statuses[guid];

      if (objectiveGameLogic.IsAnInactiveContractControlledObjective()) {
        Main.LogDebug($"[ObjectiveStatusesConditional] '{objectiveGameLogic.gameObject.name}' is an objective in an inactive contract controlled chunk. Auto-suceeeding objective for this check");
        completedStatus[guid] = true;
        return;
      }

      switch (objectiveStatus) {
        case ObjectiveStatusEvaluationType.InProgress: {
          string message2;
          if (objectiveGameLogic.IsInProgress) {
            message2 = string.Format("Objective[{0}] is in progress.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            completedStatus[guid] = true;
            return;
          }
          message2 = string.Format("Objective[{0}] is NOT in progress.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return;
        }
        case ObjectiveStatusEvaluationType.Complete: {
          string message2;
          if (objectiveGameLogic.IsComplete) {
            message2 = string.Format("Objective[{0}] is Complete.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            completedStatus[guid] = true;
            return;
          }
          message2 = string.Format("Objective[{0}] is NOT Complete.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return;
        }
        case ObjectiveStatusEvaluationType.Success: {
          string message2;
          if (objectiveGameLogic.CurrentObjectiveStatus == ObjectiveStatus.Succeeded) {
            message2 = string.Format("Objective[{0}] is Succeeded.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            completedStatus[guid] = true;
            return;
          }
          message2 = string.Format("Objective[{0}] is NOT Succeeded.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return;
        }
        case ObjectiveStatusEvaluationType.Failed: {
          string message2;
          if (objectiveGameLogic.CurrentObjectiveStatus == ObjectiveStatus.Failed) {
            message2 = string.Format("Objective[{0}] is Failed.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            completedStatus[guid] = true;
          }
          message2 = string.Format("Objective[{0}] is NOT Failed.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return;
        }
        case ObjectiveStatusEvaluationType.NotInProgress: {
          string message2;
          if (!objectiveGameLogic.IsInProgress) {
            message2 = string.Format("Objective[{0}] is NOT in progress.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            completedStatus[guid] = true;
            return;
          }
          message2 = string.Format("Objective[{0}] is in progress.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return;
        }
        default: {
          string message2 = string.Format("Objective[{0}] status is weird! [{1}]", objectiveGameLogic.DisplayName, objectiveStatus);
          base.LogEvaluationFailed(message2, responseName);
          return;
        }
      }
    }
  }
}
