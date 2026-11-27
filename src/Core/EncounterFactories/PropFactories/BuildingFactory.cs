using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;

using MissionControl.Data;
using MissionControl.Utils;

using BTAssetReader.Unity;

namespace MissionControl.EncounterFactories {
  public class BuildingFactory : PropFactory {
    private string facilityName = "UNNAMED";

    private PropBuildingDef PropBuildingDef { get; set; }
    private String GUID { get; set; }
    private List<string> Tags { get; set; } = new List<string>();
    private String CustomName { get; set; }
    private int CustomStructurePoints { get; set; }

    private GameObject facilityGO;
    private GameObject buildingGroupGO;
    private GameObject buildingGO;
    private GameObject glassParentGO;
    private GameObject glassGO;

    // private GameObject destructDecalParent;
    private GameObject destructSplit;
    private GameObject destructShell;
    private bool usedCustomOrAutoStumpShell = false;

    // CameraFade variables
    private LODGroup fadeLODGroup;
    private StructureGroup fadeStructureGroup;
    private DestructibleObject fadeDestructibleObject;

    public BuildingFactory(PropBuildingDef propBuildingDef, string guid, List<string> tags, string customName, int customStructurePoints) {
      PropBuildingDef = propBuildingDef;
      GUID = guid;
      Tags = tags;
      CustomName = customName;
      CustomStructurePoints = customStructurePoints;
    }

    private GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.SetParent(parent.transform, false);

      return gameObject;
    }

    public static void SyncDestructionParent(DestructibleObject destructibleObject) {
      if (destructibleObject == null || destructibleObject.destructionParent == null) return;

      Transform src = destructibleObject.transform;
      Transform dst = destructibleObject.destructionParent.transform;
      dst.position = src.position;
      dst.rotation = src.rotation;
      // Use ancestor/instance scale only - PropModelDef.Scale is already baked into footprint-based rubble/decals and applied directly to split/custom shell meshes
      dst.localScale = src.parent != null ? src.parent.lossyScale : Vector3.one;
    }

    public static void SyncAllDestructionParents(GameObject root) {
      if (root == null) return;

      DestructibleObject[] destructibles = root.GetComponentsInChildren<DestructibleObject>(true);
      foreach (DestructibleObject destructible in destructibles) {
        SyncDestructionParent(destructible);
      }
    }

    public GameObject CreateFacility(string name, GameObject parent, string teamGUID = null) {
      this.facilityName = name;
      facilityGO = CreateGameObject(parent, name);

      CreateBuildingGroup(facilityGO, $"BuildingGroup_{facilityName}", teamGUID);
      facilityGO.AddComponent<FacilityParent>();

      return facilityGO;
    }

    private GameObject CreateBuildingGroup(GameObject facilityGO, string name, string teamGUID) {
      buildingGroupGO = CreateGameObject(facilityGO, name);

      CreateBuilding(buildingGroupGO, $"Building_{facilityName}", DestructibleObject.DestructType.targetStruct);

      SnapToTerrain snapToTerrain = buildingGroupGO.AddComponent<SnapToTerrain>();
      BuildingRepresentation buildingRepresentation = buildingGroupGO.AddComponent<BuildingRepresentation>();

      ObstructionGameLogic obstructionGameLogic = buildingGroupGO.AddComponent<ObstructionGameLogic>();
      obstructionGameLogic.buildingDefId = PropBuildingDef.BuildingDefID; // Supports direct BuildingDefIds (e.g. buildingdef_Military_Large) or general values (e.g. ObstructionGameLogic.buildingDef_SolidObstruction)
      obstructionGameLogic.teamDefinitionGuid = teamGUID == null ? TeamUtils.WORLD_TEAM_ID : teamGUID;
      obstructionGameLogic.encounterTags.AddRange(Tags);

      string obstructionGuid = GUID ?? Guid.NewGuid().ToString();
      obstructionGameLogic.encounterObjectGuid = obstructionGuid;

      // Track the custom buildings to bypass the min 8 cell hit count for buildings to be added to the proper building list of a MapEncounterLayerDataCell
      MissionControl.Instance.CustomBuildingGuids.Add(obstructionGuid);

      obstructionGameLogic.overrideBuildingName = CustomName;
      obstructionGameLogic.overrideStructurePoints = CustomStructurePoints;

      // Health is set by buildingDefId, or a manual override 'overrideStructurePoints' on Building set via ObstructionGameLogic
      DestructibleObjectGroup destructibleObjectGroup = buildingGroupGO.AddComponent<DestructibleObjectGroup>();
      destructibleObjectGroup.BakeDestructionAssets();

      snapToTerrain.ForceCalculateCurrentHeightOffset();

      return buildingGroupGO;
    }

