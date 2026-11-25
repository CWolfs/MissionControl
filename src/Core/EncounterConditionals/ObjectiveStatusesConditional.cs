using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl;

namespace MissionControl.Conditional {
  public class ObjectiveStatusesConditional : DesignConditional {

    public Dictionary<string, ObjectiveStatusEvaluationType> Statuses { get; set; } = new Dictionary<string, ObjectiveStatusEvaluationType>();
    public bool NotInContractObjectivesAreSuccesses { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      if (Statuses == null || Statuses.Count == 0) {
        return true;
      }

      bool allPassed = true;
      foreach (string objectiveGuid in Statuses.Keys) {
        Main.LogDebug($"[ObjectiveStatusesConditional] Evaluating objective '{objectiveGuid}'");
        bool passed = EvaluateChild(objectiveGuid, responseName);
        if (!passed) {
          allPassed = false;
        }
      }

      if (allPassed) {
        Main.LogDebug($"[ObjectiveStatusesConditional] All objectives are completed. Turning success.");
        return true;
      }
      return false;
    }

    private bool EvaluateChild(string guid, string responseName) {
      ObjectiveGameLogic objectiveGameLogic = this.combat.ItemRegistry.GetItemByGUID<ObjectiveGameLogic>(guid);
      ObjectiveStatusEvaluationType objectiveStatus = Statuses[guid];

      if (objectiveGameLogic == null) {
        Main.Logger.LogError($"[ObjectiveStatusesConditional] Could not find objective with GUID '{guid}'");
        return false;
      }

      if (objectiveGameLogic.IsAnInactiveContractControlledObjective()) {
        if (NotInContractObjectivesAreSuccesses) {
          Main.LogDebug($"[ObjectiveStatusesConditional] '{objectiveGameLogic.gameObject.name}' is an objective in an inactive contract controlled chunk. Auto-succeeding objective for this check");
          return true;
        }

        string inactiveMessage = string.Format("Objective[{0}] is in an inactive contract controlled chunk.", objectiveGameLogic.DisplayName);
        base.LogEvaluationFailed(inactiveMessage, responseName);
        return false;
      }

      switch (objectiveStatus) {
        case ObjectiveStatusEvaluationType.InProgress: {
          string message2;
          if (objectiveGameLogic.IsInProgress) {
            message2 = string.Format("Objective[{0}] is in progress.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            return true;
          }
          message2 = string.Format("Objective[{0}] is NOT in progress.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return false;
        }
        case ObjectiveStatusEvaluationType.Complete: {
          string message2;
          if (objectiveGameLogic.IsComplete) {
            message2 = string.Format("Objective[{0}] is Complete.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            return true;
          }
          message2 = string.Format("Objective[{0}] is NOT Complete.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return false;
        }
        case ObjectiveStatusEvaluationType.Success: {
          string message2;
          if (objectiveGameLogic.CurrentObjectiveStatus == ObjectiveStatus.Succeeded) {
            message2 = string.Format("Objective[{0}] is Succeeded.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            return true;
          }
          message2 = string.Format("Objective[{0}] is NOT Succeeded.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return false;
        }
        case ObjectiveStatusEvaluationType.Failed: {
          string message2;
          if (objectiveGameLogic.CurrentObjectiveStatus == ObjectiveStatus.Failed) {
            message2 = string.Format("Objective[{0}] is Failed.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            return true;
          }
          message2 = string.Format("Objective[{0}] is NOT Failed.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return false;
        }
        case ObjectiveStatusEvaluationType.NotInProgress: {
          string message2;
          if (!objectiveGameLogic.IsInProgress) {
            message2 = string.Format("Objective[{0}] is NOT in progress.", objectiveGameLogic.DisplayName);
            base.LogEvaluationPassed(message2, responseName);
            return true;
          }
          message2 = string.Format("Objective[{0}] is in progress.", objectiveGameLogic.DisplayName);
          base.LogEvaluationFailed(message2, responseName);
          return false;
        }
        default: {
          string message2 = string.Format("Objective[{0}] status is weird! [{1}]", objectiveGameLogic.DisplayName, objectiveStatus);
          base.LogEvaluationFailed(message2, responseName);
          return false;
        }
      }
    }
  }
}
