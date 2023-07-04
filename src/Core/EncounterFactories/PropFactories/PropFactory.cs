using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;

using MissionControl.Data;

namespace MissionControl.EncounterFactories {
  public abstract class PropFactory {
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

    private static GameObject mcStructureParent = null;
    public static GameObject MCStructureParent {
      get {
        if (mcStructureParent == null) {
          mcStructureParent = new GameObject("MCStructureParent");
          mcStructureParent.transform.SetParent(MissionControl.Instance.EncounterLayerData.transform);
          mcStructureParent.transform.position = Vector3.zero;
        }

        return mcStructureParent;
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

    protected static string genericStaticDestructName = "generic_static_destruct";

    // Rebuilt at start of custom contract type building and reset after the build has completed
    protected static Mesh[] allGameMeshes;
    protected static Material[] allGameMaterials;
    protected static DamageAssetGroup[] allDamageAssetGroups;
    protected static Dictionary<string, Material> matLookup;
    protected static Material placeholderMaterial;

    protected Mesh buildingCOLMesh = null;
    protected Mesh buildingLOD0Mesh = null;
    protected Mesh buildingLOD1Mesh = null;
    protected Mesh buildingLOD2Mesh = null;

    private float[] lodDistances1 = new float[1] { 0f };
    private float[] lodDistances2 = new float[2] { 0.1f, 0f };
    private float[] lodDistances3 = new float[3] { 0.1f, 0.01f, 0f };

    /** This is called at the start of a custom contract type build to optimise asset grabs */
    public static void RebuildStaticAssets() {
      allGameMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
      allGameMaterials = Resources.FindObjectsOfTypeAll<Material>();
      allDamageAssetGroups = Resources.FindObjectsOfTypeAll<DamageAssetGroup>();

      placeholderMaterial = new Material(Shader.Find("BattleTech Standard"));

      matLookup = new Dictionary<string, Material>();
      foreach (Material mat in allGameMaterials) {
        matLookup[mat.name] = mat;
      }
    }

    /** This is called at the end of a custom contract type build to prevent holding references open */
    public static void ResetStaticAssets() {
      allGameMeshes = null;
      allGameMaterials = null;
      allDamageAssetGroups = null;
      matLookup = null;
      placeholderMaterial = null;
    }

    private GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.parent = parent.transform;
      gameObject.transform.localPosition = Vector3.zero;

      return gameObject;
    }

    protected LODGroup SetupLODGroup(GameObject buildingGO) {
      // Setup LOD Group
      LODGroup lodGroup = buildingGO.AddComponent<LODGroup>();
      lodGroup.animateCrossFading = true;
      lodGroup.fadeMode = LODFadeMode.CrossFade;

      MeshRenderer lod0MR = buildingGO.transform.Find($"{buildingGO.name}_LOD0").GetComponent<MeshRenderer>();
      MeshRenderer lod1MR = buildingGO.transform.Find($"{buildingGO.name}_LOD1")?.GetComponent<MeshRenderer>();
      MeshRenderer lod2MR = buildingGO.transform.Find($"{buildingGO.name}_LOD2")?.GetComponent<MeshRenderer>();


      int availableLODCount = 1;
      // Only support sequential LODs - do not allow LOD0 and LOD2, for example. Only LOD0 | LOD0, LOD1 | LOD0, LOD1, LOD2
      if (lod1MR != null && lod2MR == null) availableLODCount = 2;
      if (lod1MR != null && lod2MR != null) availableLODCount = 3;

      LOD[] lods = new LOD[availableLODCount]; // Numer of LODS

      float[] lodRanges = null;
      if (availableLODCount == 1) {
        lodRanges = lodDistances1;
      } else if (availableLODCount == 2) {
        lodRanges = lodDistances2;
      } else if (availableLODCount == 3) {
        lodRanges = lodDistances3;
      }

      Renderer[] lod0Renderers = new Renderer[1];
      lod0Renderers[0] = lod0MR;
      lods[0] = new LOD(lodRanges[0], lod0Renderers);

      if (lod1MR != null) {
        Renderer[] lod1Renderers = new Renderer[1];
        lod1Renderers[0] = lod1MR;
        lods[1] = new LOD(lodRanges[1], lod1Renderers);
      }

      if (lod2MR != null) {
        Renderer[] lod2Renderers = new Renderer[1];
        lod2Renderers[0] = lod2MR;
        lods[2] = new LOD(lodRanges[2], lod2Renderers);
      }

      // Set LODs into Group
      lodGroup.SetLODs(lods);
      lodGroup.RecalculateBounds();
      lodGroup.fadeMode = LODFadeMode.CrossFade;
      lodGroup.animateCrossFading = true;
      // lodGroup.size = 6f / Mathf.Max(Mathf.Max(staticRoot.transform.lossyScale.x, staticRoot.transform.lossyScale.y), staticRoot.transform.lossyScale.z);

      return lodGroup;
    }

    protected void CalculateBounds(MeshFilter mf, BoxCollider boxCollider) {
      Mesh mesh = mf.sharedMesh;
      Matrix4x4 localToWorld = mf.transform.localToWorldMatrix;
      Bounds bounds = new Bounds(localToWorld.MultiplyPoint3x4(mesh.vertices[0]), Vector3.zero);

      for (int i = 1; i < mesh.vertices.Length; i++) {
        bounds.Encapsulate(localToWorld.MultiplyPoint3x4(mesh.vertices[i]));
      }

      // Convert bounds back into local space
      Bounds localBounds = new Bounds(
          mf.transform.InverseTransformPoint(bounds.center),
          mf.transform.InverseTransformDirection(bounds.size)
      );

      boxCollider.size = localBounds.size;
      boxCollider.center = localBounds.center;
    }

    protected void LoadAssetBundle(PropModelDef propModelDef) {
      if (!AssetBundleLoader.HasAlreadyLoadedBundle(propModelDef.BundlePath)) {
        AssetBundleLoader.LoadPropBundle(propModelDef.BundlePath);
      }
    }

    protected Mesh CenterMeshPivot(Mesh oldMesh) {
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

    protected void CreateColAndLODs(GameObject buildingGO, PropModelDef propModelDef) {
      bool isFlimsyBase = false;

      if (propModelDef.IsMeshInBundle) {
        LoadAssetBundle(propModelDef);

        buildingCOLMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_COL");
        buildingLOD0Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_LOD0");
        buildingLOD1Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_LOD1");
        buildingLOD2Mesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}_LOD2");

        if (buildingCOLMesh == null && buildingLOD0Mesh == null) {
          Mesh bundledFlimsyStyledMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, $"{propModelDef.MeshName}");

          if (bundledFlimsyStyledMesh != null) {
            Main.Logger.Log($"[PropFactory.CreateColAndLODs] Found a flimsy base in bundle for '{propModelDef.Key}' so using that for COL, LOD0, LOD1, LOD2");
            bundledFlimsyStyledMesh.name = bundledFlimsyStyledMesh.name.Replace("(Clone)", "");

            // If enabled, recenter the mesh pivot as filmsy pivots are often all over the place
            if (propModelDef.ChangePivotToCenterIfFlimsyMeshFormat) {
              bundledFlimsyStyledMesh = CenterMeshPivot(bundledFlimsyStyledMesh);
            }

            buildingCOLMesh = bundledFlimsyStyledMesh;
            buildingLOD0Mesh = bundledFlimsyStyledMesh;
            buildingLOD1Mesh = bundledFlimsyStyledMesh;
            buildingLOD2Mesh = bundledFlimsyStyledMesh;

            isFlimsyBase = true;
          } else {
            Main.Logger.LogError("[PropFactory.CreateColAndLODs] IsMeshInBundle was true but couldn't find the mesh in either COL/LOD0/LOD1/LOD2 or fimsey (pure name) format");
          }
        }

        if (buildingCOLMesh == null) {
          Main.Logger.LogError("[PropFactory.CreateColAndLODs] Bundle COL Mesh is is null. It's possible LOD0, LOD1 and LOD2 might also be null. Check the names of the Meshes in the bundle match the Mesh in the MC PropModelDef");
        } else {
          Main.Logger.Log("[PropFactory.CreateColAndLODs] Bundle COL Mesh is " + buildingCOLMesh.name);
        }
      } else {
        foreach (Mesh mesh in allGameMeshes) {
          // If a flimsy base (e.g. no COL, LOD0, LOD1, LOD2) then set them all to the flimsy mesh
          if (mesh.name == propModelDef.MeshName) {
            Main.Logger.Log($"[PropFactory.CreateColAndLODs] Found a flimsy base for '{propModelDef.Key}' so using that for COL, LOD0, LOD1, LOD2");

            // If enabled, recenter the mesh pivot as filmsy pivots are often all over the place
            Mesh flimsyFormattedMesh = mesh;
            if (propModelDef.ChangePivotToCenterIfFlimsyMeshFormat) {
              flimsyFormattedMesh = CenterMeshPivot(mesh);
            }

            buildingCOLMesh = flimsyFormattedMesh;
            buildingLOD0Mesh = flimsyFormattedMesh;
            buildingLOD1Mesh = flimsyFormattedMesh;
            buildingLOD2Mesh = flimsyFormattedMesh;

            isFlimsyBase = true;

            break;
          }

          if (mesh.name == $"{propModelDef.MeshName}_COL") buildingCOLMesh = mesh;
          if (mesh.name == $"{propModelDef.MeshName}_LOD0") buildingLOD0Mesh = mesh;
          if (mesh.name == $"{propModelDef.MeshName}_LOD1") buildingLOD1Mesh = mesh;
          if (mesh.name == $"{propModelDef.MeshName}_LOD2") buildingLOD2Mesh = mesh;

          if (buildingCOLMesh != null && buildingLOD0Mesh != null && buildingLOD1Mesh != null && buildingLOD2Mesh != null) break;
        }
      }

      GameObject buildingCOLGO = CreateGameObject(buildingGO, $"{buildingGO.name}_COL");
      MeshCollider buildingCOLCollider = buildingCOLGO.AddComponent<MeshCollider>();
      buildingCOLCollider.sharedMesh = buildingCOLMesh;
      buildingCOLGO.layer = 12; // so raycasts can hit it for highlight effect and selection
      // ApplyScale(buildingCOLGO);

      GameObject buildingLOD0GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD0");
      MeshFilter buildingLOD0MF = buildingLOD0GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD0MR = buildingLOD0GO.AddComponent<MeshRenderer>();

      Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, propModelDef, propModelDef.Materials, placeholderMaterial);
      buildingLOD0MR.materials = materials;
      buildingLOD0MF.mesh = buildingLOD0Mesh;
      // ApplyScale(buildingLOD0GO);