    public GameObject CreateBuilding(GameObject buildingGroupGO, string name, DestructibleObject.DestructType destructTypeOverride) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      buildingGO = CreateGameObject(buildingGroupGO, name);
      buildingGO.SetActive(false);

      CreateColAndLODs(buildingGO, propModelDef);
      CreatePropLights(buildingGO, propModelDef);
      GameObject flimsyParentGO = CreateFlimsies(buildingGroupGO);
      GameObject glassParentGO = CreateGlass(buildingGroupGO);
      GameObject decalParentGO = CreateDecals(buildingGroupGO);
      CreateGenericStaticDestruct(buildingGO);

      DestructibleObject destructibleObject = buildingGO.AddComponent<DestructibleObject>();
      destructibleObject.destructType = destructTypeOverride;

      if (destructTypeOverride == DestructibleObject.DestructType.flimsyStruct) {
        destructibleObject.isFlimsy = true;
        destructibleObject.instantKillFlimsy = true;
        destructibleObject.isDependentFlimsy = true;
      }

      destructibleObject.structSize = propModelDef.DestructibleSize;
      destructibleObject.structMaterial = propModelDef.DestructibleMaterial;
      destructibleObject.flimsyDestructType = propModelDef.FlimsyDestructibleType;
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.damagedInstance = destructSplit.GetComponent<PhysicsExplodeChildren>();
      destructibleObject.embeddedFlimsyChildren = new List<DestructibleObject>();
      destructibleObject.shellInstance = destructShell;
      destructibleObject.damageAssetGroup = GetRandomDamageAssetGroup();
      destructibleObject.decalObjects = new List<GameObject>();
      destructibleObject.decalSpawners = new List<BTDecalSpawner>();
      destructibleObject.flimsyChild = flimsyParentGO;
      destructibleObject.glassChild = glassParentGO;
      destructibleObject.dependentPersistentFX = new List<GameObject>();

      // Setup LOD Group
      LODGroup lodGroup = SetupLODGroup(buildingGO);

      StructureGroup structureGroup = buildingGO.AddComponent<StructureGroup>();
      structureGroup.lodGroup = lodGroup;
      structureGroup.structureGroupRenderers = buildingGO.GetComponentsInChildren<MeshRenderer>();
      structureGroup.destructibleObject = destructibleObject;
      structureGroup.destructionParent = buildingGO;

      destructibleObject.structureGroup = structureGroup;
      destructibleObject.destructList = destructibleObject.GetComponentsInChildren<DestructibleObject>().ToList();

      buildingGO.transform.localScale = propModelDef.Scale.Value; // Sets the Model's scale
      buildingGO.SetActive(true);

      destructibleObject.destructionParent.transform.SetParent(MCGenericStaticDestruct.transform, true);

      destructSplit.transform.SetParent(destructibleObject.destructionParent.transform, false);
      destructSplit.transform.localPosition = Vector3.zero;
      destructSplit.transform.localEulerAngles = Vector3.zero;
      destructSplit.transform.localScale = propModelDef.Scale.Value;
      destructShell.transform.SetParent(destructibleObject.destructionParent.transform, false);
      destructShell.transform.localPosition = Vector3.zero;
      destructShell.transform.localEulerAngles = Vector3.zero;
      if (usedCustomOrAutoStumpShell) {
        destructShell.transform.localScale = propModelDef.Scale.Value;
      }

