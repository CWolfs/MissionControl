using UnityEngine;

using BattleTech;
using BattleTech.UI;

using Harmony;

namespace MissionControl.Result {
  public class SetUnitsInRegionToBeTaggedObjectivesResult : EncounterResult {
    public string RegionGuid { get; set; }
    public int NumberOfUnits { get; set; } = 0;
    public string Type { get; set; }
    public string Team { get; set; }
    public bool IsObjectiveTarget { get; set; } = true;
    public string[] Tags { get; set; }

    int processedUnitCount = 0;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      TagUnitsInRegion();
    }

    private void TagUnitsInRegion() {
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectivesResult] Tagging '{NumberOfUnits}' '{Type}' in region '{RegionGuid}'");
      RegionGameLogic regionGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[SetUnitsInRegionToBeTaggedObjectivesResult] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      if (Type == "Building") {
        BuildingRepresentation[] buildingsInMap = GameObject.Find("GAME").GetComponentsInChildren<BuildingRepresentation>();
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectivesResult] Collected '{buildingsInMap.Length}' buildings to check.");

        if (NumberOfUnits > 0) {
          buildingsInMap.Shuffle();
        }

        foreach (BuildingRepresentation building in buildingsInMap) {
          bool isBuildingInRegion = RegionUtil.PointInRegion(UnityGameInstance.BattleTechGame.Combat, building.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectivesResult] Found building '{building.gameObject.name}' in region!");
            building.ParentBuilding.EncounterTags.UnionWith(Tags);

            SetTeam(building.ParentBuilding);
            SetIsTargetObjective(building.ParentBuilding);

            if (HasReachedUnitLimit()) break;
          }
        }
      } else {
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectivesResult] Tagging '{Type}' Not Yet Supported");
      }
    }

    private bool HasReachedUnitLimit() {
      processedUnitCount += 1;

      if (NumberOfUnits > 0 && (processedUnitCount >= NumberOfUnits)) {
        processedUnitCount = 0;
        return true;
      }

      return false;
    }

    private void SetTeam(ICombatant combatant) {
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectivesResult] Setting Team '{Team}' for '{combatant.GameRep.name} - {combatant.DisplayName}'");
      combatant.RemoveFromTeam();

      Team newTeam = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(TeamUtils.GetTeamGuid(Team));
      combatant.AddToTeam(newTeam);
    }

    private void SetIsTargetObjective(ICombatant combatant) {
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectivesResult] Setting IsTargetObjective '{IsObjectiveTarget}' for '{combatant.GameRep.name} - {combatant.DisplayName}'");
      ObstructionGameLogic obstructionGameLogic = combatant.GameRep.GetComponent<ObstructionGameLogic>();
      obstructionGameLogic.isObjectiveTarget = true;

      if (Type == "Building") {
        AccessTools.Field(typeof(BattleTech.Building), "isObjectiveTarget").SetValue(combatant, true);
      }

      CombatHUDInWorldElementMgr inworldElementManager = GameObject.Find("uixPrfPanl_HUD(Clone)").GetComponent<CombatHUDInWorldElementMgr>();
      AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddTickMark").Invoke(inworldElementManager, new object[] { combatant });
      AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddInWorldActorElements").Invoke(inworldElementManager, new object[] { combatant });
    }
  }
}
