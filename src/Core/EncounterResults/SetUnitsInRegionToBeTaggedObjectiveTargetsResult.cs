using UnityEngine;

using System.Collections.Generic;


using BattleTech;
using BattleTech.UI;

using Harmony;

namespace MissionControl.Result {
  public class SetUnitsInRegionToBeTaggedObjectiveTargetsResult : EncounterResult {
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
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Tagging '{NumberOfUnits}' '{Type}' in region '{RegionGuid}'");
      RegionGameLogic regionGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      if (Type == "Building") {
        List<BuildingRepresentation> buildingsInMap = GameObjextExtensions.GetBuildingsInMap();
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Collected '{buildingsInMap.Count}' buildings to check.");

        if (NumberOfUnits > 0) {
          buildingsInMap.Shuffle();
        }

        foreach (BuildingRepresentation building in buildingsInMap) {
          bool isBuildingInRegion = RegionUtil.PointInRegion(UnityGameInstance.BattleTechGame.Combat, building.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Found building '{building.gameObject.name}' in region!");
            building.ParentBuilding.EncounterTags.UnionWith(Tags);

            SetTeam(building.ParentBuilding);
            SetIsTargetObjective(building.ParentBuilding);

            if (HasReachedUnitLimit()) break;
          }
        }
      } else {
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Tagging '{Type}' Not Yet Supported.");
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
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Setting Team '{Team}' for '{combatant.GameRep.name} - {combatant.DisplayName}'");
      Team oldTeam = combatant.team;
      Team newTeam = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(TeamUtils.GetTeamGuid(Team));

      if (Type == "Building") {
        oldTeam.RemoveBuilding((BattleTech.Building)combatant);
        newTeam.AddCombatant(combatant);
      } else {
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Setting Team '{Type}' Not Yet Supported. Use 'SetTeamByLanceSpawnerGuid'");
      }

      /*
      // Dumped this here for a visibility test after OnEncounterBegin
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Units in team are: '{newTeam.units.Count}'");
      newTeam.units.ForEach(unit => {
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Hiding unit '{unit.DisplayName}'");
        unit.OnPlayerVisibilityChanged(VisibilityLevel.None);
      });

      List<ICombatant> allLivingCombatants = UnityGameInstance.BattleTechGame.Combat.GetAllLivingCombatants();
      AccessTools.Method(typeof(TurnDirector), "RebuildVisCaches").Invoke(UnityGameInstance.BattleTechGame.Combat.TurnDirector, new object[] { allLivingCombatants });
      */
    }

    private void SetIsTargetObjective(ICombatant combatant) {
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Setting isObjectiveTarget '{IsObjectiveTarget}' for '{combatant.GameRep.name} - {combatant.DisplayName}'");
      ObstructionGameLogic obstructionGameLogic = combatant.GameRep.GetComponent<ObstructionGameLogic>();
      obstructionGameLogic.isObjectiveTarget = true;

      if (Type == "Building") {
        BattleTech.Building building = combatant as BattleTech.Building;
        AccessTools.Field(typeof(BattleTech.Building), "isObjectiveTarget").SetValue(building, true);
        building.BuildingRep.IsTargetable = true;
      }

      CombatHUDInWorldElementMgr inworldElementManager = GameObject.Find("uixPrfPanl_HUD(Clone)").GetComponent<CombatHUDInWorldElementMgr>();
      AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddTickMark").Invoke(inworldElementManager, new object[] { combatant });
      AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddInWorldActorElements").Invoke(inworldElementManager, new object[] { combatant });

      if (Type == "Building") {
        CombatHUDNumFlagHex numFlagEx = inworldElementManager.GetNumFlagForCombatant(combatant);
        CombatHUDFloatieStackActor floatie = inworldElementManager.GetFloatieStackForCombatant(combatant);

        numFlagEx.anchorPosition = CombatHUDInWorldScalingActorInfo.AnchorPosition.Feet;
        floatie.anchorPosition = CombatHUDInWorldScalingActorInfo.AnchorPosition.Feet;
      }
    }
  }
}
