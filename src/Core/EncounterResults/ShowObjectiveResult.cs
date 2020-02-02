using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using System.Linq;
using System.Collections.Generic;

using MissionControl.Utils;

using Harmony;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
  public class ShowObjectiveResult : EncounterResult {
    public string ObjectiveGuid { get; set; }
    public bool IsContractObjective { get; set; } = false;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[ShowObjectiveResult] Showing objective for object guid '{ObjectiveGuid}'");

      if (IsContractObjective) {
        ContractObjectiveGameLogic contractObjectiveGameLogic = MissionControl.Instance.EncounterLayerData.GetContractObjectiveGameLogicByGUID(ObjectiveGuid);
        ShowContractObjective(contractObjectiveGameLogic);
      } else {
        ObjectiveGameLogic objectiveGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<ObjectiveGameLogic>(ObjectiveGuid);
        if (objectiveGameLogic != null) {
          ShowObjective(objectiveGameLogic);
        } else {
          Main.Logger.LogError($"[ShowObjectiveResult] ObjectiveGameLogic not found with objective guid '{ObjectiveGuid}'");
        }
      }
    }

    public void ShowObjective(ObjectiveGameLogic objectiveGameLogic) {
      objectiveGameLogic.displayToUser = true;
      AccessTools.Method(typeof(ObjectiveGameLogic), "ShowObjective").Invoke(objectiveGameLogic, null);
    }

    public void ShowContractObjective(ContractObjectiveGameLogic contractObjectiveGameLogic) {
      contractObjectiveGameLogic.LogObjective("Contract Objective Shown");
      contractObjectiveGameLogic.displayToUser = true;
      AccessTools.Field(typeof(ContractObjectiveGameLogic), "currentObjectiveStatus").SetValue(contractObjectiveGameLogic, ObjectiveStatus.Active);
      EncounterLayerParent.EnqueueLoadAwareMessage(new ObjectiveUpdated(contractObjectiveGameLogic.encounterObjectGuid));
    }
  }
}
