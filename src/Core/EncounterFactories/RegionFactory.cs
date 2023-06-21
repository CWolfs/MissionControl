using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Utils;

namespace MissionControl.EncounterFactories {
  public class RegionFactory {
    private static float DEFAULT_REGION_RADIUS = 70.71068f;

    private static GameObject CreateRegionGameObject(GameObject parent, string name = null) {
      GameObject escapeRegionGo = new GameObject((name == null) ? "Region" : name);
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

    public static RegionGameLogic CreateRegion(GameObject parent, string regionGameLogicGuid, string objectiveGuid, string name, string regionDefId, float radius = 0, bool showRegionHexWhenActive = true, bool alwaysShowRegionWhenActive = true, bool showPreviewOfRegion = false) {
      GameObject regionGo = CreateRegionGameObject(parent, name);
      float regionRadius = (radius > 0) ? radius : DEFAULT_REGION_RADIUS;

      MeshCollider collider = regionGo.AddComponent<MeshCollider>();
      MeshFilter mf = regionGo.AddComponent<MeshFilter>();
      Mesh mesh = MeshTools.CreateHexigon(regionRadius);
      collider.sharedMesh = mesh;
      mf.mesh = mesh;

      regionGo.AddComponent<TerrainDataChangeDetection>();
      regionGo.AddComponent<SnapToTerrain>();
      regionGo.AddComponent<MeshRenderer>();

      RegionGameLogic regionGameLogic = regionGo.AddComponent<RegionGameLogic>();

      if (showRegionHexWhenActive) regionGameLogic.regionVersion = RegionVersion.VersionTwo; // If disabled the region will not render the hex visuals

      regionGameLogic.encounterObjectGuid = regionGameLogicGuid;
      regionGameLogic.radius = regionRadius;
      regionGameLogic.regionDefId = regionDefId;
      regionGameLogic.SetDrawRegionDefIdDisplay(regionDefId);
      regionGameLogic.alwaysShowRegionWhenActive = alwaysShowRegionWhenActive;

      regionGameLogic.ShowPreviewOfRegion(showPreviewOfRegion); // This displays the region's 'Future Target' mouse over label if it's not an active region

      CreateRegionPointGameObject(regionGo, $"RegionPoint1", new Vector3(0, 0, regionRadius));                      // North
      CreateRegionPointGameObject(regionGo, $"RegionPoint2", new Vector3(regionRadius, 0, regionRadius / 2f));      // North-East
      CreateRegionPointGameObject(regionGo, $"RegionPoint3", new Vector3(regionRadius, 0, -(regionRadius / 2f)));   // South-East
      CreateRegionPointGameObject(regionGo, $"RegionPoint4", new Vector3(0, 0, -regionRadius));                     // South
      CreateRegionPointGameObject(regionGo, $"RegionPoint5", new Vector3(-regionRadius, 0, -(regionRadius / 2f)));  // South-West
      CreateRegionPointGameObject(regionGo, $"RegionPoint6", new Vector3(-regionRadius, 0, regionRadius / 2f));     // North-West

      return regionGameLogic;
    }
  }
}