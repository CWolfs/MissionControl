using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;

using MissionControl.Data;

namespace MissionControl.EncounterFactories {
  public class BuildingFactory {
    private static GameObject mcBuildingParent = null;
    public static GameObject MCBuildingParent {
      get {
        if (mcBuildingParent == null) {
          mcBuildingParent = new GameObject("MCBuildingParent");
          mcBuildingParent.transform.SetParent(MissionControl.Instance.EncounterLayerData.transform);
          mcBuildingParent.transform.position = Vector3.zero;
        }

        return mcBuildingParent;
      }
    }

    private static GameObject mcGenericStaticDestruct = null;
    public static GameObject MCGenericStaticDestruct {
      get {
        if (mcGenericStaticDestruct == null) {
          mcGenericStaticDestruct = new GameObject("MCGenericStaticDestruct");
          mcGenericStaticDestruct.transform.SetParent(MissionControl.Instance.EncounterLayerData.transform);
          mcGenericStaticDestruct.transform.position = Vector3.zero;
        }

        return mcGenericStaticDestruct;
      }
    }

    private static string genericStaticDestructName = "generic_static_destruct";

    private string facilityName = "UNNAMED";

    private PropBuildingDef PropBuildingDef { get; set; }

    // TODO: Make this cache at custom contract type level
    private Mesh[] allGameMeshes;
    private Material[] allGameMaterials;
    private DamageAssetGroup[] allDamageAssetGroups;
    private Dictionary<string, Material> matLookup;

    private Material placeholderMaterial;

    private GameObject facilityGO;
    private GameObject buildingGroupGO;
    private GameObject buildingGO;

    private Mesh buildingCOLMesh = null;
    private Mesh buildingLOD0Mesh = null;
    private Mesh buildingLOD1Mesh = null;
    private Mesh buildingLOD2Mesh = null;

    // private GameObject destructDecalParent;
    private GameObject destructSplit;
    private GameObject destructShell;

    public BuildingFactory(PropBuildingDef propBuildingDef) {
      PropBuildingDef = propBuildingDef;

      allGameMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
      allGameMaterials = Resources.FindObjectsOfTypeAll<Material>();
      allDamageAssetGroups = Resources.FindObjectsOfTypeAll<DamageAssetGroup>();

      placeholderMaterial = new Material(Shader.Find("BattleTech Standard"));

      matLookup = new Dictionary<string, Material>();
      foreach (Material mat in allGameMaterials) {
        matLookup[mat.name] = mat;
      }
    }

    private GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.parent = parent.transform;
      gameObject.transform.localPosition = Vector3.zero;

      return gameObject;
    }

    public GameObject CreateFacility(string name) {
      this.facilityName = name;
      facilityGO = CreateGameObject(BuildingFactory.MCBuildingParent, name);

      CreateBuildingGroup(facilityGO, $"BuildingGroup_{facilityName}");
      facilityGO.AddComponent<FacilityParent>();

      return facilityGO;
    }

    private GameObject CreateBuildingGroup(GameObject facilityGO, string name) {
      buildingGroupGO = CreateGameObject(facilityGO, name);

      CreateBuilding(buildingGroupGO, $"Building_{facilityName}");

      SnapToTerrain snapToTerrain = buildingGroupGO.AddComponent<SnapToTerrain>();
      BuildingRepresentation buildingRepresentation = buildingGroupGO.AddComponent<BuildingRepresentation>();

      ObstructionGameLogic obstructionGameLogic = buildingGroupGO.AddComponent<ObstructionGameLogic>();
      obstructionGameLogic.buildingDefId = PropBuildingDef.BuildingDefID; // Supports direct BuildingDefIds (e.g. buildingdef_Military_Large) or general values (e.g. ObstructionGameLogic.buildingDef_SolidObstruction)
      obstructionGameLogic.teamDefinitionGuid = TeamUtils.WORLD_TEAM_ID;

      string obstructionGuid = Guid.NewGuid().ToString();
      obstructionGameLogic.encounterObjectGuid = obstructionGuid;

      // Track the custom buildings to bypass the min 8 cell hit count for buildings to be added to the proper building list of a MapEncounterLayerDataCell
      MissionControl.Instance.CustomBuildingGuids.Add(obstructionGuid);

      // obstructionGameLogic.overrideBuildingName // keep here as a reminder they exist if needed
      // obstructionGameLogic.overrideStructurePoints // keep here as a reminder they exist if needed

      // Health is set by buildingDefId, or a manual override 'overrideStructurePoints' on Building set via ObstructionGameLogic
      DestructibleObjectGroup destructibleObjectGroup = buildingGroupGO.AddComponent<DestructibleObjectGroup>();
      destructibleObjectGroup.BakeDestructionAssets();

      snapToTerrain.ForceCalculateCurrentHeightOffset();

      return buildingGroupGO;
    }

