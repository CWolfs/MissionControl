using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Utils;

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

    public static RegionGameLogic CreateEscapeRegion(GameObject parent, string regionGameLogicGuid, string objectiveGuid, string name = null) {
      GameObject escapeRegionGo = CreateEscapeRegionGameObject(parent, name);

      MeshCollider collider = escapeRegionGo.AddComponent<MeshCollider>();
      MeshFilter mf = escapeRegionGo.AddComponent<MeshFilter>();
      Mesh mesh = MeshTools.CreateHexigon(REGION_RADIUS);
      collider.sharedMesh = mesh;
      mf.mesh = mesh;

      escapeRegionGo.AddComponent<TerrainDataChangeDetection>();
      escapeRegionGo.AddComponent<SnapToTerrain>();
      escapeRegionGo.AddComponent<MeshRenderer>();

      RegionGameLogic regionGameLogic = escapeRegionGo.AddComponent<RegionGameLogic>();
      regionGameLogic.encounterObjectGuid = regionGameLogicGuid;
      regionGameLogic.radius = REGION_RADIUS;
      regionGameLogic.regionDefId = "regionDef_EvacZone";
      regionGameLogic.alwaysShowRegionWhenActive = true;

      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint1", new Vector3(0, 0, REGION_RADIUS));                       // North
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint2", new Vector3(REGION_RADIUS, 0, REGION_RADIUS / 2f));      // North-East
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint3", new Vector3(REGION_RADIUS, 0, -(REGION_RADIUS / 2f)));   // South-East
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint4", new Vector3(0, 0, -REGION_RADIUS));                      // South
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint5", new Vector3(-REGION_RADIUS, 0, -(REGION_RADIUS / 2f)));  // South-West
      CreateRegionPointGameObject(escapeRegionGo, $"RegionPoint6", new Vector3(-REGION_RADIUS, 0, REGION_RADIUS / 2f));     // North-West

      return regionGameLogic;
    }
  }
}