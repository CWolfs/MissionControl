using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering.UrbanWarfare;
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
      CombatGameState combat = UnityGameInstance.BattleTechGame?.Combat;
      if (combat == null) {
        Main.Logger.LogError("[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Combat state is null.");
        return;
      }

      RegionGameLogic regionGameLogic = combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      if (Type == "Building") {
        List<BattleTech.Building> buildingsInMap = combat.ItemRegistry.GetObjectsOfType<BattleTech.Building>(TaggedObjectType.Building);
        Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Collected '{buildingsInMap.Count}' buildings to check.");

        if (NumberOfUnits > 0) {
          buildingsInMap.Shuffle();
        }

        EncounterLayerData layerData = combat.EncounterLayerData ?? MissionControl.Instance.EncounterLayerData;
        int processedUnitCount = 0;
        foreach (BattleTech.Building building in buildingsInMap) {
          if (!IsEligibleTargetBuilding(combat, building)) continue;

          BuildingRepresentation buildingRep = building.BuildingRep;
          bool isBuildingInRegion = layerData.IsPointInRegion(buildingRep.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Found building '{buildingRep.gameObject.name}' in region!");
            if (Tags != null && Tags.Length > 0) {
              building.EncounterTags.UnionWith(Tags);
            }

            SetTeam(combat, building);
            SetIsTargetObjective(building);

            processedUnitCount += 1;
            if (NumberOfUnits > 0 && processedUnitCount >= NumberOfUnits) break;
          }
        }

        if (processedUnitCount > 0 && MissionControl.Instance.EncounterLayerData != null) {
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

    private static bool IsEligibleTargetBuilding(CombatGameState combat, BattleTech.Building building) {
      if (building == null || building.IsDead) return false;

      BuildingRepresentation buildingRep = building.BuildingRep;
      if (buildingRep == null || !buildingRep.gameObject.activeInHierarchy) return false;

      ObstructionGameLogic obstruction = buildingRep.GetComponent<ObstructionGameLogic>()
        ?? ObstructionGameLogic.GetObstructionFromBuilding(building, combat.ItemRegistry);
      if (obstruction != null && (!obstruction.IsRealBuilding || !obstruction.IsBuildingEnabled)) return false;

      if (building.UrbanDestructible != null && building.UrbanDestructible.CurDesolationState == UrbanBuildingGroup.DesolationState.Rubble) {
        return false;
      }

      return true;
    }

    private void SetTeam(CombatGameState combat, ICombatant combatant) {
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Setting Team '{Team}' for '{combatant.GameRep?.name} - {combatant.DisplayName}' who is currently on team '{combatant.team?.Name} : {combatant.team?.GUID}'");
      Team oldTeam = combatant.team;
      string newTeamGuid = TeamUtils.GetTeamGuid(Team);
      Team newTeam = string.IsNullOrEmpty(newTeamGuid) ? null : combat.ItemRegistry.GetItemByGUID<Team>(newTeamGuid);
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
      Main.LogDebug($"[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] Setting isObjectiveTarget '{IsObjectiveTarget}' for '{combatant.GameRep?.name} - {combatant.DisplayName}'");
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