    private GameObject CreateBuilding(GameObject buildingGroupGO, string name) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      buildingGO = CreateGameObject(buildingGroupGO, name);
      buildingGO.SetActive(false);

      CreateColAndLODs(buildingGO);
      CreateFlimsies(buildingGO);
      CreateGenericStaticDestruct(buildingGO);

      DestructibleObject destructibleObject = buildingGO.AddComponent<DestructibleObject>();
      destructibleObject.destructType = DestructibleObject.DestructType.targetStruct;
      destructibleObject.structSize = PropBuildingDef.DestructibleSize;
      destructibleObject.structMaterial = PropBuildingDef.DestructibleMaterial;
      destructibleObject.flimsyDestructType = PropBuildingDef.FlimsyDestructibleType;
      destructibleObject.dependentPersistentFX = new List<GameObject>();

      destructibleObject.damagedInstance = destructSplit.GetComponent<PhysicsExplodeChildren>();
      destructibleObject.embeddedFlimsyChildren = new List<DestructibleObject>();

      destructibleObject.shellInstance = destructShell;

      destructibleObject.damageAssetGroup = allDamageAssetGroups[UnityEngine.Random.Range(0, allDamageAssetGroups.Length)];
      destructibleObject.decalObjects = new List<GameObject>();
      destructibleObject.decalSpawners = new List<BTDecalSpawner>();

      LODGroup lodGroup = buildingGO.AddComponent<LODGroup>();
      lodGroup.animateCrossFading = true;
      lodGroup.fadeMode = LODFadeMode.CrossFade;
      LOD[] lods = new LOD[1]; // Numer of LODS

      // Setup LOD0 
      // TODO: Support LOD1, LOD2
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

      buildingGO.SetActive(true);

      destructibleObject.destructionParent.transform.SetParent(MCGenericStaticDestruct.transform, true);

      destructSplit.transform.SetParent(destructibleObject.destructionParent.transform, false);
      destructSplit.transform.localPosition = Vector3.zero;
      destructSplit.transform.localEulerAngles = Vector3.zero;
      destructShell.transform.SetParent(destructibleObject.destructionParent.transform, false);
      destructShell.transform.localPosition = Vector3.zero;
      destructShell.transform.localEulerAngles = Vector3.zero;

      if (!propModelDef.HasCustomShell) RandomiseShell(destructibleObject);

