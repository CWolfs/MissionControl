using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering.MechCustomization;

using MissionControl.Data;
using MissionControl.Utils;
using BattleTech.Assetbundles;

namespace MissionControl.EncounterFactories {
  public class DropshipFactory : PropFactory {
    private PropDropshipDef PropDropshipDef { get; set; }
    private String CustomName { get; set; }
    private int CustomStructurePoints { get; set; }
    private DropshipAnimationState StartingState { get; set; }
    private string TeamGUID { get; set; }

    private GameObject dropshipGO;

    private GameObject destructSplit;
    private GameObject destructShell;

    public DropshipFactory(PropDropshipDef propDropshipDef, string customName, int customStructurePoints, DropshipAnimationState startingState, string teamGUID) {
      PropDropshipDef = propDropshipDef;
      CustomName = customName;
      CustomStructurePoints = customStructurePoints;
      StartingState = startingState;
      TeamGUID = teamGUID;
    }

    private GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.parent = parent.transform;
      gameObject.transform.localPosition = Vector3.zero;

      return gameObject;
    }

    public GameObject CreateDropship(GameObject parentGO, string name) {
      dropshipGO = CreateGameObject(parentGO, name);
      PropModelDef propModelDef = PropDropshipDef.GetPropModelDef();

      if (string.IsNullOrEmpty(propModelDef.CompleteBundleName)) {
        CreateCustomDropship(dropshipGO);
      } else {
        CreateVanillaDropship(dropshipGO);
      }

      return dropshipGO;
    }

    // TODO: Finish this so it works for custom models/meshes/bundles
    private GameObject CreateCustomDropship(GameObject parent) {
      BuildingRepresentation buildingRepresentation = dropshipGO.AddComponent<BuildingRepresentation>();

      DropshipGameLogic dropshipGameLogic = dropshipGO.AddComponent<DropshipGameLogic>();
      dropshipGameLogic.buildingDefId = PropDropshipDef.BuildingDefID; // Supports direct BuildingDefIds (e.g. buildingdef_Military_Large) or general values (e.g. ObstructionGameLogic.buildingDef_SolidObstruction)
      dropshipGameLogic.teamDefinitionGuid = TeamUtils.WORLD_TEAM_ID;
      dropshipGameLogic.currentAnimationState = StartingState;

      string obstructionGuid = Guid.NewGuid().ToString();
      dropshipGameLogic.encounterObjectGuid = obstructionGuid;

      // Track the custom buildings to bypass the min 8 cell hit count for buildings to be added to the proper building list of a MapEncounterLayerDataCell
      MissionControl.Instance.CustomBuildingGuids.Add(obstructionGuid);

      dropshipGameLogic.overrideBuildingName = CustomName;
      dropshipGameLogic.overrideStructurePoints = CustomStructurePoints;

      // Health is set by buildingDefId, or a manual override 'overrideStructurePoints' on Building set via ObstructionGameLogic
      DestructibleObjectGroup destructibleObjectGroup = dropshipGO.AddComponent<DestructibleObjectGroup>();
      destructibleObjectGroup.BakeDestructionAssets();

      dropshipGO.AddComponent<Animator>();
      dropshipGO.AddComponent<MechCustomization>();
      dropshipGO.AddComponent<MeshCollider>();

      return dropshipGO;
    }