      if (buildingLOD1Mesh != null) {
        Main.Logger.Log($"[PropFactory.CreateColAndLODs] Check - LOD1 mesh " + buildingLOD1Mesh.name + " isFlimsyBase: " + isFlimsyBase);
      } else {
        Main.Logger.Log($"[PropFactory.CreateColAndLODs] Check - LOD1 mesh is null and isFlimsyBase: " + isFlimsyBase);
      }

      if (!isFlimsyBase) {
        if (buildingLOD1Mesh != null) {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] Building LOD1 GO with mesh '{propModelDef.MeshName}_LOD1'");

          GameObject buildingLOD1GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD1");
          MeshFilter buildingLOD1MF = buildingLOD1GO.AddComponent<MeshFilter>();
          MeshRenderer buildingLOD1MR = buildingLOD1GO.AddComponent<MeshRenderer>();
          buildingLOD1MR.materials = materials;
          buildingLOD1MF.mesh = buildingLOD1Mesh;
        } else {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] No LOD1 mesh found under name '{propModelDef.MeshName}_LOD1' so skipping LOD1 GO build");
        }

        if (buildingLOD2Mesh != null) {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] Building LOD2 with mesh '{propModelDef.MeshName}_LOD1'");

          GameObject buildingLOD2GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD2");
          MeshFilter buildingLOD2MF = buildingLOD2GO.AddComponent<MeshFilter>();
          MeshRenderer buildingLOD2MR = buildingLOD2GO.AddComponent<MeshRenderer>();
          buildingLOD2MR.materials = materials;
          buildingLOD2MF.mesh = buildingLOD2Mesh;
        } else {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] No LOD2 mesh found under name '{propModelDef.MeshName}_LOD1' so skipping LOD2 GO build");
        }
      }
    }

    protected Material[] BuildMaterialsForRenderer(Mesh mesh, PropModelDef propModelDef, List<PropMaterialDef> materialDefs, Material placeholderMaterial) {
      Material[] materials = new Material[mesh.subMeshCount];

      Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] mesh.subMeshCount for '{mesh.name}' is '{mesh.subMeshCount}'");
      for (int i = 0; i < mesh.subMeshCount; i++) {
        if (i >= materialDefs.Count) {
          Main.Logger.LogWarning($"[PropFactory.BuildMaterialsForRenderer] Not enough supplied material identifiers in prop data to use for submesh '{i}' for mesh '{mesh.name}'. Duplicating existing mat references.");
          materialDefs.Add(materialDefs[UnityEngine.Random.Range(0, materialDefs.Count)]);
        }

        PropMaterialDef propMaterialDef = materialDefs[i];

        if (propMaterialDef.Shader != null) {
          Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with specific shader '{propMaterialDef.Shader}' and texture '{propMaterialDef.Texture}'");
          // Build whole material with specific shader and texture
          if (propModelDef.BundlePath == null) Main.Logger.LogWarning("[PropFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef Shader name but you have no included a bundle for this PropModelDef. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");
          if (propModelDef.BundlePath == null) Main.Logger.LogWarning("[PropFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef Texture name but you have no included a bundle for this PropModelDef. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");

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
          Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Texture but no Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with default shader 'BattleTech Standard' and texture '{propMaterialDef.Texture}'");
          if (propModelDef.BundlePath == null) Main.Logger.LogWarning("[PropFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef Texture name but you have no included a bundle for this PropModelDef. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");

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
          // Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Only material name provided in PropMaterialDef '{propMaterialDef.Name}' so looking for material in bundle first then game data");

          // Look first at bundle for custom bundled Material
          Material mat = propModelDef.BundlePath != null ? AssetBundleLoader.GetAsset<Material>(propModelDef.BundlePath, propMaterialDef.Name) : null;
          if (mat != null) Main.Logger.Log("[PropFactory.BuildMaterialsForRenderer] Found Material in bundle " + mat.name + " after looking for: " + propMaterialDef.Name);

          // Otherwise look at all game Materials
          if (mat == null) mat = matLookup.ContainsKey(propMaterialDef.Name) ? matLookup[propMaterialDef.Name] : placeholderMaterial;
          Main.Logger.Log("[PropFactory.BuildMaterialsForRenderer] Using Material " + mat.name + " after looking for: " + propMaterialDef.Name);

          materials[i] = mat;
        }
      }

      return materials;
    }
  }
}