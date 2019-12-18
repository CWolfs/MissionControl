using UnityEngine;

using System;

using BattleTech;

using MissionControl.Utils;

namespace MissionControl.EncounterFactories {
  public class RegionFactory {
    private static float REGION_RADIUS = 70.71068f;

    private static GameObject CreateWithdrawRegionGameObject(GameObject parent, string name = null) {
      GameObject escapeRegionGo = new GameObject((name == null) ? "Region_Withdraw" : name);
      escapeRegionGo.transform.parent = parent.transform;
      escapeRegionGo.transform.localPosition = Vector3.zero;

      return escapeRegionGo;
    }

    private static RegionPointGameLogic CreateRegionPointGameObject(GameObject parent, string name, Vector3 localPosition) {
      GameObject regionPointGo = new GameObject((name == null) ? "RegionPoint" : name);
      regionPointGo.transform.parent = parent.transform;
      regionPointGo.transform.localPosition = localPosition;

      RegionPointGameLogic regionPoint = regionPointGo.AddComponent<RegionPointGameLogic>();
      regionPoint.encounterObjectGuid = Guid.NewGuid().ToString();

      return regionPoint;
    }

    public static RegionGameLogic CreateWithdrawRegion(GameObject parent, string regionGameLogicGuid, string objectiveGuid, string name = null) {
      GameObject withdrawRegionGo = CreateWithdrawRegionGameObject(parent, name);

      MeshCollider collider = withdrawRegionGo.AddComponent<MeshCollider>();
      MeshFilter mf = withdrawRegionGo.AddComponent<MeshFilter>();
      Mesh mesh = MeshTools.CreateHexigon(REGION_RADIUS);
      collider.sharedMesh = mesh;
      mf.mesh = mesh;

      withdrawRegionGo.AddComponent<TerrainDataChangeDetection>();
      withdrawRegionGo.AddComponent<SnapToTerrain>();
      withdrawRegionGo.AddComponent<MeshRenderer>();

      RegionGameLogic regionGameLogic = withdrawRegionGo.AddComponent<RegionGameLogic>();
      regionGameLogic.encounterObjectGuid = regionGameLogicGuid;
      regionGameLogic.radius = REGION_RADIUS;
      regionGameLogic.regionDefId = "regionDef_EvacZone";
      regionGameLogic.alwaysShowRegionWhenActive = true;

      CreateRegionPointGameObject(withdrawRegionGo, $"RegionPoint1", new Vector3(0, 0, REGION_RADIUS));                       // North
      CreateRegionPointGameObject(withdrawRegionGo, $"RegionPoint2", new Vector3(REGION_RADIUS, 0, REGION_RADIUS / 2f));      // North-East
      CreateRegionPointGameObject(withdrawRegionGo, $"RegionPoint3", new Vector3(REGION_RADIUS, 0, -(REGION_RADIUS / 2f)));   // South-East
      CreateRegionPointGameObject(withdrawRegionGo, $"RegionPoint4", new Vector3(0, 0, -REGION_RADIUS));                      // South
      CreateRegionPointGameObject(withdrawRegionGo, $"RegionPoint5", new Vector3(-REGION_RADIUS, 0, -(REGION_RADIUS / 2f)));  // South-West
      CreateRegionPointGameObject(withdrawRegionGo, $"RegionPoint6", new Vector3(-REGION_RADIUS, 0, REGION_RADIUS / 2f));     // North-West

      return regionGameLogic;
    }
  }
}