    private void CreateVanillaDropship(GameObject parent) {
      Main.Logger.Log("[DropshipFactory.CreateVanillaDropship] Creating vanilla dropship " + PropDropshipDef.Key);
      PropModelDef propModelDef = PropDropshipDef.GetPropModelDef();
      string completeBundleName = propModelDef.CompleteBundleName;

      GameObject prefab = DataManager.Instance.SynchronouslyLoadPrefab(parent, completeBundleName);

      if (prefab != null) {
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localEulerAngles = Vector3.zero;
        DropshipGameLogic dropshipGameLogic = prefab.GetComponentInChildren<DropshipGameLogic>();
        dropshipGameLogic.currentAnimationState = DropshipAnimationState.Landed;

        dropshipGameLogic.encounterObjectGuid = Guid.NewGuid().ToString();
        MissionControl.Instance.CustomDropshipsGuids.Add(dropshipGameLogic.GUID);

        if (TeamGUID != null) {
          Main.Logger.Log("[DropshipFactory.CreateVanillaDropship] Adding to team " + TeamGUID);
          dropshipGameLogic.teamDefinitionGuid = TeamGUID;
        }
      } else {
        Main.Logger.LogError("[DropshipFactory.CreateVanillaDropship] Prefab is null");
      }
    }

    // public GameObject CreateBuilding(GameObject buildingGroupGO, string name, DestructibleObject.DestructType destructTypeOverride) {
    //   PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

    //   dropshipGO = CreateGameObject(buildingGroupGO, name);
    //   dropshipGO.SetActive(false);

    //   CreateColAndLODs(dropshipGO, propModelDef);
    //   GameObject flimsyParentGO = CreateFlimsies(buildingGroupGO);
    //   GameObject glassParentGO = CreateGlass(buildingGroupGO);
    //   CreateGenericStaticDestruct(dropshipGO);

    //   DestructibleObject destructibleObject = dropshipGO.AddComponent<DestructibleObject>();
    //   destructibleObject.destructType = destructTypeOverride;

    //   if (destructTypeOverride == DestructibleObject.DestructType.flimsyStruct) {
    //     destructibleObject.isFlimsy = true;
    //     destructibleObject.instantKillFlimsy = true;
    //     destructibleObject.isDependentFlimsy = true;
    //   }

    //   destructibleObject.structSize = propModelDef.DestructibleSize;
    //   destructibleObject.structMaterial = propModelDef.DestructibleMaterial;
    //   destructibleObject.flimsyDestructType = propModelDef.FlimsyDestructibleType;
    //   destructibleObject.dependentPersistentFX = new List<GameObject>();
    //   destructibleObject.damagedInstance = destructSplit.GetComponent<PhysicsExplodeChildren>();
    //   destructibleObject.embeddedFlimsyChildren = new List<DestructibleObject>();
    //   destructibleObject.shellInstance = destructShell;
    //   destructibleObject.damageAssetGroup = allDamageAssetGroups[UnityEngine.Random.Range(0, allDamageAssetGroups.Length)];
    //   destructibleObject.decalObjects = new List<GameObject>();
    //   destructibleObject.decalSpawners = new List<BTDecalSpawner>();
    //   destructibleObject.flimsyChild = flimsyParentGO;
    //   destructibleObject.glassChild = glassParentGO;
    //   destructibleObject.dependentPersistentFX = new List<GameObject>();

    //   // Setup LOD Group
    //   LODGroup lodGroup = SetupLODGroup(dropshipGO);

    //   StructureGroup structureGroup = dropshipGO.AddComponent<StructureGroup>();
    //   structureGroup.lodGroup = lodGroup;
    //   structureGroup.structureGroupRenderers = dropshipGO.GetComponentsInChildren<MeshRenderer>();
    //   structureGroup.destructibleObject = destructibleObject;
    //   structureGroup.destructionParent = dropshipGO;

    //   destructibleObject.structureGroup = structureGroup;
    //   destructibleObject.destructList = destructibleObject.GetComponentsInChildren<DestructibleObject>().ToList();

    //   dropshipGO.SetActive(true);

    //   destructibleObject.destructionParent.transform.SetParent(MCGenericStaticDestruct.transform, true);

    //   destructSplit.transform.SetParent(destructibleObject.destructionParent.transform, false);
    //   destructSplit.transform.localPosition = Vector3.zero;
    //   destructSplit.transform.localEulerAngles = Vector3.zero;
    //   destructShell.transform.SetParent(destructibleObject.destructionParent.transform, false);
    //   destructShell.transform.localPosition = Vector3.zero;
    //   destructShell.transform.localEulerAngles = Vector3.zero;

