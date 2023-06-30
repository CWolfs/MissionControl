using UnityEngine;

using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;
using BattleTech.Rendering.UI;
using BattleTech.Framework;

using MissionControl.Utils;
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

      // obstructionGameLogic.overrideBuildingName // keep here as a reminder they exist if needed
      // obstructionGameLogic.overrideStructurePoints // keep here as a reminder they exist if needed

      // Health is set by buildingDefId, or a manual override 'overrideStructurePoints' on Building set via ObstructionGameLogic
      DestructibleObjectGroup destructibleObjectGroup = buildingGroupGO.AddComponent<DestructibleObjectGroup>();
      destructibleObjectGroup.BakeDestructionAssets();

      snapToTerrain.ForceCalculateCurrentHeightOffset();

      return buildingGroupGO;
    }

    private GameObject CreateBuilding(GameObject buildingGroupGO, string name) {
      buildingGO = CreateGameObject(buildingGroupGO, name);
      buildingGO.SetActive(false);

      CreateColAndLODs(buildingGO);
      CreateGenericStaticDestruct(buildingGO);

      DestructibleObject destructibleObject = buildingGO.AddComponent<DestructibleObject>();
      // TODO: Expose this to the PropDefs
      destructibleObject.destructType = DestructibleObject.DestructType.targetStruct;
      destructibleObject.structSize = DestructibleObject.DestructibleSize.large;
      destructibleObject.structMaterial = DestructibleObject.DestructibleMaterial.metal;
      destructibleObject.flimsyDestructType = FlimsyDestructType.largeMetal;
      destructibleObject.dependentPersistentFX = new List<GameObject>();

      destructibleObject.damagedInstance = destructSplit.GetComponent<PhysicsExplodeChildren>();
      destructibleObject.embeddedFlimsyChildren = new List<DestructibleObject>();

      destructibleObject.shellInstance = destructShell;

      destructibleObject.damageAssetGroup = allDamageAssetGroups[Random.Range(0, allDamageAssetGroups.Length)];
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

      RandomiseShell(destructibleObject);

      return buildingGO;
    }

    private void CreateColAndLODs(GameObject buildingGO) {
      PropModelDef propModelDef = PropBuildingDef.GetPropModelDef();

      if (propModelDef.IsMeshInBundle) {
        // TODO: Find mesh in bundle
      } else {
        foreach (Mesh mesh in allGameMeshes) {
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

      GameObject buildingLOD0GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD0");
      MeshFilter buildingLOD0MF = buildingLOD0GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD0MR = buildingLOD0GO.AddComponent<MeshRenderer>();

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

    private void CreateGenericStaticDestruct(GameObject buildingGO) {
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
        // List<string> materialNames = new List<string>() { "envMatStct_darkMetal_generic", "envMatStct_glassA_decals_generic", "envMatStct_mediumMetal_generic", "envMatStct_lightMetal_generic", "envMatStct_decals_generic", "envMatStct_lights_decals_generic", "envMatStct_asphalt_generic" };
        Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, PropBuildingDef.GetPropModelDef().Materials, placeholderMaterial);
        splitPiece.GetComponent<MeshRenderer>().materials = materials;
      }

      destructSplit.AddComponent<PhysicsExplodeChildren>();
      destructSplit.SetActive(false);

      // Shell
      destructShell = CreateGameObject(buildingGO, $"{buildingGO.name}_{genericStaticDestructName}_shell");
      destructShell.layer = 8;
      destructShell.SetActive(false);

      string shellPrefabName = "small_civilian_building_shell";
      GameObject randomShellPrefab = AssetBundleLoader.GetAsset<GameObject>("common-assets-bundle", shellPrefabName);
      if (randomShellPrefab == null) Main.Logger.LogError($"[CreateGenericStaticDestruct] Shell prefab '{shellPrefabName}' is null");

      shellPrefabName = "small_civilian_building_walls_shell";
      GameObject wallShellPrefab = AssetBundleLoader.GetAsset<GameObject>("common-assets-bundle", shellPrefabName);
      if (wallShellPrefab == null) Main.Logger.LogError($"[CreateGenericStaticDestruct] Shell prefab '{shellPrefabName}' is null");

      GameObject randomShell1 = GameObject.Instantiate(randomShellPrefab, Vector3.zero, new Quaternion(), destructShell.transform);
      randomShell1.name = randomShell1.name.Replace("(Clone)", "");

      GameObject randomShell2 = GameObject.Instantiate(randomShellPrefab, Vector3.zero, new Quaternion(), destructShell.transform);
      randomShell2.name = randomShell2.name.Replace("(Clone)", "");

      GameObject randomShell3 = GameObject.Instantiate(randomShellPrefab, Vector3.zero, new Quaternion(), destructShell.transform);
      randomShell3.name = randomShell3.name.Replace("(Clone)", "");

      GameObject wallShell = GameObject.Instantiate(wallShellPrefab, Vector3.zero, new Quaternion(), destructShell.transform);
      wallShell.name = wallShell.name.Replace("(Clone)", "");

      SetShellMaterials(randomShell1);
      SetShellMaterials(randomShell2);
      SetShellMaterials(randomShell3);
      SetShellMaterials(wallShell);
    }

    private void RandomiseShell(DestructibleObject linkedDestructibleObject) {
      float x = linkedDestructibleObject.footprint.x;
      float z = linkedDestructibleObject.footprint.z;

      foreach (Transform t in destructShell.transform) {
        t.localPosition = new Vector3(Random.Range(-x, x), 3, Random.Range(-z, z));

        if (!t.gameObject.name.Contains("wall")) {
          t.rotation = Random.rotation;

          float scale = Random.Range(0.5f, 1.5f);
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
        shellMaterialToUse[i] = shellMaterials[Random.Range(0, shellMaterials.Length)];
      }

      mr.materials = shellMaterialToUse;
    }

    private Material[] BuildMaterialsForRenderer(Mesh mesh, List<PropMaterialDef> materialDefs, Material placeholderMaterial) {
      Material[] materials = new Material[mesh.subMeshCount];

      Main.Logger.Log("[BuildingFactory.BuildMaterialsForRenderer] mesh.subMeshCount " + mesh.subMeshCount);
      for (int i = 0; i < mesh.subMeshCount; i++) {
        if (i >= materialDefs.Count) {
          Main.Logger.LogWarning($"[BuildingFactory.BuildMaterialsForRenderer] Not enough supplied material identifiers in prop data to use for submesh '{i}' for mesh '{mesh.name}'. Falling back to placeholder mat");
          materials[i] = placeholderMaterial;
          continue;
        }

        PropMaterialDef propMaterialDef = materialDefs[i];

        if (propMaterialDef.Shader != null) {
          Main.Logger.Log($"[BuildingFactory.BuildMaterialsForRenderer] Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with specific shader '{propMaterialDef.Shader}' and texture '{propMaterialDef.Texture}'");
          // Build whole material with specific shader and texture

          // First look for Shader in bundle
          Shader shader = null; // TODO: support this

          // Otherwise look for Shader in game
          if (shader == null) shader = Shader.Find(propMaterialDef.Shader);

          // First look for Texture in bundle
          Texture texture = null; // TODO: support this

          // Otherwise look for texture in game
          if (texture == null) texture = UnityGameInstance.Instance.Game.DataManager.TextureManager.GetLoadedTexture(propMaterialDef.Texture);

          Material material = new Material(shader);
          material.mainTexture = texture;
          materials[i] = material;
        } else if (propMaterialDef.Texture != null) {
          Main.Logger.Log($"[BuildingFactory.BuildMaterialsForRenderer] Texture but no Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with default shader 'BattleTech Standard' and texture '{propMaterialDef.Texture}'");

          // Build whole material with specifix texture but use 'BattleTech Standard' shader
          Shader shader = Shader.Find("BattleTech Standard");

          // First look for Texture in bundle
          Texture texture = null; // TODO: support this

          // Otherwise look for texture in game
          if (texture == null) texture = UnityGameInstance.Instance.Game.DataManager.TextureManager.GetLoadedTexture(propMaterialDef.Texture);

          Material material = new Material(shader);
          material.mainTexture = texture;
          materials[i] = material;
        } else {
          Main.Logger.Log($"[BuildingFactory.BuildMaterialsForRenderer] Only Material Name provided PropMaterialDef '{propMaterialDef.Name}' so looking for Material in bundle '{PropBuildingDef.GetPropModelDef().BundlePath}' first then Game data");

          // Look first at bundle for custom bundled Material
          Material mat = null; // TODO: support this

          // Otherwise look at all game Materials
          if (mat == null) mat = matLookup.ContainsKey(materialDefs[i].Name) ? matLookup[materialDefs[i].Name] : placeholderMaterial;

          materials[i] = mat;
        }
      }

      return materials;
    }
  }
}