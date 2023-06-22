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

    private static void RotateVector3(ref Vector3 point, float theta) {
      float x = point.x;
      float z = point.z;

      point.x = x * Mathf.Cos(theta) + z * Mathf.Sin(theta);
      point.z = -x * Mathf.Sin(theta) + z * Mathf.Cos(theta);
    }

    public static RegionGameLogic CreateRegion(GameObject parent, string regionGameLogicGuid, string objectiveGuid, string name, string regionDefId, float radius = 0, bool showRegionHexWhenActive = true, bool alwaysShowRegionWhenActive = false, bool showPreviewOfRegion = false) {
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

      // Theta is -30 degrees converted to radians
      float theta = -30f * Mathf.Deg2Rad;
      Vector3 armPoint1 = new Vector3(0, 0, regionRadius);                      // North
      Vector3 armPoint2 = new Vector3(regionRadius, 0, regionRadius / 2f);      // NorthEast
      Vector3 armPoint3 = new Vector3(regionRadius, 0, -(regionRadius / 2f));   // SouthEast
      Vector3 armPoint4 = new Vector3(0, 0, -regionRadius);                     // South
      Vector3 armPoint5 = new Vector3(-regionRadius, 0, -(regionRadius / 2f));  // SouthWest
      Vector3 armPoint6 = new Vector3(-regionRadius, 0, regionRadius / 2f);     // NorthWest

      RotateVector3(ref armPoint1, theta);
      RotateVector3(ref armPoint2, theta);
      RotateVector3(ref armPoint3, theta);
      RotateVector3(ref armPoint4, theta);
      RotateVector3(ref armPoint5, theta);
      RotateVector3(ref armPoint6, theta);

      CreateRegionPointGameObject(regionGo, $"RegionPoint1", armPoint1);  // North
      CreateRegionPointGameObject(regionGo, $"RegionPoint2", armPoint2);  // North-East
      CreateRegionPointGameObject(regionGo, $"RegionPoint3", armPoint3);  // South-East
      CreateRegionPointGameObject(regionGo, $"RegionPoint4", armPoint4);  // South
      CreateRegionPointGameObject(regionGo, $"RegionPoint5", armPoint5);  // South-West
      CreateRegionPointGameObject(regionGo, $"RegionPoint6", armPoint6);  // North-West

      return regionGameLogic;
    }
  }
}