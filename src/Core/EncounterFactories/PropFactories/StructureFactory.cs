using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Data;

namespace MissionControl.EncounterFactories {
  public class StructureFactory : PropFactory {
    private string structureName = "UNNAMED";

    private PropStructureDef PropStructureDef { get; set; }

    private GameObject structureParentGO;
    private GameObject structureGroupGO;
    private GameObject structureGO;

    // CameraFade variables
    private LODGroup fadeLODGroup;
    private StructureGroup fadeStructureGroup;

    public StructureFactory(PropStructureDef propStructureDef) {
      PropStructureDef = propStructureDef;
    }

    private GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.SetParent(parent.transform, false);

      return gameObject;
    }

    public GameObject CreateStructure(string name, GameObject parent, string guid, List<string> tags, string teamGUID = null) {
      this.structureName = name;
      structureParentGO = CreateGameObject(parent, name);

      CreateStructureGroup(structureParentGO, $"StructureGroup_{structureName}", guid, tags, teamGUID);
      structureParentGO.AddComponent<FacilityParent>();

      return structureParentGO;
    }

    private GameObject CreateStructureGroup(GameObject facilityGO, string name, string guid, List<string> tags, string teamGUID) {
      structureGroupGO = CreateGameObject(facilityGO, name);

      CreateStructure(structureGroupGO, $"Structure_{structureName}");

      SnapToTerrain snapToTerrain = structureGroupGO.AddComponent<SnapToTerrain>();

      ObstructionGameLogic obstructionGameLogic = structureGroupGO.AddComponent<ObstructionGameLogic>();
      obstructionGameLogic.buildingDefId = ObstructionGameLogic.buildingDef_SolidObstruction;
      obstructionGameLogic.teamDefinitionGuid = teamGUID == null ? TeamUtils.WORLD_TEAM_ID : teamGUID;

      obstructionGameLogic.encounterTags.AddRange(tags);

      string obstructionGuid = guid ?? Guid.NewGuid().ToString();
      obstructionGameLogic.encounterObjectGuid = obstructionGuid;

      // Track the custom buildings to bypass the min 8 cell hit count for buildings to be added to the proper building list of a MapEncounterLayerDataCell
      MissionControl.Instance.CustomBuildingGuids.Add(obstructionGuid);

      snapToTerrain.ForceCalculateCurrentHeightOffset();

      return structureGroupGO;
    }

    private GameObject CreateStructure(GameObject buildingGroupGO, string name) {
      PropModelDef propModelDef = PropStructureDef.GetPropModelDef();

      structureGO = CreateGameObject(buildingGroupGO, name);
      structureGO.SetActive(false);

      CreateColAndLODs(structureGO, propModelDef);
      CreatePropLights(structureGO, propModelDef);
      GameObject flimsyParentGO = CreateFlimsies(structureGO);

      // Setup LOD Group
      LODGroup lodGroup = SetupLODGroup(structureGO);

      StructureGroup structureGroup = structureGO.AddComponent<StructureGroup>();
      structureGroup.lodGroup = lodGroup;
      structureGroup.structureGroupRenderers = structureGO.GetComponentsInChildren<MeshRenderer>();
      // structureGroup.destructibleObject = destructibleObject;
      structureGroup.destructionParent = structureGO;

      structureGO.transform.localScale = propModelDef.Scale.Value; // Sets the Model's scale
      structureGO.SetActive(true);

      // Cache for adding to Camera Fade Manager
      fadeLODGroup = lodGroup;
      fadeStructureGroup = structureGroup;

      return structureGO;
    }

    // Add to CameraFadeManager to allow for fading the building when the camera is too close
    // public void AddToCameraFadeGroup() {
    //   CameraFadeManager instance = CameraFadeManager.Instance;
    //   Renderer[] structureGroupRenderers = fadeStructureGroup.structureGroupRenderers;
    //   CameraFadeManager.FadeGroup fadeGroup = instance.Add(fadeLODGroup, structureGroupRenderers);

    //   fadeStructureGroup.destructibleObject.AddChildrenToFadeManager(fadeGroup);
    // }

    // private GameObject CreateDecals(GameObject structureGO) {

    // }

    private GameObject CreateFlimsies(GameObject structuregGO) {
      List<PropDestructibleFlimsyDef> flimsyModels = PropStructureDef.DestructibleFlimsyModels;

      if (flimsyModels.Count > 0) {
        GameObject flimsyParentGO = CreateGameObject(structuregGO, "_flimsy");
        flimsyParentGO.transform.localRotation = Quaternion.identity;

        foreach (PropDestructibleFlimsyDef propFlimsyModel in flimsyModels) {
          CreateFlimsy(flimsyParentGO, propFlimsyModel);
        }

        return flimsyParentGO;
      }

      return null;
    }

    private void CreateFlimsy(GameObject flimsyParentGO, PropDestructibleFlimsyDef propFlimsyDef) {
      PropModelDef propModelDef = propFlimsyDef.GetPropModelDef();

      Main.Logger.Log("[StructureFactory.CreateFlimsy] About to create flimsy " + propFlimsyDef.Key);
      GameObject flimsyGO = CreateGameObject(flimsyParentGO, propFlimsyDef.Key);
      flimsyGO.SetActive(false);

      MeshFilter mf = flimsyGO.AddComponent<MeshFilter>();
      flimsyGO.AddComponent<MeshRenderer>();

      BoxCollider boxCollider = flimsyGO.AddComponent<BoxCollider>();

      if (!AttachFlimsyMesh(flimsyGO, propFlimsyDef)) {
        flimsyGO.transform.SetParent(null, false);
        MonoBehaviour.Destroy(flimsyGO);
        return;
      }
      CalculateBounds(mf, boxCollider);

      flimsyGO.transform.localPosition = propFlimsyDef.Position;
      flimsyGO.transform.localEulerAngles = propFlimsyDef.Rotation;

      if (propFlimsyDef.AllowModelMeshOffsets && propModelDef.MeshOffsets.ContainsKey("LOD0")) {
        flimsyGO.transform.localPosition += propModelDef.MeshOffsets["LOD0"];
      }

      flimsyGO.SetActive(true);
    }

    private bool AttachFlimsyMesh(GameObject flimsyGO, PropDestructibleFlimsyDef flimsyDef) {
      PropModelDef propModelDef = flimsyDef.GetPropModelDef();
      Mesh flimsyLOD0Mesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_LOD0", "LOD0", isFlimsyBase: false);
      if (flimsyLOD0Mesh == null) {
        flimsyLOD0Mesh = ResolveMesh(propModelDef, propModelDef.MeshName, "Flimsy", isFlimsyBase: true);
      }

      if (flimsyLOD0Mesh == null) {
        Main.Logger.LogError($"[StructureFactory.AttachFlimsyMesh] Could not resolve flimsy mesh '{propModelDef.MeshName}' for '{propModelDef.Key}'");
        return false;
      }

      MeshFilter flimsyLOD0MF = flimsyGO.GetComponent<MeshFilter>();
      MeshRenderer flimsyLOD0MR = flimsyGO.GetComponent<MeshRenderer>();

      Material[] materials = BuildMaterialsForRenderer(flimsyLOD0Mesh, propModelDef, propModelDef.Materials, placeholderMaterial);
      flimsyLOD0MR.materials = materials;
      flimsyLOD0MF.mesh = flimsyLOD0Mesh;
      return true;
    }
  }
}