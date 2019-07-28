using UnityEngine;
using System;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

namespace MissionControl.EncounterFactories {
  public class EscapeRegionFactory {
    private static float REGION_RADIUS = 70.71068f;

    private static GameObject CreateEscapeRegionGameObject(GameObject parent, string name = null) {
      GameObject escapeRegionGo = new GameObject((name == null) ? "Region_Escape" : name);
      escapeRegionGo.transform.parent = parent.transform;
      escapeRegionGo.transform.localPosition = Vector3.zero;

      return escapeRegionGo;
    }

    private static RegionPointGameLogic CreateRegionPointGameObject(GameObject parent, string name, Vector3 localPosition) {
      GameObject regionPointGo = new GameObject((name == null) ? "RegionPoint" : name);
      regionPointGo.transform.parent = parent.transform;
      regionPointGo.transform.localPosition = localPosition;

      RegionPointGameLogic regionPoint = regionPointGo.AddComponent<RegionPointGameLogic>();

      return regionPoint;
    }

    public static RegionGameLogic CreateEscapeRegion(GameObject parent, string name = null) {
      GameObject escapeRegionGo = CreateEscapeRegionGameObject(parent, name);

      // Test position
      escapeRegionGo.transform.position = new Vector3(-90, 300, 0);

      escapeRegionGo.AddComponent<MeshCollider>();
      escapeRegionGo.AddComponent<MeshFilter>();
      escapeRegionGo.AddComponent<TerrainDataChangeDetection>();
      escapeRegionGo.AddComponent<SnapToTerrain>();

      RegionGameLogic regionGameLogic = escapeRegionGo.AddComponent<RegionGameLogic>();
      regionGameLogic.encounterObjectGuid = Guid.NewGuid().ToString();
      regionGameLogic.radius = REGION_RADIUS;
      regionGameLogic.regionDefId = "regionDef_EvacZone";
      // TODO: Add objective references

      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint1", new Vector3(0, 0, REGION_RADIUS));                   // North
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint2", new Vector3(REGION_RADIUS, 0, REGION_RADIUS / 2));   // North-East
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint3", new Vector3(REGION_RADIUS, 0, -REGION_RADIUS / 2));  // South-East
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint4", new Vector3(0, 0, -REGION_RADIUS));                  // South
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint5", new Vector3(-REGION_RADIUS, 0, -REGION_RADIUS / 2)); // South-West
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint6", new Vector3(-REGION_RADIUS, 0, REGION_RADIUS / 2));  // North-West

      return regionGameLogic;
    }
  }
}