using UnityEngine;

using BattleTech;

using System.Collections.Generic;

using Harmony;

namespace MissionControl {
  public class EncounterDataManager {
    private static EncounterDataManager instance;
    public static EncounterDataManager Instance {
      get {
        if (instance == null) instance = new EncounterDataManager();
        return instance;
      }
    }

    public void HandlePlotsAndMapMetadataUpdate() {
      // TODO: When new buildings can be added - handle usecase where contract builder added new buildings - don't wipe that data
      if (MissionControl.Instance.IsCustomContractType) {
        OverridePlots();
        ResetAllBuildingData();
        GenerateEncounterLayerBuildingData();
        RefreshMapMetadata();
      }
    }

    public void HandleDeferredContractTypeBuild() {
      if (MissionControl.Instance.IsCustomContractType) {
        ProcessQueuedBuildingMounts();
        ResetAllBuildingData();
        GenerateEncounterLayerBuildingData();
        RefreshMapMetadata();
      }
    }

    public void OverridePlots() {
      GameObject plotParentGo = GameObject.Find("PlotParent");

      // Disable all plots as some are initially on
      List<GameObject> allPlotVariantsList = GameObjextExtensions.GetAllPlotVariantsList();
      foreach (GameObject plotVariant in allPlotVariantsList) {
        plotVariant.SetActive(false);
      }

      // Enable all plots specified by Mission Control.
      // This support is better than vanilla as vanilla only allowed for one variant per plot
      // This allows for all variants for all plots
      PlotOverride plotOverride = MissionControl.Instance.EncounterLayerData.GetComponent<PlotOverride>();
      List<PlotOverrideEntry> plotOverrideEntries = plotOverride.plotOverrideEntryList;

      foreach (PlotOverrideEntry plotOverrideEntry in plotOverrideEntries) {
        Transform plotTransform = plotParentGo.transform.Find(plotOverrideEntry.plotName);
        GameObject plotGo = plotTransform.gameObject;
        GameObject plotVariantGo = plotTransform.Find(plotOverrideEntry.plotVariant).gameObject;

        // Enable
        Main.LogDebug($"[EncounterDataManager.OverridePlots] Enabling plot '{plotOverrideEntry.plotName}' and variant '{plotOverrideEntry.plotVariant}'");
        plotGo.SetActive(true);
        plotVariantGo.SetActive(true);
      }
    }

    public void ResetAllBuildingData() {
      Main.LogDebug($"[EncounterDataManager.ResetAllBuildingData] Resetting all old building data");
      List<BuildingRepresentation> buildingsInMap = GameObjextExtensions.GetBuildingsInMap();
      foreach (BuildingRepresentation buildingRep in buildingsInMap) {
        BattleTech.Building building = buildingRep.ParentBuilding;

        if (building != null) {
          building.isObjectiveTarget = false;
          building.isObjectiveActive = false;
          building.objectiveGUIDS.Clear();
        }

        ObstructionGameLogic obstructionGameLogic = buildingRep.GetComponent<ObstructionGameLogic>();
        if (obstructionGameLogic != null) {
          obstructionGameLogic.isObjectiveTarget = false;
        }
      }
    }

    public void ProcessQueuedBuildingMounts() {
      List<object[]> queuedBuildingMounts = MissionControl.Instance.QueuedBuildingMounts;
      foreach (object[] mountInfo in queuedBuildingMounts) {
        SetMountOnPosition((GameObject)mountInfo[0], (string)mountInfo[1]);
      }
      MissionControl.Instance.QueuedBuildingMounts.Clear();
    }

