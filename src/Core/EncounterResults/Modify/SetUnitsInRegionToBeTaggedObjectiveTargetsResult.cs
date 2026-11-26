using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.UI;

using BattleTech.Framework;

namespace MissionControl.Result {
  public class SetUnitsInRegionToBeTaggedObjectiveTargetsResult : EncounterResult {
    public string RegionGuid { get; set; }
    public int NumberOfUnits { get; set; } = 0;
    public string Type { get; set; }
    public string Team { get; set; }
    public bool IsObjectiveTarget { get; set; } = true;
    public string[] Tags { get; set; }

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

        int processedUnitCount = 0;
        foreach (BuildingRepresentation building in buildingsInMap) {
          if (building == null || building.ParentBuilding == null || building.ParentBuilding.IsDead) continue;

          bool isBuildingInRegion = RegionUtil.PointInRegion(UnityGameInstance.BattleTechGame.Combat, building.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Found building '{building.gameObject.name}' in region!");
            if (Tags != null && Tags.Length > 0) {
              building.ParentBuilding.EncounterTags.UnionWith(Tags);
            }

            SetTeam(building.ParentBuilding);
            SetIsTargetObjective(building.ParentBuilding);

            processedUnitCount += 1;
            if (NumberOfUnits > 0 && processedUnitCount >= NumberOfUnits) break;
          }
        }

        if (processedUnitCount > 0) {
          // Ensure objectives do a check now units have target objective changed
          ObjectiveGameLogic[] objectiveGameLogics = MissionControl.Instance.EncounterLayerData.GetComponentsInChildren<ObjectiveGameLogic>();
          foreach (ObjectiveGameLogic objectiveGameLogic in objectiveGameLogics) {
            objectiveGameLogic.QueueCheckObjective();
          }
        }
      } else {
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Tagging '{Type}' Not Yet Supported.");
      }
    }

    private void SetTeam(ICombatant combatant) {
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Setting Team '{Team}' for '{combatant.GameRep.name} - {combatant.DisplayName}' who is currently on team '{combatant.team?.Name} : {combatant.team?.GUID}'");
      Team oldTeam = combatant.team;
      string newTeamGuid = TeamUtils.GetTeamGuid(Team);
      Team newTeam = string.IsNullOrEmpty(newTeamGuid) ? null : UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(newTeamGuid);
      if (newTeam == null) {
        Main.Logger.LogError($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Could not find Team '{Team}'.");
        return;
      }

      if (oldTeam == newTeam) {
        return;
      }

      if (Type == "Building") {
        oldTeam?.RemoveBuilding((BattleTech.Building)combatant);
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
      ObstructionGameLogic obstructionGameLogic = combatant.GameRep?.GetComponent<ObstructionGameLogic>();
      if (obstructionGameLogic != null) {
        obstructionGameLogic.isObjectiveTarget = IsObjectiveTarget;
      }

      if (Type == "Building") {
        BattleTech.Building building = combatant as BattleTech.Building;
        if (building != null) {
          building.isObjectiveTarget = IsObjectiveTarget;
        }
      }

      if (IsObjectiveTarget) {
        GameObject hudPanel = GameObject.Find("uixPrfPanl_HUD(Clone)");
        CombatHUDInWorldElementMgr inworldElementManager = hudPanel?.GetComponent<CombatHUDInWorldElementMgr>();
        if (inworldElementManager != null) {
          inworldElementManager.AddTickMark(combatant);
          inworldElementManager.AddInWorldActorElements(combatant);

          if (Type == "Building") {
            CombatHUDNumFlagHex numFlagEx = inworldElementManager.GetNumFlagForCombatant(combatant);
            CombatHUDFloatieStackActor floatie = inworldElementManager.GetFloatieStackForCombatant(combatant);

            if (numFlagEx != null) numFlagEx.anchorPosition = CombatHUDInWorldScalingActorInfo.AnchorPosition.Feet;
            if (floatie != null) floatie.anchorPosition = CombatHUDInWorldScalingActorInfo.AnchorPosition.Feet;
          }
        } else {
          Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] 'uixPrfPanl_HUD(Clone)' wasn't found so skipping logic for it.");
        }
      }
    }
  }
}