      if (glassGO != null) {
        glassGO.transform.SetParent(glassParentGO.transform, false);
        glassGO.transform.localPosition = PropBuildingDef.Glass != null ? PropBuildingDef.Glass.Position : Vector3.zero;
        glassGO.transform.localEulerAngles = PropBuildingDef.Glass != null ? PropBuildingDef.Glass.Rotation : Vector3.zero;
      }

      if (!usedCustomOrAutoStumpShell) RandomiseShell(destructibleObject);

      // Cache for adding to Camera Fade Manager
      fadeLODGroup = lodGroup;
      fadeStructureGroup = structureGroup;
      fadeDestructibleObject = destructibleObject;

      return buildingGO;
    }

    // Add to CameraFadeManager to allow for fading the building when the camera is too close
    public void AddToCameraFadeGroup() {
      CameraFadeManager instance = CameraFadeManager.Instance;
      Renderer[] structureGroupRenderers = fadeStructureGroup.structureGroupRenderers;
      CameraFadeManager.FadeGroup fadeGroup = instance.Add(fadeLODGroup, structureGroupRenderers);

      fadeStructureGroup.destructibleObject.AddChildrenToFadeManager(fadeGroup);

      // TODO: Include this when adding glass support
      // if (structureGroup != null) {
      // for (int i = 0; i < structureGroup.flimsyChildren.Length; i++) {
      //   structureGroup.flimsyChildren[i].fadeGroup = fadeGroup;
      //   if (glassRenderer != null) {
      //     fadeGroup.AddRenderer(glassRenderer);
      //   }

      if (fadeDestructibleObject.damagedInstance != null) {
        fadeDestructibleObject.damagedInstance.UpdateSnapshots(forceUpdate: true);
        for (int j = 0; j < fadeDestructibleObject.damagedInstance.children.Length; j++) {
          fadeDestructibleObject.damagedInstance.children[j].fadeGroup = fadeGroup;
        }
      }
    }

    private GameObject CreateGlass(GameObject buildingGroupGO) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      buildingGlassMesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_glass", "Glass", isFlimsyBase: false);
      if (buildingGlassMesh == null) return null;

      Main.Logger.Log("[PropFactory.CreateGlass] Glass Mesh is " + buildingGlassMesh.name);

      glassParentGO = CreateGameObject(buildingGroupGO, "_glass");
      glassParentGO.transform.localRotation = Quaternion.identity;

      glassGO = CreateGameObject(glassParentGO, buildingGlassMesh.name);
      MeshFilter mf = glassGO.AddComponent<MeshFilter>();
      MeshRenderer mr = glassGO.AddComponent<MeshRenderer>();

      mf.sharedMesh = buildingGlassMesh;

      Material material = BuildMaterialForRenderer(buildingGlassMesh, propModelDef, propModelDef.GlassMaterial);
      mr.sharedMaterial = material;

      return glassParentGO;
    }

    private GameObject CreateDecals(GameObject buildingGroupGO) {
      List<PropDecalDef> decals = PropBuildingDef.Decals;

      if (decals.Count > 0) {
        GameObject decalParentGO = CreateGameObject(buildingGroupGO, "_decals");

        foreach (PropDecalDef propDecalDef in decals) {
          CreateDecal(decalParentGO, propDecalDef);
        }

        return decalParentGO;
      }

      return null;
    }

    private void CreateDecal(GameObject decalParentGO, PropDecalDef propDecalDef) {
      Main.Logger.Log("[BuildingFactory.CreateDecal] About to create decal " + propDecalDef.Key);
      GameObject decalGO = CreateGameObject(decalParentGO, propDecalDef.Key);
      decalGO.SetActive(false);

      BTDecal btDecal = decalGO.AddComponent<BTDecal>();
      Material material = BuildMaterialForRenderer(null, propDecalDef, propDecalDef.Material);

      btDecal.decalMaterial = material;
      btDecal.sheetXCoord = propDecalDef.SheetXCoordinate;
      btDecal.sheetYCoord = propDecalDef.SheetYCoordinate;
      btDecal.alpha = propDecalDef.Alpha;
      btDecal.priority = propDecalDef.Priority;

      decalGO.transform.localPosition = propDecalDef.Position.Value;
      decalGO.transform.localEulerAngles = propDecalDef.Rotation.Value;
      decalGO.transform.localScale = propDecalDef.Scale.Value;

      decalGO.SetActive(true);
    }

    private GameObject CreateFlimsies(GameObject buildingGroupGO) {
      List<PropDestructibleFlimsyDef> flimsyModels = PropBuildingDef.DestructibleFlimsyModels;

      if (flimsyModels.Count > 0) {
        GameObject flimsyParentGO = CreateGameObject(buildingGroupGO, "_flimsy");
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

    // private void EnsureMiniumMeshSize(GameObject go) {
    //   Vector3 minSize = new Vector3(4, 1, 4);
    //   Transform transform = go.transform;
    //   MeshFilter meshFilter = go.GetComponent<MeshFilter>();
    //   if (meshFilter == null) {
    //     Debug.LogError("MeshFilter component is missing.");
    //     return;
    //   }

    //   Mesh mesh = meshFilter.mesh;
    //   Vector3 meshSize = mesh.bounds.size;

    //   Vector3 scale = transform.localScale;
    //   if (meshSize.x < minSize.x) {
    //     scale.x = minSize.x;
    //   }

    //   if (meshSize.y < minSize.y) {
    //     scale.y = minSize.y;
    //   }

    //   if (meshSize.z < minSize.z) {
    //     scale.z = minSize.z;
    //   }

    //   transform.localScale = scale;
    // }

    private bool AttachFlimsyMesh(GameObject flimsyGO, PropDestructibleFlimsyDef flimsyDef) {
      PropModelDef propModelDef = flimsyDef.GetPropModelDef();
      Mesh flimsyLOD0Mesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_LOD0", "LOD0", isFlimsyBase: false);
      if (flimsyLOD0Mesh == null) {
        flimsyLOD0Mesh = ResolveMesh(propModelDef, propModelDef.MeshName, "Flimsy", isFlimsyBase: true);
      }

      if (flimsyLOD0Mesh == null) {
        Main.Logger.LogError($"[BuildingFactory.AttachFlimsyMesh] Could not resolve flimsy mesh '{propModelDef.MeshName}' for '{propModelDef.Key}'");
        return false;
      }

      MeshFilter flimsyLOD0MF = flimsyGO.GetComponent<MeshFilter>();
      MeshRenderer flimsyLOD0MR = flimsyGO.GetComponent<MeshRenderer>();

      Material[] materials = BuildMaterialsForRenderer(flimsyLOD0Mesh, propModelDef, propModelDef.Materials, placeholderMaterial);
      flimsyLOD0MR.materials = materials;
      flimsyLOD0MF.mesh = flimsyLOD0Mesh;
      return true;
    }

    private void CreateGenericStaticDestruct(GameObject buildingGO) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      CustomUnityModelAsset fileAsset = propModelDef.UsesFileForMesh ? ResolveCustomFileModel(propModelDef, makeReadable: true) : null;
      Material[] outerMaterials = BuildMaterialsForRenderer(buildingLOD0Mesh, propModelDef, propModelDef.Materials, placeholderMaterial);
      Material interiorRubbleMaterial = ResolveInteriorRubbleMaterial(propModelDef, outerMaterials);

      // SPLIT
      bool builtSplits = false;
      if (fileAsset != null && fileAsset.CustomSplits != null && fileAsset.CustomSplits.Count > 0 && !string.Equals(propModelDef.SplitMode, "None", StringComparison.OrdinalIgnoreCase)) {
        InstantiateSplitPieces(buildingGO, propModelDef, fileAsset.CustomSplits, outerMaterials);
        builtSplits = true;
      } else if (propModelDef.HasCustomSplits && propModelDef.UsesBundleForMesh && !string.IsNullOrEmpty(propModelDef.BundlePath)) {
        LoadAssetBundle(propModelDef);

        // Spawn the custom bundled prefab split
        GameObject destructSplitPrefab = AssetBundleLoader.GetAsset<GameObject>(propModelDef.BundlePath, $"{propModelDef.MeshName}_{GenericStaticDestructName}_split");
        if (destructSplitPrefab != null) {
          destructSplit = GameObject.Instantiate(destructSplitPrefab, Vector3.zero, new Quaternion(), buildingGO.transform);
          LayerTools.SetLayerRecursively(destructSplit, 8);

          foreach (Transform splitPiece in destructSplit.transform) {
            MeshRenderer splitRenderer = splitPiece.GetComponent<MeshRenderer>();
            if (splitRenderer != null) {
              splitRenderer.materials = outerMaterials;
            }
          }
          builtSplits = true;
        }
      } else if (!propModelDef.UsesFileForMesh
                 && (propModelDef.HasCustomSplits || !string.IsNullOrEmpty(propModelDef.SplitPrefabName))
                 && !string.Equals(propModelDef.AssetSource, "Bundle", StringComparison.OrdinalIgnoreCase)) {
        // Reconstruct genuine vanilla multi-chunk splits with exact per-piece materials from BattleTech_Data
        List<SplitPieceAsset> vanillaPieces = GameAssetProvider.Instance.TryLoadSplitPrefab(
          propModelDef.SplitPrefabName,
          propModelDef.MeshName,
          propModelDef.SourceAssetFile,
          matName => matLookup != null && matLookup.TryGetValue(matName, out Material m) ? m : null);

        if (vanillaPieces != null && vanillaPieces.Count > 0) {
          InstantiateSplitPieces(buildingGO, propModelDef, vanillaPieces, outerMaterials);
          builtSplits = true;
        }
      }

      if (!builtSplits && !string.Equals(propModelDef.SplitMode, "None", StringComparison.OrdinalIgnoreCase)) {
        // Procedural plane-sliced solid chunks (shared with Designer-Prop-Studio)
        BTAssetReader.Core.Models.RawMeshData rawSource = fileAsset?.RawLod0Mesh ?? CustomModelProvider.ExtractRawMeshFromUnityMesh(buildingLOD0Mesh);
        if (rawSource != null) {
          int count = propModelDef.FractureCount > 1 ? propModelDef.FractureCount : 8;
          List<SplitPieceAsset> proceduralPieces = CustomModelProvider.Instance.GetOrBuildProceduralSplits(
            propModelDef.Key,
            rawSource,
            count,
            propModelDef.FractureSeed,
            outerMaterials,
            interiorRubbleMaterial);

          if (proceduralPieces != null && proceduralPieces.Count > 0) {
            InstantiateSplitPieces(buildingGO, propModelDef, proceduralPieces, outerMaterials);
            builtSplits = true;
          }
        }
      }

      if (!builtSplits) {
        // Fallback single-piece fracture
        destructSplit = CreateGameObject(buildingGO, $"{buildingGO.name}_{GenericStaticDestructName}_split");
        destructSplit.layer = 8;

        List<GameObject> splitPieceGOs = MeshFracturer.Fracture(buildingGO, 1);
        foreach (GameObject splitPiece in splitPieceGOs) {
          splitPiece.transform.SetParent(destructSplit.transform, false);
          splitPiece.transform.localPosition = Vector3.zero;
          splitPiece.transform.localRotation = Quaternion.identity;
          splitPiece.layer = 8;

          splitPiece.GetComponent<MeshRenderer>().materials = outerMaterials;
        }
      }

      destructSplit.AddComponent<PhysicsExplodeChildren>();
      destructSplit.SetActive(false);

      // SHELL
      destructShell = CreateGameObject(buildingGO, $"{buildingGO.name}_{GenericStaticDestructName}_shell");
      destructShell.layer = 8;
      destructShell.SetActive(false);
      usedCustomOrAutoStumpShell = false;

      bool wantAutoStump = string.Equals(propModelDef.ShellMode, "AutoStump", StringComparison.OrdinalIgnoreCase)
        || (string.Equals(propModelDef.ShellMode, "Auto", StringComparison.OrdinalIgnoreCase) && propModelDef.UsesFileForMesh && fileAsset?.ShellMesh == null);
      bool hasFileCustomShell = fileAsset?.ShellMesh != null && !string.Equals(propModelDef.ShellMode, "Rubble", StringComparison.OrdinalIgnoreCase);

      if (propModelDef.HasCustomShell || hasFileCustomShell) {
        Main.Logger.Log("[BuildingFactory.CreateGenericStaticDestruct] CustomShell has been set for " + PropBuildingDef.Key);

        Mesh shellMesh = hasFileCustomShell ? fileAsset.ShellMesh : ResolveMesh(propModelDef, $"{propModelDef.MeshName}_shell", "Shell", isFlimsyBase: false);
        Mesh shellCOLMesh = hasFileCustomShell ? (fileAsset.ShellColMesh ?? fileAsset.ShellMesh) : ResolveMesh(propModelDef, $"{propModelDef.MeshName}_shell_COL", "ShellCOL", isFlimsyBase: false);

        if (shellMesh == null) {
          Main.Logger.LogError($"[BuildingFactory.CreateGenericStaticDestruct] Shell Mesh is null for {propModelDef.Key}");
          throw new Exception($"[BuildingFactory.CreateGenericStaticDestruct] Model '{propModelDef.Key}' is missing a custom shell mesh even though it's set to use one");
        } else {
          Main.Logger.Log("[BuildingFactory.CreateGenericStaticDestruct] Shell Mesh is " + shellMesh.name);
        }

        AttachShellMesh(shellMesh, shellCOLMesh, propModelDef, outerMaterials, interiorRubbleMaterial);
        usedCustomOrAutoStumpShell = true;
      } else if (wantAutoStump) {
        BTAssetReader.Core.Models.RawMeshData rawSource = fileAsset?.RawLod0Mesh ?? CustomModelProvider.ExtractRawMeshFromUnityMesh(buildingLOD0Mesh);
        Mesh stumpMesh = rawSource != null
          ? CustomModelProvider.Instance.GetOrBuildAutoStumpShell(propModelDef.Key, rawSource, propModelDef.StumpHeight, propModelDef.FractureSeed)
          : null;

        if (stumpMesh != null) {
          AttachShellMesh(stumpMesh, stumpMesh, propModelDef, outerMaterials, interiorRubbleMaterial);
          usedCustomOrAutoStumpShell = true;
        }
      }

      if (!usedCustomOrAutoStumpShell) {
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

    private void InstantiateSplitPieces(GameObject buildingGO, PropModelDef propModelDef, List<SplitPieceAsset> pieces, Material[] fallbackOuterMaterials) {
      destructSplit = CreateGameObject(buildingGO, $"{buildingGO.name}_{GenericStaticDestructName}_split");
      destructSplit.layer = 8;

      foreach (SplitPieceAsset piece in pieces) {
        GameObject pieceGO = CreateGameObject(destructSplit, piece.Name ?? "splitPiece");
        pieceGO.layer = 8;
        pieceGO.transform.localPosition = piece.LocalPosition;
        pieceGO.transform.localRotation = piece.LocalRotation;
        pieceGO.transform.localScale = piece.LocalScale;

        MeshFilter pmf = pieceGO.AddComponent<MeshFilter>();
        pmf.sharedMesh = piece.Mesh;

        MeshRenderer pmr = pieceGO.AddComponent<MeshRenderer>();
        if (piece.Materials != null && piece.Materials.Length > 0) {
          Material[] safeMats = new Material[piece.Materials.Length];
          for (int mi = 0; mi < piece.Materials.Length; mi++) {
            safeMats[mi] = piece.Materials[mi] ?? placeholderMaterial;
          }
          pmr.sharedMaterials = safeMats;
        } else {
          pmr.sharedMaterials = fallbackOuterMaterials;
        }

        if (piece.MeshColliderMesh != null) {
          MeshCollider mc = pieceGO.AddComponent<MeshCollider>();
          mc.sharedMesh = piece.MeshColliderMesh;
          mc.convex = piece.MeshColliderConvex;
        } else {
          BoxCollider bc = pieceGO.AddComponent<BoxCollider>();
          if (piece.BoxCollider != null) {
            bc.center = new Vector3(piece.BoxCollider.Center.X, piece.BoxCollider.Center.Y, piece.BoxCollider.Center.Z);
            bc.size = new Vector3(piece.BoxCollider.Size.X, piece.BoxCollider.Size.Y, piece.BoxCollider.Size.Z);
          } else {
            CalculateBounds(pmf, bc);
          }
        }

        Rigidbody rb = pieceGO.AddComponent<Rigidbody>();
        rb.mass = piece.Mass;

        DestructibleObject pieceDestructible = pieceGO.AddComponent<DestructibleObject>();
        pieceDestructible.dependentPersistentFX = new List<GameObject>();
        pieceDestructible.embeddedFlimsyChildren = new List<DestructibleObject>();

        pieceGO.AddComponent<PhysicsExplodeChild>();
      }
    }

    private void AttachShellMesh(Mesh shellMesh, Mesh shellCOLMesh, PropModelDef propModelDef, Material[] outerMaterials, Material interiorRubbleMaterial) {
      GameObject shellGO = new GameObject(shellMesh.name);
      shellGO.transform.SetParent(destructShell.transform, false);
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
        mr.materials = BuildMaterialsForRenderer(shellMesh, propModelDef, propModelDef.CustomShellMaterials, placeholderMaterial);
      } else if (outerMaterials != null && outerMaterials.Length > 0 && shellMesh.subMeshCount > 0) {
        Material[] shellMats = new Material[shellMesh.subMeshCount];
        for (int s = 0; s < shellMesh.subMeshCount; s++) {
          if (s < outerMaterials.Length) shellMats[s] = outerMaterials[s];
          else shellMats[s] = interiorRubbleMaterial ?? outerMaterials[0];
        }
        mr.materials = shellMats;
      } else {
        SetShellMaterials(shellGO);
      }
    }

    private Material ResolveInteriorRubbleMaterial(PropModelDef propModelDef, Material[] outerMaterials) {
      string preferredName = propModelDef.DestructibleMaterial == DestructibleObject.DestructibleMaterial.metal
        ? "envMatStct_darkMetal_generic"
        : "envMatStct_concrete_generic";

      if (matLookup != null && matLookup.TryGetValue(preferredName, out Material sceneMat) && sceneMat != null) {
        return sceneMat;
      }
      Material gdMat = GameAssetProvider.Instance.TryLoadMaterial(preferredName);
      if (gdMat != null) return gdMat;
      if (outerMaterials != null && outerMaterials.Length > 0 && outerMaterials[0] != null) return outerMaterials[0];
      return placeholderMaterial;
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
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      // Setup materials
      Mesh mesh = shell.GetComponent<MeshFilter>().mesh;
      MeshRenderer mr = shell.GetComponent<MeshRenderer>();

      Material[] shellMaterials = BuildMaterialsForRenderer(mesh, propModelDef, propModelDef.Materials.Where(mat => !mat.Name.Contains("glass") && !mat.Name.Contains("lights") && !mat.Name.Contains("decals")).ToList(), placeholderMaterial);

      Material[] shellMaterialToUse = new Material[mesh.subMeshCount];
      for (int i = 0; i < mesh.subMeshCount; i++) {
        shellMaterialToUse[i] = shellMaterials.Length > 0 ? shellMaterials[UnityEngine.Random.Range(0, shellMaterials.Length)] : placeholderMaterial;
      }

      mr.materials = shellMaterialToUse;
    }
  }
}