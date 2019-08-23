using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using System.Linq;
using System.Collections.Generic;

using MissionControl.Utils;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
	public class FailObjectivesResult : EncounterResult {
    public List<string> ObjectiveNameWhiteList { get; set; } = new List<string>();

		public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
			Main.LogDebug("[FailObjectivesResult] Failing objectives...");

			ContractObjectiveGameLogic[] existingContractObjectives = MissionControl.Instance.EncounterLayerData.GetComponents<ContractObjectiveGameLogic>();
			existingContractObjectives.ToList().ForEach(obj => {
				obj.OverrideRequiredByMission(false);
				obj.objectiveRefList.ForEach(objective => {
					Main.LogDebug("[FailObjectivesResult] Objective name is: " + objective.encounterObject.name);
					if (!ObjectiveNameWhiteList.Contains(objective.encounterObject.name)) {
						objective.encounterObject.CompleteObjective("Dynamic Withdraw Triggered", CompleteObjectiveType.Failed, true, true);
					}
				});
				ReflectionHelper.SetPrivateField(obj, "currentObjectiveStatus", ObjectiveStatus.Failed);
			});
    }
	}
}
