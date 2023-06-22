using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;
using BattleTech.Rendering.UI;

using MissionControl.Utils;

namespace MissionControl.EncounterFactories {
  public class BuildingFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.parent = parent.transform;
      gameObject.transform.localPosition = Vector3.zero;

      return gameObject;
    }

    public static GameObject CreateFacility(GameObject parent, string name) {
      GameObject facilityGO = CreateGameObject(parent, name);
      facilityGO.AddComponent<FacilityParent>();

      CreateBuildingGroup(facilityGO, "BuildingGroup_MyTower");

      return facilityGO;
    }

    private static GameObject CreateBuildingGroup(GameObject facilityGO, string name) {
      GameObject buildingGroupGO = CreateGameObject(facilityGO, name);

      BuildingRepresentation buildingRepresentation = buildingGroupGO.AddComponent<BuildingRepresentation>();

      ObstructionGameLogic obstructionGameLogic = buildingGroupGO.AddComponent<ObstructionGameLogic>();
      obstructionGameLogic.buildingDefId = ObstructionGameLogic.buildingDef_SolidObstruction;

      DestructibleObject destructibleObject = buildingGroupGO.AddComponent<DestructibleObject>();
      destructibleObject.destructType = DestructibleObject.DestructType.targetStruct;

      SnapToTerrain snapToTerrain = buildingGroupGO.AddComponent<SnapToTerrain>();

      CreateBuilding(buildingGroupGO, "Building_MyTower");

      return buildingGroupGO;
    }

    private static GameObject CreateBuilding(GameObject buildingGroupGO, string name) {
      GameObject buildingGO = CreateGameObject(buildingGroupGO, name);

      LODGroup lOdGroup = buildingGO.AddComponent<LODGroup>();
      StructureGroup structureGroup = buildingGO.AddComponent<StructureGroup>();

      CreateColAndLODs(buildingGO);

      return buildingGO;
    }

    private static void CreateColAndLODs(GameObject buildingGO) {
      Mesh buildingCOLMesh = null;
      Mesh buildingLOD0Mesh = null;
      Mesh buildingLOD1Mesh = null;
      Mesh buildingLOD2Mesh = null;

      // TODO: Put this in a common area to call only once per map load
      Mesh[] allGameMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
      foreach (Mesh mesh in allGameMeshes) {
        if (mesh.name == "largeMilitaryBldgA_COL") buildingCOLMesh = mesh;
        if (mesh.name == "largeMilitaryBldgA_LOD0") buildingLOD0Mesh = mesh;
        if (mesh.name == "largeMilitaryBldgA_LOD1") buildingLOD1Mesh = mesh;
        if (mesh.name == "largeMilitaryBldgA_LOD2") buildingLOD2Mesh = mesh;
      }

      GameObject buildingCOLGO = CreateGameObject(buildingGO, $"{buildingGO.name}_COL");
      MeshCollider buildingCOLCollider = buildingCOLGO.AddComponent<MeshCollider>();
      buildingCOLCollider.sharedMesh = buildingCOLMesh;

      GameObject buildingLOD0GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD0");
      MeshFilter buildingLOD0MF = buildingLOD0GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD0MR = buildingLOD0GO.AddComponent<MeshRenderer>();

      buildingLOD0MF.mesh = buildingLOD0Mesh;

      GameObject buildingLOD1GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD1");
      MeshFilter buildingLOD1MF = buildingLOD1GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD1MR = buildingLOD1GO.AddComponent<MeshRenderer>();

      buildingLOD1MF.mesh = buildingLOD1Mesh;

      GameObject buildingLOD2GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD2");
      MeshFilter buildingLOD2MF = buildingLOD2GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD2MR = buildingLOD2GO.AddComponent<MeshRenderer>();

      buildingLOD2MF.mesh = buildingLOD2Mesh;
    }
  }
}