    //   if (glassGO != null) {
    //     glassGO.transform.SetParent(glassParentGO.transform, false);
    //     glassGO.transform.localPosition = PropBuildingDef.Glass != null ? PropBuildingDef.Glass.Position : Vector3.zero;
    //     glassGO.transform.localEulerAngles = PropBuildingDef.Glass != null ? PropBuildingDef.Glass.Rotation : Vector3.zero;
    //   }

    //   if (!propModelDef.HasCustomShell) RandomiseShell(destructibleObject);

    //   // Cache for adding to Camera Fade Manager
    //   fadeLODGroup = lodGroup;
    //   fadeStructureGroup = structureGroup;
    //   fadeDestructibleObject = destructibleObject;

    //   return dropshipGO;
    // }

    private GameObject CreateFlimsies(GameObject buildingGroupGO) {
      List<PropDestructibleFlimsyDef> flimsyModels = PropDropshipDef.DestructibleFlimsyModels;

      if (flimsyModels.Count > 0) {
        GameObject flimsyParentGO = CreateGameObject(buildingGroupGO, "_flimsy");
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

      Main.Logger.Log("[BuildingFactory.CreateFlimsy] About to create flimsy " + propFlimsyDef.Key);
      GameObject flimsyGO = CreateGameObject(flimsyParentGO, propFlimsyDef.Key);
      flimsyGO.SetActive(false);

      MeshFilter mf = flimsyGO.AddComponent<MeshFilter>();
      flimsyGO.AddComponent<MeshRenderer>();

      Rigidbody rb = flimsyGO.AddComponent<Rigidbody>();
      rb.mass = propFlimsyDef.Mass;

      BoxCollider boxCollider = flimsyGO.AddComponent<BoxCollider>();

      DestructibleObject destructibleObject = flimsyGO.AddComponent<DestructibleObject>();
      destructibleObject.isFlimsy = true;
      destructibleObject.embeddedFlimsy = true;
      destructibleObject.structSize = propModelDef.DestructibleSize;
      destructibleObject.flimsyDestructType = propModelDef.FlimsyDestructibleType;
      destructibleObject.structMaterial = propModelDef.DestructibleMaterial;
      destructibleObject.destructType = DestructibleObject.DestructType.flimsyChild;
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.pieceSize = DestructibleObject.PieceSize.small; // TODO: Expose this

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
          Main.Logger.LogError("[BuildingFactory.AttachFlimsyMesh] Bundle LOD Mesh is null. It's possible LOD0, LOD1 and LOD2 might also be null. Check the names of the Meshes in the bundle match the Mesh in the MC PropModelDef");
        } else {
          Main.Logger.Log("[BuildingFactory.AttachFlimsyMesh] Bundle LOD Mesh is " + flimsyLOD0Mesh.name);
        }

        if (flimsyLOD0Mesh.name == propModelDef.MeshName) {
          Main.Logger.Log($"[BuildingFactory.AttachFlimsyMesh] Found a flimsy base for '{propModelDef.Key}'");

          // If enabled, recenter the mesh pivot as filmsy pivots are often all over the place
          Mesh flimsyFormattedMesh = flimsyLOD0Mesh;
          if (propModelDef.ChangePivotToCenterIfFlimsyMeshFormat) {
            Main.Logger.Log($"[BuildingFactory.AttachFlimsyMesh] Found a flimsy base for '{propModelDef.Key}'");
            flimsyFormattedMesh = CenterMeshPivot(flimsyLOD0Mesh);
            flimsyLOD0Mesh = flimsyFormattedMesh;
          }
        }
      } else {
        foreach (Mesh mesh in allGameMeshes) {
          // If a flimsy base (e.g. no COL, LOD0, LOD1, LOD2) then set them all to the flimsy mesh
          if (mesh.name == propModelDef.MeshName) {
            Main.Logger.Log($"[BuildingFactory.AttachFlimsyMesh] Found a flimsy base for '{propModelDef.Key}'");

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

    private void CreateGenericStaticDestruct(GameObject buildingGO) {
      PropModelDef propModelDef = PropDropshipDef.GetPropModelDef();

      // SPLIT
      if (propModelDef.HasCustomSplits) {
        LoadAssetBundle(propModelDef);

        // Spawn the prefab split
        GameObject destructSplitPrefab = AssetBundleLoader.GetAsset<GameObject>(propModelDef.BundlePath, $"{propModelDef.MeshName}_{GenericStaticDestructName}_split");
        destructSplit = GameObject.Instantiate(destructSplitPrefab, Vector3.zero, new Quaternion(), buildingGO.transform);
        LayerTools.SetLayerRecursively(destructSplit, 8);

        foreach (GameObject splitPiece in destructSplit.transform) {
          // Apply Material
          // FIXME: This is a hack that reuses the materials of the original model and only assigns the first sequential materials from that original mesh into the splits
          // FIXME: This will result in some split pieces having the wrong material but it's only intended as a placeholder for now until I figure out what to do with this usecase
          // FIXME: A manual process won't work here as splits can go up to the hundreds so some kind of workflow is required that works for both:
          //  - user custom bundled
          //  - original vanilla extracted and bundled
          Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, propModelDef, PropDropshipDef.GetPropModelDef().Materials, placeholderMaterial);
          splitPiece.GetComponent<MeshRenderer>().materials = materials;
        }
      } else {
        // Split
        destructSplit = CreateGameObject(buildingGO, $"{buildingGO.name}_{GenericStaticDestructName}_split");
        destructSplit.layer = 8;

        List<GameObject> splitPieceGOs = MeshFracturer.Fracture(buildingGO, 1);
        foreach (GameObject splitPiece in splitPieceGOs) {
          splitPiece.transform.SetParent(destructSplit.transform);
          splitPiece.layer = 8;

          // Apply material
          // FIXME: The fracture UV mapping and material assignment need more work. Fewer mats should be assign per split (e.g. in vanilla a building with 7 mats might have splits with 2-3 mats depending on how they were fractured )
          // FIXME: So we don't need to assign all 7 original ones to each split. Probably need a 3rd party lib or definitely a better script for fracturing.
          // FIXME: Having issues making good runtime fractures - just use a copy of LOD0 for now
          Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, propModelDef, PropDropshipDef.GetPropModelDef().Materials, placeholderMaterial);
          splitPiece.GetComponent<MeshRenderer>().materials = materials;
        }
      }

      destructSplit.AddComponent<PhysicsExplodeChildren>();
      destructSplit.SetActive(false);

      // SHELL
      destructShell = CreateGameObject(buildingGO, $"{buildingGO.name}_{GenericStaticDestructName}_shell");
      destructShell.layer = 8;
      destructShell.SetActive(false);

      if (propModelDef.HasCustomShell) {
        Main.Logger.Log("[BuildingFactory.CreateGenericStaticDestruct] CustomShell has been set for " + PropDropshipDef.Key);

        // Load from bundle
        LoadAssetBundle(propModelDef);

        Mesh shellMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_shell");
        Mesh shellCOLMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_shell_COL");

        GameObject shellGO = new GameObject(shellMesh.name);
        shellGO.transform.SetParent(destructShell.transform);
        shellGO.transform.localPosition = Vector3.zero;

        MeshFilter mf = shellGO.AddComponent<MeshFilter>();
        mf.sharedMesh = shellMesh;

        MeshRenderer mr = shellGO.AddComponent<MeshRenderer>();

        if (shellCOLMesh != null) {
          MeshCollider shellCollider = shellGO.AddComponent<MeshCollider>();
          shellCollider.sharedMesh = shellCOLMesh;
        }

        if (propModelDef.CustomShellMaterials.Count > 0) {
          Main.Logger.Log("[BuildingFactory.CreateGenericStaticDestruct] Shell Materials are specified");

          Material[] materials = BuildMaterialsForRenderer(shellMesh, propModelDef, propModelDef.CustomShellMaterials, placeholderMaterial);
          mr.materials = materials;
        } else {
          SetShellMaterials(shellGO);
        }
      } else {
        // Dynamically create basic shell debris
        string shellPrefabName = "small_civilian_building_shell";
        GameObject randomShellPrefab = AssetBundleLoader.GetAsset<GameObject>("common-assets-bundle", shellPrefabName);
        if (randomShellPrefab == null) Main.Logger.LogError($"[CreateGenericStaticDestruct] Shell prefab '{shellPrefabName}' is null");

        shellPrefabName = "small_civilian_building_walls_shell";
        GameObject wallShellPrefab = AssetBundleLoader.GetAsset<GameObject>("common-assets-bundle", shellPrefabName);
        if (wallShellPrefab == null) Main.Logger.LogError($"[CreateGenericStaticDestruct] Shell prefab '{shellPrefabName}' is null");

        int shellDebrisCount = 0;

        switch (propModelDef.DestructibleSize) {
          case DestructibleObject.DestructibleSize.small: shellDebrisCount = 1; break;
          case DestructibleObject.DestructibleSize.medium: shellDebrisCount = 2; break;
          case DestructibleObject.DestructibleSize.large: shellDebrisCount = 3; break;
          case DestructibleObject.DestructibleSize.huge: shellDebrisCount = 4; break;
        }

        for (int i = 0; i < shellDebrisCount; i++) {
          GameObject randomShellDebris = GameObject.Instantiate(randomShellPrefab, Vector3.zero, new Quaternion(), destructShell.transform);
          randomShellDebris.name = randomShellDebris.name.Replace("(Clone)", "");
          SetShellMaterials(randomShellDebris);
        }

        GameObject wallShell = GameObject.Instantiate(wallShellPrefab, Vector3.zero, new Quaternion(), destructShell.transform);
        wallShell.name = wallShell.name.Replace("(Clone)", "");
        SetShellMaterials(wallShell);
      }
    }

    private void RandomiseShell(DestructibleObject linkedDestructibleObject) {
      float x = linkedDestructibleObject.footprint.x;
      float z = linkedDestructibleObject.footprint.z;

      foreach (Transform t in destructShell.transform) {
        t.localPosition = new Vector3(UnityEngine.Random.Range(-x, x), 3, UnityEngine.Random.Range(-z, z));

        if (!t.gameObject.name.Contains("wall")) {
          t.rotation = UnityEngine.Random.rotation;

          float scale = UnityEngine.Random.Range(0.5f, 1.5f);
          t.localScale = new Vector3(scale, 1, scale);
        }
      }
    }

    private void SetShellMaterials(GameObject shell) {
      PropModelDef propModelDef = PropDropshipDef.GetPropModelDef();

      // Setup materials
      Mesh mesh = shell.GetComponent<MeshFilter>().mesh;
      MeshRenderer mr = shell.GetComponent<MeshRenderer>();

      Material[] shellMaterials = BuildMaterialsForRenderer(mesh, propModelDef, propModelDef.Materials.Where(mat => !mat.Name.Contains("glass") && !mat.Name.Contains("lights") && !mat.Name.Contains("decals")).ToList(), null);

      Material[] shellMaterialToUse = new Material[mesh.subMeshCount];
      for (int i = 0; i < mesh.subMeshCount; i++) {
        shellMaterialToUse[i] = shellMaterials[UnityEngine.Random.Range(0, shellMaterials.Length)];
      }

      mr.materials = shellMaterialToUse;
    }
  }
}