using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;
using BattleTech.Rendering.UI;
using BattleTech.Framework;

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

      CreateBuildingGroup(facilityGO, "BuildingGroup_MyTower");

      facilityGO.AddComponent<FacilityParent>();

      return facilityGO;
    }

    private static GameObject CreateBuildingGroup(GameObject facilityGO, string name) {
      GameObject buildingGroupGO = CreateGameObject(facilityGO, name);

      CreateBuilding(buildingGroupGO, "Building_MyTower");

      SnapToTerrain snapToTerrain = buildingGroupGO.AddComponent<SnapToTerrain>();

      BuildingRepresentation buildingRepresentation = buildingGroupGO.AddComponent<BuildingRepresentation>();

      ObstructionGameLogic obstructionGameLogic = buildingGroupGO.AddComponent<ObstructionGameLogic>();
      obstructionGameLogic.buildingDefId = "buildingdef_Military_Large"; //  ObstructionGameLogic.buildingDef_SolidObstruction;
      // obstructionGameLogic.overrideBuildingName // keep here as a reminder they exist if needed
      // obstructionGameLogic.overrideStructurePoints // keep here as a reminder they exist if needed
      obstructionGameLogic.teamDefinitionGuid = TeamUtils.TARGET_TEAM_ID;

      // Health is set by buildingDefId, or a manual override 'overrideStructurePoints' on Building set via ObstructionGameLogic
      DestructibleObjectGroup destructibleObjectGroup = buildingGroupGO.AddComponent<DestructibleObjectGroup>();
      destructibleObjectGroup.BakeDestructionAssets();

      snapToTerrain.ForceCalculateCurrentHeightOffset();

      return buildingGroupGO;
    }

    private static GameObject CreateBuilding(GameObject buildingGroupGO, string name) {
      GameObject buildingGO = CreateGameObject(buildingGroupGO, name);

      CreateColAndLODs(buildingGO);

      DestructibleObject destructibleObject = buildingGO.AddComponent<DestructibleObject>();
      destructibleObject.destructType = DestructibleObject.DestructType.targetStruct;
      destructibleObject.structSize = DestructibleObject.DestructibleSize.large;
      destructibleObject.structMaterial = DestructibleObject.DestructibleMaterial.metal;
      destructibleObject.flimsyDestructType = FlimsyDestructType.largeMetal;

      LODGroup lodGroup = buildingGO.AddComponent<LODGroup>();
      lodGroup.animateCrossFading = true;
      lodGroup.fadeMode = LODFadeMode.CrossFade;
      LOD[] lods = new LOD[1]; // Numer of LODS

      // Setup LOD0 
      MeshRenderer lod0MR = buildingGO.transform.Find($"{buildingGO.name}_LOD0").GetComponent<MeshRenderer>();
      Renderer[] lod0Renderers = new Renderer[1];
      lod0Renderers[0] = lod0MR;
      lods[0] = new LOD(0, lod0Renderers);

      // Set LODs
      lodGroup.SetLODs(lods);
      lodGroup.RecalculateBounds();

      StructureGroup structureGroup = buildingGO.AddComponent<StructureGroup>();
      structureGroup.lodGroup = lodGroup;
      structureGroup.structureGroupRenderers = buildingGO.GetComponentsInChildren<MeshRenderer>();
      structureGroup.destructibleObject = destructibleObject;
      structureGroup.destructionParent = buildingGO;

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

      Material placeholderMaterial = new Material(Shader.Find("BattleTech Standard"));

      GameObject buildingCOLGO = CreateGameObject(buildingGO, $"{buildingGO.name}_COL");
      MeshCollider buildingCOLCollider = buildingCOLGO.AddComponent<MeshCollider>();
      buildingCOLCollider.sharedMesh = buildingCOLMesh;
      buildingCOLGO.layer = 12; // so raycasts can hit it for highlight effect and selection

      GameObject buildingLOD0GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD0");
      MeshFilter buildingLOD0MF = buildingLOD0GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD0MR = buildingLOD0GO.AddComponent<MeshRenderer>();
      buildingLOD0MF.mesh = buildingLOD0Mesh;
      buildingLOD0MR.material = placeholderMaterial;

      // GameObject buildingLOD1GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD1");
      // MeshFilter buildingLOD1MF = buildingLOD1GO.AddComponent<MeshFilter>();
      // MeshRenderer buildingLOD1MR = buildingLOD1GO.AddComponent<MeshRenderer>();
      // buildingLOD1MF.mesh = buildingLOD1Mesh;

      // GameObject buildingLOD2GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD2");
      // MeshFilter buildingLOD2MF = buildingLOD2GO.AddComponent<MeshFilter>();
      // MeshRenderer buildingLOD2MR = buildingLOD2GO.AddComponent<MeshRenderer>();
      // buildingLOD2MF.mesh = buildingLOD2Mesh;
    }
  }
}