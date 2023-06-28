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
  public class BuildingBuilder {
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

    // TODO: Make this cache at custom contract type level
    private Mesh[] allGameMeshes;
    private Material[] allGameMaterials;
    private DamageAssetGroup[] allDamageAssetGroups;
    private Dictionary<string, Material> matLookup;

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

    public BuildingBuilder() {
      allGameMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
      allGameMaterials = Resources.FindObjectsOfTypeAll<Material>();
      allDamageAssetGroups = Resources.FindObjectsOfTypeAll<DamageAssetGroup>();

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

    public GameObject CreateFacility(string name, Vector3 position) {
      facilityGO = CreateGameObject(BuildingBuilder.MCBuildingParent, name);

      CreateBuildingGroup(facilityGO, "BuildingGroup_MyTower", position);

      facilityGO.AddComponent<FacilityParent>();
      facilityGO.transform.position = position;

      return facilityGO;
    }

    private GameObject CreateBuildingGroup(GameObject facilityGO, string name, Vector3 position) {
      buildingGroupGO = CreateGameObject(facilityGO, name);

      CreateBuilding(buildingGroupGO, "Building_MyTower", position);

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

    private GameObject CreateBuilding(GameObject buildingGroupGO, string name, Vector3 position) {
      buildingGO = CreateGameObject(buildingGroupGO, name);
      buildingGO.SetActive(false);

      CreateColAndLODs(buildingGO);
      CreateGenericStaticDestruct(buildingGO);

      DestructibleObject destructibleObject = buildingGO.AddComponent<DestructibleObject>();
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

      // TODO: Set these when read
      //  - destructibleObject.shellInstance

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

      buildingGO.SetActive(true);

      destructibleObject.destructionParent.transform.SetParent(MCGenericStaticDestruct.transform, true);
      destructibleObject.destructionParent.transform.position = position;

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

      // Mock the names of materials provided by a prop json
      // This will later be extroplated from the prop json (and checked if custom materials or not)
      List<string> materialNames = new List<string>() { "envMatStct_darkMetal_generic", "envMatStct_glassA_decals_generic", "envMatStct_mediumMetal_generic", "envMatStct_lightMetal_generic", "envMatStct_decals_generic", "envMatStct_lights_decals_generic", "envMatStct_asphalt_generic" };
      Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, materialNames, placeholderMaterial);

      buildingLOD0MR.materials = materials;
      buildingLOD0MF.mesh = buildingLOD0Mesh;
      // buildingLOD0MR.material = placeholderMaterial;

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
        Material placeholderMaterial = new Material(Shader.Find("BattleTech Standard"));
        List<string> materialNames = new List<string>() { "envMatStct_darkMetal_generic", "envMatStct_glassA_decals_generic", "envMatStct_mediumMetal_generic", "envMatStct_lightMetal_generic", "envMatStct_decals_generic", "envMatStct_lights_decals_generic", "envMatStct_asphalt_generic" };
        Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, materialNames, placeholderMaterial);
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

      List<string> shellMaterialNames = new List<string>() { "envMatStct_darkMetal_generic", "envMatStct_mediumMetal_generic", "envMatStct_lightMetal_generic", "envMatStct_asphalt_generic" };
      Material[] shellMaterials = BuildMaterialsForRenderer(mesh, shellMaterialNames, null);

      Material[] shellMaterialToUse = new Material[mesh.subMeshCount];
      for (int i = 0; i < mesh.subMeshCount; i++) {
        shellMaterialToUse[i] = shellMaterials[Random.Range(0, shellMaterials.Length)];
      }

      mr.materials = shellMaterialToUse;
    }

    private Material[] BuildMaterialsForRenderer(Mesh mesh, List<string> materialNames, Material placeholderMaterial) {
      Material[] materials = new Material[mesh.subMeshCount];
      for (int i = 0; i < mesh.subMeshCount; i++) {
        if (i >= materialNames.Count) {
          Main.Logger.LogWarning($"[CreateColAndLODs] Not enough supplied material identifiers in prop data to use for submesh '{i}' for mesh '{mesh.name}'. Falling back to placeholder mat");
          materials[i] = placeholderMaterial;
        } else {
          Material mat = matLookup.ContainsKey(materialNames[i]) ? matLookup[materialNames[i]] : placeholderMaterial;
          materials[i] = mat;
        }
      }

      return materials;
    }
  }
}