    public void SetMountOnPosition(GameObject target, string mountTargetPath) {
      GameObject go = GameObject.Find(mountTargetPath);
      if (go == null) {
        Main.Logger.LogError($"[EncounterDataManager.SetMountOnPositions] Target '{mountTargetPath}' could not be found");
        return;
      } else {
        Main.LogDebug($"[EncounterDataManager.SetMountOnPositions] Target '{mountTargetPath}' found with '{go.name}'");
      }

      GameObject copyGo = GameObject.Instantiate(go);
      copyGo.transform.position = go.transform.position;
      Vector3 pos = copyGo.transform.position;
      Collider col = copyGo.GetComponentInChildren<Collider>();
      Vector4 colliderPos = col.transform.position;

      RaycastHit[] hits = Physics.RaycastAll(new Vector3(colliderPos.x, colliderPos.y + 500f, colliderPos.z), go.transform.TransformDirection(Vector3.down), 1000f);
      foreach (RaycastHit hit1 in hits) {
        if (hit1.collider.gameObject.name == col.gameObject.name) {
          pos.y = hit1.point.y;
          break;
        }
      }

      target.transform.position = pos;
      GameObject.Destroy(copyGo);
    }

    public void GenerateEncounterLayerBuildingData() {
      Main.Logger.LogDebug($"[EncounterDataManager.GenerateEncounterLayerBuildingData] Generating building data");
      Terrain terrain = Terrain.activeTerrain;
      float terrainZSize = terrain.terrainData.size.z;
      float terrainXSize = terrain.terrainData.size.x;
      Vector3 terrainPosition = terrain.transform.position;
      float halfMapCellSize = (float)MapMetaDataExporter.cellSize / 2f;
      int halfMapCellSizeInt = MapMetaDataExporter.cellSize / 2;
      EncounterLayerParent encounterLayerParent = MissionControl.Instance.EncounterLayerParent;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      MapMetaDataExporter mapMetaExporter = encounterLayerParent.GetComponent<MapMetaDataExporter>();
      MapMetaData mapMetaData = UnityGameInstance.BattleTechGame.Combat.MapMetaData;
      Vector3 raycastOrigin = new Vector3(0f, 1000f, 0f);

      // Lookups
      Dictionary<string, int> regionRaycastHits = new Dictionary<string, int>();
      List<RegionGameLogic> regionGameObjectList = new List<RegionGameLogic>();
      List<ObstructionGameLogic> obstructionGameObjectList = new List<ObstructionGameLogic>();

      // Marks only the ObstructionGameLogic objects for ray tracing for performance reasons
      mapMetaExporter.MarkCellsForRaycasting(mapMetaData.mapTerrainDataCells, (int)terrain.transform.position.x, (int)terrain.transform.position.z);

      // TODO: Maybe wipe region building lists. Not sure if I really need/want this yet
      RegionGameLogic[] componentsInChildren = encounterLayerData.GetComponentsInChildren<RegionGameLogic>();
      for (int i = 0; i < componentsInChildren.Length; i++) {
        componentsInChildren[i].InitRegionForRayCasting();
      }

      // Iterate over the Z cell range
      for (float i = halfMapCellSize; i < terrainZSize; i += (float)MapMetaDataExporter.cellSize) {
        int zCellIndex = (int)i / MapMetaDataExporter.cellSize;

        // Iterate over the X cell range
        for (float j = halfMapCellSize; j < terrainXSize; j += (float)MapMetaDataExporter.cellSize) {
          int xCellIndex = (int)j / MapMetaDataExporter.cellSize;

          MapEncounterLayerDataCell mapEncounterLayerDataCell = new MapEncounterLayerDataCell();
          if (mapMetaData.mapTerrainDataCells[zCellIndex, xCellIndex].doRayCast) {
            int hitIndex = 0;
            regionRaycastHits.Clear();
            regionGameObjectList.Clear();
            obstructionGameObjectList.Clear();

            for (int k = -halfMapCellSizeInt; k < halfMapCellSizeInt; k++) {
              raycastOrigin.z = i + (float)k + terrainPosition.z;

              for (int l = -halfMapCellSizeInt; l < halfMapCellSizeInt; l++) {
                raycastOrigin.x = j + (float)l + terrainPosition.x;

                RaycastHit[] raycastHits = Physics.RaycastAll(raycastOrigin, Vector3.down);
                List<ObstructionGameLogic> list3 = new List<ObstructionGameLogic>();

                // Go through all the raycasts at Z,X of the terrain by cell size (middle of cell) 
                // Then find any regions hit, record the number of hits/cells the region has
                for (int m = 0; m < raycastHits.Length; m++) {
                  RegionGameLogic regionGameLogic = raycastHits[m].transform.GetComponent<RegionGameLogic>();
                  if (regionGameLogic != null) {
                    if (!regionRaycastHits.ContainsKey(regionGameLogic.encounterObjectGuid)) {
                      regionRaycastHits[regionGameLogic.encounterObjectGuid] = 1;
                    } else {
                      string encounterObjectGuid = regionGameLogic.encounterObjectGuid;
                      regionRaycastHits[encounterObjectGuid]++;
                    }

                    // Cache the region in the lookup
                    if (!regionGameObjectList.Contains(regionGameLogic)) {
                      regionGameObjectList.Add(regionGameLogic);
                    }
                  }

                  ObstructionGameLogic obstructionGameLogicInParent = raycastHits[m].transform.GetComponentInParent<ObstructionGameLogic>();
                  if (obstructionGameLogicInParent != null && raycastHits[m].point.y > mapMetaData.mapTerrainDataCells[zCellIndex, xCellIndex].terrainHeight) {
                    if (obstructionGameLogicInParent.IsBuildingHitAddWorthy) {
                      if (!obstructionGameObjectList.Contains(obstructionGameLogicInParent)) {
                        obstructionGameObjectList.Add(obstructionGameLogicInParent);
                      }
                    }

                    Vector3 normal = raycastHits[m].normal;
                    BuildingRaycastHit buildingRaycastHit = new BuildingRaycastHit {
                      buildingSteepness = 90f - 57.29578f * Mathf.Atan2(normal.y, Mathf.Sqrt(normal.x * normal.x + normal.z * normal.z)),
                      buildingHeight = raycastHits[m].point.y,
                      buildingGuid = obstructionGameLogicInParent.encounterObjectGuid,
                      hitIndex = hitIndex
                    };

                    mapEncounterLayerDataCell.AddBuildingHit(buildingRaycastHit);
                  }
                }
                hitIndex++;
              }
            }

            // For all the regions detected, if it exists in 10 or more cells - add the region to the map encounter layer data (this is vanilla... why?!)
            // And all all obstruction games logics to the region
            foreach (RegionGameLogic regionGameLogic in regionGameObjectList) {
              if (regionRaycastHits[regionGameLogic.encounterObjectGuid] >= 10) {
                mapEncounterLayerDataCell.AddRegion(regionGameLogic);
                foreach (ObstructionGameLogic obstructionGameLogic in obstructionGameObjectList) {
                  regionGameLogic.AddBuildingGuidToRegion(obstructionGameLogic.encounterObjectGuid);
                }
              }
            }
            mapEncounterLayerDataCell.AverageTheBuildingHits();
            mapEncounterLayerDataCell.SortBuildingListByHeight();
          }

          encounterLayerData.mapEncounterLayerDataCells[zCellIndex, xCellIndex] = mapEncounterLayerDataCell;
          encounterLayerData.mapEncounterLayerDataCells[zCellIndex, xCellIndex].relatedTerrainCell = mapMetaData.mapTerrainDataCells[zCellIndex, xCellIndex];
          mapMetaData.mapTerrainDataCells[zCellIndex, xCellIndex].MapEncounterLayerDataCell = mapEncounterLayerDataCell;
        }
      }
    }

    private void RefreshMapMetadata() {
      foreach (MapTerrainDataCell mapTerrainDataCell in UnityGameInstance.BattleTechGame.Combat.MapMetaData.mapTerrainDataCells) {
        mapTerrainDataCell.UpdateCachedValues();
      }
    }
  }
}