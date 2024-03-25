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
      gameObject.transform.parent = parent.transform;
      gameObject.transform.localPosition = Vector3.zero;

      return gameObject;
    }

    public GameObject CreateStructure(string name, GameObject parent) {
      this.structureName = name;
      structureParentGO = CreateGameObject(parent, name);

      CreateStructureGroup(structureParentGO, $"StructureGroup_{structureName}");
      structureParentGO.AddComponent<FacilityParent>();

      if (structureParentGO != null) {
        structureParentGO.transform.localScale = PropStructureDef.GetPropModelDef().Scale.Value;
      }

      return structureParentGO;
    }

    private GameObject CreateStructureGroup(GameObject facilityGO, string name) {
      structureGroupGO = CreateGameObject(facilityGO, name);

      CreateStructure(structureGroupGO, $"Structure_{structureName}");

      SnapToTerrain snapToTerrain = structureGroupGO.AddComponent<SnapToTerrain>();

      ObstructionGameLogic obstructionGameLogic = structureGroupGO.AddComponent<ObstructionGameLogic>();
      obstructionGameLogic.buildingDefId = ObstructionGameLogic.buildingDef_SolidObstruction;
      obstructionGameLogic.teamDefinitionGuid = TeamUtils.WORLD_TEAM_ID;

      string obstructionGuid = Guid.NewGuid().ToString();
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

    private GameObject CreateFlimsies(GameObject structuregGO) {
      List<PropDestructibleFlimsyDef> flimsyModels = PropStructureDef.DestructibleFlimsyModels;

      if (flimsyModels.Count > 0) {
        GameObject flimsyParentGO = CreateGameObject(structuregGO, "_flimsy");
        flimsyParentGO.transform.rotation = Quaternion.identity;

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

      AttachFlimsyMesh(flimsyGO, propFlimsyDef);
      CalculateBounds(mf, boxCollider);

      flimsyGO.transform.localPosition = propFlimsyDef.Position;
      flimsyGO.transform.localEulerAngles = propFlimsyDef.Rotation;

      flimsyGO.SetActive(true);
    }

    private void AttachFlimsyMesh(GameObject flimsyGO, PropDestructibleFlimsyDef flimsyDef) {
      PropModelDef propModelDef = flimsyDef.GetPropModelDef();
      Mesh flimsyLOD0Mesh = null;

      if (propModelDef.IsMeshInBundle) {
        LoadAssetBundle(propModelDef);

        flimsyLOD0Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_LOD0");

        if (flimsyLOD0Mesh == null) flimsyLOD0Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}");

        if (flimsyLOD0Mesh == null) {
          Main.Logger.LogError("[StructureFactory.AttachFlimsyMesh] Bundle LOD Mesh is null. It's possible LOD0, LOD1 and LOD2 might also be null. Check the names of the Meshes in the bundle match the Mesh in the MC PropModelDef");
        } else {
          Main.Logger.Log("[StructureFactory.AttachFlimsyMesh] Bundle LOD Mesh is " + flimsyLOD0Mesh.name);
        }
      } else {
        foreach (Mesh mesh in allGameMeshes) {
          // If a flimsy base (e.g. no COL, LOD0, LOD1, LOD2) then set them all to the flimsy mesh
          if (mesh.name == propModelDef.MeshName) {
            Main.Logger.Log($"[StructureFactory.AttachFlimsyMesh] Found a flimsy base for '{propModelDef.Key}' so using that for COL, LOD0, LOD1, LOD2");

            // If enabled, recenter the mesh pivot as filmsy pivots are often all over the place
            Mesh flimsyFormattedMesh = mesh;
            if (propModelDef.ChangePivotToCenterIfFlimsyMeshFormat) {
              flimsyFormattedMesh = CenterMeshPivot(mesh);
            }

            flimsyLOD0Mesh = flimsyFormattedMesh;
            break;
          }

          if (mesh.name == $"{propModelDef.MeshName}_LOD0") flimsyLOD0Mesh = mesh;
        }
      }
      MeshFilter flimsyLOD0MF = flimsyGO.GetComponent<MeshFilter>();
      MeshRenderer flimsyLOD0MR = flimsyGO.GetComponent<MeshRenderer>();

      Material[] materials = BuildMaterialsForRenderer(flimsyLOD0Mesh, propModelDef, propModelDef.Materials, placeholderMaterial);
      flimsyLOD0MR.materials = materials;
      flimsyLOD0MF.mesh = flimsyLOD0Mesh;
    }
  }
}