      return buildingGO;
    }

    private void LoadAssetBundle(PropModelDef propModelDef) {
      if (!AssetBundleLoader.HasAlreadyLoadedBundle(propModelDef.BundlePath)) {
        AssetBundleLoader.LoadPropBundle(propModelDef.BundlePath);
      }
    }

    private Mesh CenterMeshPivot(Mesh oldMesh) {
      // Copy the mesh to not alter the original one
      Mesh mesh = MonoBehaviour.Instantiate(oldMesh);

      // Calculate the mesh's center point
      Vector3 center = mesh.bounds.center;

      // Shift the mesh vertices to center the mesh
      Vector3[] vertices = mesh.vertices;
      for (int i = 0; i < vertices.Length; i++) {
        vertices[i] -= center;
      }

      mesh.vertices = vertices;
      mesh.RecalculateBounds();

      return mesh;
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

    private void CreateColAndLODs(GameObject buildingGO) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      if (propModelDef.IsMeshInBundle) {
        LoadAssetBundle(propModelDef);

        buildingCOLMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_COL");
        buildingLOD0Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_LOD0");
        buildingLOD1Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_LOD1");
        buildingLOD2Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_LOD2");

        if (buildingCOLMesh == null) {
          Main.Logger.LogError("[BuildingFactory.CreateColAndLODs] Bundle COL Mesh is is null. It's possible LOD0, LOD1 and LOD2 might also be null. Check the names of the Meshes in the bundle match the Mesh in the MC PropModelDef");
        } else {
          Main.Logger.Log("[BuildingFactory.CreateColAndLODs] Bundle COL Mesh is " + buildingCOLMesh.name);
        }
      } else {
        foreach (Mesh mesh in allGameMeshes) {
          // If a flimsy base (e.g. no COL, LOD0, LOD1, LOD2) then set them all to the flimsy mesh
          if (mesh.name == propModelDef.MeshName) {
            Main.Logger.Log($"[BuildingFactory.CreateColAndLODs] Found a flimsy base for '{propModelDef.Key}' so using that for COL, LOD0, LOD1, LOD2");

            // If enabled, recenter the mesh pivot as filmsy pivots are often all over the place
            Mesh flimsyFormattedMesh = mesh;
            if (propModelDef.ChangePivotToCenterIfFlimsyMeshFormat) {
              flimsyFormattedMesh = CenterMeshPivot(mesh);
            }

            buildingCOLMesh = flimsyFormattedMesh;
            buildingLOD0Mesh = flimsyFormattedMesh;
            buildingLOD1Mesh = flimsyFormattedMesh;
            buildingLOD2Mesh = flimsyFormattedMesh;
            break;
          }

          if (mesh.name == $"{propModelDef.MeshName}_COL") buildingCOLMesh = mesh;
          if (mesh.name == $"{propModelDef.MeshName}_LOD0") buildingLOD0Mesh = mesh;
          if (mesh.name == $"{propModelDef.MeshName}_LOD1") buildingLOD1Mesh = mesh;
          if (mesh.name == $"{propModelDef.MeshName}_LOD2") buildingLOD2Mesh = mesh;
        }
      }

      GameObject buildingCOLGO = CreateGameObject(buildingGO, $"{buildingGO.name}_COL");
      MeshCollider buildingCOLCollider = buildingCOLGO.AddComponent<MeshCollider>();
      buildingCOLCollider.sharedMesh = buildingCOLMesh;
      buildingCOLGO.layer = 12; // so raycasts can hit it for highlight effect and selection
      // EnsureMiniumMeshSize(buildingCOLGO);

      GameObject buildingLOD0GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD0");
      MeshFilter buildingLOD0MF = buildingLOD0GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD0MR = buildingLOD0GO.AddComponent<MeshRenderer>();
      // EnsureMiniumMeshSize(buildingLOD0GO);

      Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, propModelDef.Materials, placeholderMaterial);
      buildingLOD0MR.materials = materials;
      buildingLOD0MF.mesh = buildingLOD0Mesh;

      // GameObject buildingLOD1GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD1");
      // MeshFilter buildingLOD1MF = buildingLOD1GO.AddComponent<MeshFilter>();
      // MeshRenderer buildingLOD1MR = buildingLOD1GO.AddComponent<MeshRenderer>();
      // buildingLOD1MF.mesh = buildingLOD1Mesh;

      // GameObject buildingLOD2GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD2");
      // MeshFilter buildingLOD2MF = buildingLOD2GO.AddComponent<MeshFilter>();
      // MeshRenderer buildingLOD2MR = buildingLOD2GO.AddComponent<MeshRenderer>();
      // buildingLOD2MF.mesh = buildingLOD2Mesh;
    }

    private void CreateFlimsies(GameObject buildingGO) {

    }

    private void CreateGenericStaticDestruct(GameObject buildingGO) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      // Split
      destructSplit = CreateGameObject(buildingGO, $"{buildingGO.name}_{genericStaticDestructName}_split");
      destructSplit.layer = 8;

      List<GameObject> splitPieceGOs = MeshFracturer.Fracture(buildingGO, 1);
      foreach (GameObject splitPiece in splitPieceGOs) {
        splitPiece.transform.SetParent(destructSplit.transform);
        splitPiece.layer = 8;

        // Apply material
        // TODO: The fracture UV mapping and material assignment need more work. Fewer mats should be assign per split (e.g. in vanilla a building with 7 mats might have splits with 2-3 mats depending on how they were fractured )
        // TODO: So we don't need to assign all 7 original ones to each split. Probably need a 3rd party lib or definitely a better script for fracturing.
        // TODO: Having issues making good runtime fractures - just use a copy of LOD0 for now
        Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, PropBuildingDef.GetPropModelDef().Materials, placeholderMaterial);
        splitPiece.GetComponent<MeshRenderer>().materials = materials;
      }

      destructSplit.AddComponent<PhysicsExplodeChildren>();
      destructSplit.SetActive(false);

      // Shell
      destructShell = CreateGameObject(buildingGO, $"{buildingGO.name}_{genericStaticDestructName}_shell");
      destructShell.layer = 8;
      destructShell.SetActive(false);

      if (propModelDef.HasCustomShell) {
        Main.Logger.Log("[BuildingFactory.CreateGenericStaticDestruct] CustomShell has been set for " + PropBuildingDef.Key);

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

          Material[] materials = BuildMaterialsForRenderer(shellMesh, propModelDef.CustomShellMaterials, placeholderMaterial);
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

        switch (PropBuildingDef.DestructibleSize) {
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
      // Setup materials
      Mesh mesh = shell.GetComponent<MeshFilter>().mesh;
      MeshRenderer mr = shell.GetComponent<MeshRenderer>();

      Material[] shellMaterials = BuildMaterialsForRenderer(mesh, PropBuildingDef.GetPropModelDef().Materials.Where(mat => !mat.Name.Contains("glass") && !mat.Name.Contains("lights") && !mat.Name.Contains("decals")).ToList(), null);

      Material[] shellMaterialToUse = new Material[mesh.subMeshCount];
      for (int i = 0; i < mesh.subMeshCount; i++) {
        shellMaterialToUse[i] = shellMaterials[UnityEngine.Random.Range(0, shellMaterials.Length)];
      }

      mr.materials = shellMaterialToUse;
    }

    private Material[] BuildMaterialsForRenderer(Mesh mesh, List<PropMaterialDef> materialDefs, Material placeholderMaterial) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();
      Material[] materials = new Material[mesh.subMeshCount];

      Main.Logger.Log($"[BuildingFactory.BuildMaterialsForRenderer] mesh.subMeshCount for '{mesh.name}' is '{mesh.subMeshCount}'");
      for (int i = 0; i < mesh.subMeshCount; i++) {
        if (i >= materialDefs.Count) {
          Main.Logger.LogWarning($"[BuildingFactory.BuildMaterialsForRenderer] Not enough supplied material identifiers in prop data to use for submesh '{i}' for mesh '{mesh.name}'. Duplicating existing mat references.");
          materialDefs.Add(materialDefs[UnityEngine.Random.Range(0, materialDefs.Count)]);
        }

        PropMaterialDef propMaterialDef = materialDefs[i];

        if (propMaterialDef.Shader != null) {
          Main.Logger.Log($"[BuildingFactory.BuildMaterialsForRenderer] Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with specific shader '{propMaterialDef.Shader}' and texture '{propMaterialDef.Texture}'");
          // Build whole material with specific shader and texture
          if (propModelDef.BundlePath == null) Main.Logger.LogWarning("[BuildingFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef Shader name but you have no included a bundle for this PropModelDef. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");
          if (propModelDef.BundlePath == null) Main.Logger.LogWarning("[BuildingFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef Texture name but you have no included a bundle for this PropModelDef. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");

          // First look for Shader in bundle
          Shader shader = propModelDef.BundlePath != null ? AssetBundleLoader.GetAsset<Shader>(propModelDef.BundlePath, propMaterialDef.Shader) : null;

          // Otherwise look for Shader in game
          if (shader == null) shader = Shader.Find(propMaterialDef.Shader);

          // First look for Texture in bundle
          Texture texture = propModelDef.BundlePath != null ? AssetBundleLoader.GetAsset<Texture>(propModelDef.BundlePath, propMaterialDef.Texture) : null;

          // Otherwise look for texture in game
          if (texture == null) texture = UnityGameInstance.Instance.Game.DataManager.TextureManager.GetLoadedTexture(propMaterialDef.Texture);

          Material material = new Material(shader);
          material.mainTexture = texture;
          materials[i] = material;
        } else if (propMaterialDef.Texture != null) {
          Main.Logger.Log($"[BuildingFactory.BuildMaterialsForRenderer] Texture but no Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with default shader 'BattleTech Standard' and texture '{propMaterialDef.Texture}'");
          if (propModelDef.BundlePath == null) Main.Logger.LogWarning("[BuildingFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef Texture name but you have no included a bundle for this PropModelDef. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");

          // Build whole material with specifix texture but use 'BattleTech Standard' shader
          Shader shader = Shader.Find("BattleTech Standard");

          // First look for Texture in bundle
          Texture texture = propModelDef.BundlePath != null ? AssetBundleLoader.GetAsset<Texture>(propModelDef.BundlePath, propMaterialDef.Texture) : null;

          // Otherwise look for texture in game
          if (texture == null) texture = UnityGameInstance.Instance.Game.DataManager.TextureManager.GetLoadedTexture(propMaterialDef.Texture);

          Material material = new Material(shader);
          material.mainTexture = texture;
          materials[i] = material;
        } else {
          Main.Logger.Log($"[BuildingFactory.BuildMaterialsForRenderer] Only material name provided in PropMaterialDef '{propMaterialDef.Name}' so looking for material in bundle first then game data");

          // Look first at bundle for custom bundled Material
          Material mat = propModelDef.BundlePath != null ? AssetBundleLoader.GetAsset<Material>(propModelDef.BundlePath, propMaterialDef.Name) : null;

          // Otherwise look at all game Materials
          if (mat == null) mat = matLookup.ContainsKey(propMaterialDef.Name) ? matLookup[propMaterialDef.Name] : placeholderMaterial;

          materials[i] = mat;
        }
      }

      return materials;
    }
  }
}