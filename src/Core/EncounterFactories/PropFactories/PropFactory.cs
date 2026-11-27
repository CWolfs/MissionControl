using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;

using MissionControl.Data;

using Newtonsoft.Json.Linq;

using BTAssetReader.Unity;

namespace MissionControl.EncounterFactories {
  public abstract class PropFactory {
    private static GameObject mcPropGroupParent = null;
    public static GameObject MCPropGroupParent {
      get {
        if (mcPropGroupParent == null) {
          mcPropGroupParent = new GameObject("MCPropGroupParent");
          mcPropGroupParent.transform.SetParent(MissionControl.Instance.EncounterLayerData.transform, true);
          mcPropGroupParent.transform.position = Vector3.zero;
        }

        return mcPropGroupParent;
      }
    }

    private static GameObject mcGenericStaticDestruct = null;
    public static GameObject MCGenericStaticDestruct {
      get {
        if (mcGenericStaticDestruct == null) {
          mcGenericStaticDestruct = new GameObject("MCGenericStaticDestruct");
          mcGenericStaticDestruct.transform.SetParent(MissionControl.Instance.EncounterLayerData.transform, true);
          mcGenericStaticDestruct.transform.position = Vector3.zero;
        }

        return mcGenericStaticDestruct;
      }
    }

    public static bool IsCustomDestructionObject(GameObject obj) {
      return obj != null && mcGenericStaticDestruct != null && obj.transform.IsChildOf(mcGenericStaticDestruct.transform);
    }

    protected static string GenericStaticDestructName { get; set; } = "generic_static_destruct";

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
    protected Mesh buildingGlassMesh = null;

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
      gameObject.transform.SetParent(parent.transform, false);

      return gameObject;
    }

    protected LODGroup SetupLODGroup(GameObject targetGO) {
      // Setup LOD Group
      LODGroup lodGroup = targetGO.AddComponent<LODGroup>();
      lodGroup.animateCrossFading = true;
      lodGroup.fadeMode = LODFadeMode.CrossFade;

      MeshRenderer lod0MR = targetGO.transform.Find($"{targetGO.name}_LOD0")?.GetComponent<MeshRenderer>();
      MeshRenderer lod1MR = targetGO.transform.Find($"{targetGO.name}_LOD1")?.GetComponent<MeshRenderer>();
      MeshRenderer lod2MR = targetGO.transform.Find($"{targetGO.name}_LOD2")?.GetComponent<MeshRenderer>();

      if (lod0MR == null) lod0MR = targetGO.transform.Find(targetGO.name)?.GetComponent<MeshRenderer>();

      if (lod0MR == null) {
        Main.Logger.LogError("[PropFactory.SetupLODGroup] Couldn't find the LOD0 / main flimsy GameObject or its MeshRenderer. This should not happen. It may be due to a badly named bundled Mesh that isn't named correctly");
      }

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

      if (lod1MR != null && lod2MR != null) {
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
      if (mf == null || mf.sharedMesh == null || boxCollider == null) return;

      Bounds meshBounds = mf.sharedMesh.bounds;
      boxCollider.center = meshBounds.center;
      boxCollider.size = meshBounds.size;
    }

    protected void LoadAssetBundle(PropModelDef propModelDef) {
      if (!AssetBundleLoader.HasAlreadyLoadedBundle(propModelDef.BundlePath)) {
        AssetBundleLoader.LoadPropBundle(propModelDef.BundlePath);
      }
    }

    protected DamageAssetGroup GetRandomDamageAssetGroup() {
      if (allDamageAssetGroups == null || allDamageAssetGroups.Length == 0) {
        Main.Logger.LogWarning("[PropFactory.GetRandomDamageAssetGroup] No DamageAssetGroup objects found in loaded scene.");
        return null;
      }
      return allDamageAssetGroups[UnityEngine.Random.Range(0, allDamageAssetGroups.Length)];
    }

    protected Mesh CenterMeshPivot(Mesh oldMesh) {
      return ApplyMeshPivotMode(oldMesh, MeshPivotMode.Center);
    }

    protected Mesh ApplyMeshPivotMode(Mesh oldMesh, MeshPivotMode pivotMode) {
      if (oldMesh == null || pivotMode == MeshPivotMode.Original) return oldMesh;
      if (!oldMesh.isReadable) {
        Main.Logger.LogWarning($"[PropFactory.ApplyMeshPivotMode] Mesh '{oldMesh.name}' is not readable; skipping pivot adjustment ({pivotMode})");
        return oldMesh;
      }

      Main.Logger.Log($"[PropFactory.ApplyMeshPivotMode] Adjusting pivot ({pivotMode}) on mesh '{oldMesh.name}'");

      // Copy the mesh to not alter the original one
      Mesh mesh = MonoBehaviour.Instantiate(oldMesh);
      Bounds bounds = mesh.bounds;
      Vector3 shift = pivotMode == MeshPivotMode.Bottom
        ? new Vector3(0f, bounds.min.y, 0f)
        : bounds.center;

      Vector3[] vertices = mesh.vertices;
      for (int i = 0; i < vertices.Length; i++) {
        vertices[i] -= shift;
      }

      mesh.vertices = vertices;
      mesh.RecalculateBounds();
      return mesh;
    }

    protected Mesh FindSceneMesh(string meshName) {
      if (allGameMeshes == null || string.IsNullOrEmpty(meshName)) return null;
      for (int i = 0; i < allGameMeshes.Length; i++) {
        Mesh m = allGameMeshes[i];
        if (m != null && m.name == meshName) return m;
      }
      return null;
    }

    protected Mesh ResolveMesh(PropModelDef propModelDef, string meshName, string slot, bool isFlimsyBase, bool makeReadable = false) {
      if (propModelDef == null) return null;
      MeshPivotMode pivotMode = propModelDef.GetEffectivePivotMode(isFlimsyBase);
      long pathId = propModelDef.GetMeshPathId(slot);

      // 1. Custom mod AssetBundle (when configured)
      if (propModelDef.UsesBundleForMesh && !string.IsNullOrEmpty(propModelDef.BundlePath)) {
        LoadAssetBundle(propModelDef);
        Mesh bundleMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, meshName);
        if (bundleMesh != null) {
          bundleMesh.name = bundleMesh.name.Replace("(Clone)", "");
          return pivotMode != MeshPivotMode.Original ? ApplyMeshPivotMode(bundleMesh, pivotMode) : bundleMesh;
        }
      }

      // 2. Active scene memory (zero-I/O fast path when no specific PathId override is set)
      if (pathId == 0) {
        Mesh sceneMesh = FindSceneMesh(meshName);
        if (sceneMesh != null) {
          return pivotMode != MeshPivotMode.Original ? ApplyMeshPivotMode(sceneMesh, pivotMode) : sceneMesh;
        }
      }

      // 3. Runtime GameData reader (BattleTech_Data/sharedassets*.assets)
      if (!string.Equals(propModelDef.AssetSource, "Bundle", StringComparison.OrdinalIgnoreCase)) {
        Mesh gameDataMesh = GameAssetProvider.Instance.TryLoadMesh(
          meshName,
          makeReadable: makeReadable,
          pivotMode: pivotMode,
          sourceAssetFile: propModelDef.SourceAssetFile,
          pathId: pathId);
        if (gameDataMesh != null) {
          return gameDataMesh;
        }
      }

      return null;
    }

    protected void CreateColAndLODs(GameObject buildingGO, PropModelDef propModelDef) {
      bool isFlimsyBase = false;

      buildingCOLMesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_COL", "COL", isFlimsyBase: false);
      buildingLOD0Mesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_LOD0", "LOD0", isFlimsyBase: false);
      buildingLOD1Mesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_LOD1", "LOD1", isFlimsyBase: false);
      buildingLOD2Mesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_LOD2", "LOD2", isFlimsyBase: false);

      if (buildingCOLMesh == null && buildingLOD0Mesh == null) {
        Mesh flimsyStyledMesh = ResolveMesh(propModelDef, propModelDef.MeshName, "Flimsy", isFlimsyBase: true);
        if (flimsyStyledMesh != null) {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] Found a flimsy base for '{propModelDef.Key}' so using that for COL, LOD0, LOD1, LOD2");
          buildingCOLMesh = flimsyStyledMesh;
          buildingLOD0Mesh = flimsyStyledMesh;
          buildingLOD1Mesh = flimsyStyledMesh;
          buildingLOD2Mesh = flimsyStyledMesh;
          isFlimsyBase = true;
        }
      } else if (buildingCOLMesh == null && buildingLOD0Mesh != null) {
        Main.Logger.LogWarning($"[PropFactory.CreateColAndLODs] No dedicated _COL mesh for '{propModelDef.Key}'; using LOD0 mesh as collider fallback");
        buildingCOLMesh = buildingLOD0Mesh;
      }

      if (buildingCOLMesh == null || buildingLOD0Mesh == null) {
        Main.Logger.LogError("[PropFactory.CreateColAndLODs] COL or LOD0 Mesh is null for '" + buildingGO.name + "' (Mesh: '" + propModelDef.MeshName + "').");
        throw new Exception("COL or LOD0 Mesh is null for '" + buildingGO.name + "' (Mesh: '" + propModelDef.MeshName + "').");
      }

      GameObject buildingCOLGO = CreateGameObject(buildingGO, $"{buildingGO.name}_COL");
      MeshCollider buildingCOLCollider = buildingCOLGO.AddComponent<MeshCollider>();
      buildingCOLCollider.sharedMesh = buildingCOLMesh;
      buildingCOLGO.layer = 12; // so raycasts can hit it for highlight effect and selection
      // ApplyScale(buildingCOLGO);

      if (propModelDef.MeshOffsets.ContainsKey("COL")) {
        Main.Logger.Log("[PropFactory.CreateColAndLODs] Apply mesh offset for COL");
        buildingCOLGO.transform.localPosition += propModelDef.MeshOffsets["COL"];
      }

      GameObject buildingLOD0GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD0");
      MeshFilter buildingLOD0MF = buildingLOD0GO.AddComponent<MeshFilter>();
      MeshRenderer buildingLOD0MR = buildingLOD0GO.AddComponent<MeshRenderer>();

      Material[] materials = BuildMaterialsForRenderer(buildingLOD0Mesh, propModelDef, propModelDef.Materials, placeholderMaterial);
      buildingLOD0MR.materials = materials;
      buildingLOD0MF.mesh = buildingLOD0Mesh;
      // ApplyScale(buildingLOD0GO);

      if (propModelDef.MeshOffsets.ContainsKey("LOD0")) {
        Main.Logger.Log("[PropFactory.CreateColAndLODs] Apply mesh offset for LOD0");
        buildingLOD0GO.transform.localPosition += propModelDef.MeshOffsets["LOD0"];
      }

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

          if (propModelDef.MeshOffsets.ContainsKey("LOD1")) {
            Main.Logger.Log("[PropFactory.CreateColAndLODs] Apply mesh offset for LOD1");
            buildingLOD1GO.transform.localPosition += propModelDef.MeshOffsets["LOD1"];
          }
        } else {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] No LOD1 mesh found under name '{propModelDef.MeshName}_LOD1' so skipping LOD1 GO build");
        }

        if (buildingLOD1Mesh != null && buildingLOD2Mesh != null) {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] Building LOD2 with mesh '{propModelDef.MeshName}_LOD2'");

          GameObject buildingLOD2GO = CreateGameObject(buildingGO, $"{buildingGO.name}_LOD2");
          MeshFilter buildingLOD2MF = buildingLOD2GO.AddComponent<MeshFilter>();
          MeshRenderer buildingLOD2MR = buildingLOD2GO.AddComponent<MeshRenderer>();
          buildingLOD2MR.materials = materials;
          buildingLOD2MF.mesh = buildingLOD2Mesh;

          if (propModelDef.MeshOffsets.ContainsKey("LOD2")) {
            Main.Logger.Log("[PropFactory.CreateColAndLODs] Apply mesh offset for LOD2");
            buildingLOD2GO.transform.localPosition += propModelDef.MeshOffsets["LOD2"];
          }
        } else {
          Main.Logger.Log($"[PropFactory.CreateColAndLODs] No LOD2 mesh found under name '{propModelDef.MeshName}_LOD1' so skipping LOD2 GO build");
        }
      }
    }

    private void SetMaterialProperties(IBundleItem bundleItem, Material material, JObject materialProperties) {
      if (materialProperties == null) return;

      foreach (KeyValuePair<string, JToken> kvPair in materialProperties) {
        string propertyNameAndType = kvPair.Key;
        string[] propertyNameAndTypeSplit = kvPair.Key.Split('.');

        string propertyName = propertyNameAndTypeSplit[0];
        string propertyType = propertyName == "shaderwords" ? null : propertyNameAndTypeSplit[1].ToLower();
        JToken propertyValue = kvPair.Value;

        if (propertyType == "texture") {
          // Grab, load and set texture
          // First look for Texture in bundle
          string textureName = propertyValue.ToString();
          Texture texture = bundleItem.BundlePath != null ? AssetBundleLoader.GetAsset<Texture>(bundleItem.BundlePath, textureName) : null;

          // Otherwise look for texture in game memory, then BattleTech_Data
          if (texture == null) texture = UnityGameInstance.Instance.Game.DataManager.TextureManager.GetLoadedTexture(textureName);
          if (texture == null) texture = GameAssetProvider.Instance.TryLoadTexture(textureName);

          material.SetTexture(propertyName, texture);
        } else if (propertyType == "textureoffset") {
          JArray propertyValueArray = propertyValue.ToObject<JArray>();
          Vector2 offset = new Vector2((float)propertyValueArray[0], (float)propertyValueArray[1]);
          material.SetTextureOffset(propertyName, offset);
        } else if (propertyType == "float") {
          material.SetFloat(propertyName, (float)propertyValue);
        } else if (propertyType == "int") {
          material.SetInt(propertyName, (int)propertyValue);
        } else if (propertyType == "vector") {
          JArray propertyValueArray = propertyValue.ToObject<JArray>();
          Vector4 vector = new Vector4((float)propertyValueArray[0], (float)propertyValueArray[1], (float)propertyValueArray[2], (float)propertyValueArray[3]);
          material.SetVector(propertyName, vector);
        } else if (propertyName == "shaderwords") {
          string[] shaderwords = propertyValue.ToString().Split(',');
          foreach (string shaderword in shaderwords) {
            material.EnableKeyword(shaderword);
          }
        }
      }
    }

    protected Material BuildMaterialForRenderer(Mesh mesh, IBundleItem bundleItem, PropMaterialDef materialDef) {
      if (materialDef == null) return placeholderMaterial;
      return BuildMaterialsForRenderer(mesh, bundleItem, new List<PropMaterialDef> { materialDef }, placeholderMaterial)[0];
    }

    protected Material[] BuildMaterialsForRenderer(Mesh mesh, IBundleItem bundleItem, List<PropMaterialDef> materialDefs, Material placeholderMaterial) {
      Material fallbackMaterial = placeholderMaterial ?? PropFactory.placeholderMaterial;
      Material[] materials = null;
      int meshSubcount = 1;

      if (mesh == null) {
        Main.Logger.Log("[PropFactory.BuildMaterialsForRenderer] No mesh provided. Decal usage mode. Using placeholder material");
        materials = new Material[1] { fallbackMaterial };
      } else {
        Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] mesh.subMeshCount for '{mesh.name}' is '{mesh.subMeshCount}'");
        materials = new Material[mesh.subMeshCount];
        meshSubcount = mesh.subMeshCount;
      }

      if (materialDefs == null || materialDefs.Count == 0) {
        Main.Logger.LogWarning($"[PropFactory.BuildMaterialsForRenderer] No material definitions supplied for mesh '{mesh?.name ?? "null"}'. Using placeholder material.");
        for (int i = 0; i < meshSubcount; i++) {
          materials[i] = fallbackMaterial;
        }
        return materials;
      }

      for (int i = 0; i < meshSubcount; i++) {
        PropMaterialDef propMaterialDef;
        if (i < materialDefs.Count) {
          propMaterialDef = materialDefs[i];
        } else {
          Main.Logger.LogWarning($"[PropFactory.BuildMaterialsForRenderer] Not enough supplied material identifiers in prop data to use for submesh '{i}' for mesh '{mesh?.name ?? "null"}'. Duplicating existing mat references.");
          propMaterialDef = materialDefs[UnityEngine.Random.Range(0, materialDefs.Count)];
        }

        if (propMaterialDef == null) {
          materials[i] = fallbackMaterial;
          continue;
        }

        if (propMaterialDef.Shader != null) {
          Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with specific shader '{propMaterialDef.Shader}' and MaterialProperties '{propMaterialDef.MaterialProperties}'");
          // Build whole material with specific shader and texture
          if (bundleItem.BundlePath == null) Main.Logger.LogWarning("[PropFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef Shader name but you have not included a bundle for this IBundledItem. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");

          Material material = null;

          // Check for cached generated Material first
          if (MissionControl.Instance.GeneratedMaterials.ContainsKey(propMaterialDef.Name)) {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Using cached runtime generated material '{propMaterialDef.Name}'");
            material = MissionControl.Instance.GeneratedMaterials[propMaterialDef.Name];
          } else {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Generating cached runtime generated material '{propMaterialDef.Name}'");

            // First look for Shader in bundle
            Shader shader = bundleItem.BundlePath != null ? AssetBundleLoader.GetAsset<Shader>(bundleItem.BundlePath, propMaterialDef.Shader) : null;

            // Otherwise look for Shader in game (and fallback shaders AssetBundle)
            if (shader == null) shader = UnityMaterialBuilder.ResolveShader(propMaterialDef.Shader, Application.dataPath, Main.Logger.LogWarning);
            if (shader == null) {
              Main.Logger.LogError("[PropFactory.BuildMaterialsForRenderer] Couldn't find Shader in bundle or game data for PropMaterialDef " + propMaterialDef.Name + ". It's possible this Map does not have it loaded in memory.");
              material = fallbackMaterial;
            } else {
              material = new Material(shader);
              material.name = propMaterialDef.Name;
              JObject materialProperties = propMaterialDef.MaterialProperties;
              SetMaterialProperties(bundleItem, material, materialProperties);

              MissionControl.Instance.GeneratedMaterials.Add(propMaterialDef.Name, material);
            }
          }

          materials[i] = material;
        } else if (propMaterialDef.MaterialProperties != null) {
          Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Texture but no Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with default shader 'BattleTech Standard' and MaterialProperties '{propMaterialDef.MaterialProperties.ToString()}'");
          if (bundleItem.BundlePath == null) Main.Logger.LogWarning("[PropFactory.BuildMaterialsForRenderer] You have specified a PropMaterialDef MaterialProperties but you have no included a bundle for this PropModelDef. This could be correct if you intend to reference a preloaded Shader but if you intend to load a custom shader from your bundle - there is no bundle loaded");

          Material material = null;

          // Check for cached generated Material first
          if (MissionControl.Instance.GeneratedMaterials.ContainsKey(propMaterialDef.Name)) {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Using cached runtime generated material '{propMaterialDef.Name}'");
            material = MissionControl.Instance.GeneratedMaterials[propMaterialDef.Name];
          } else {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Generating cached runtime generated material '{propMaterialDef.Name}'");

            // Build whole material with specifix texture but use 'BattleTech Standard' shader
            Shader shader = Shader.Find("BattleTech Standard");

            material = new Material(shader);
            material.name = propMaterialDef.Name;
            JObject materialProperties = propMaterialDef.MaterialProperties;
            SetMaterialProperties(bundleItem, material, materialProperties);

            MissionControl.Instance.GeneratedMaterials.Add(propMaterialDef.Name, material);
          }

          materials[i] = material;
        } else {
          // Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Only material name provided in PropMaterialDef '{propMaterialDef.Name}' so looking for material in bundle first then game data");

          // Look first at bundle for custom bundled Material
          Material mat = bundleItem.BundlePath != null ? AssetBundleLoader.GetAsset<Material>(bundleItem.BundlePath, propMaterialDef.Name) : null;
          if (mat != null) Main.Logger.Log("[PropFactory.BuildMaterialsForRenderer] Found Material in bundle " + mat.name + " after looking for: " + propMaterialDef.Name);

          // Next look at active scene Materials (fast path)
          if (mat == null && matLookup != null && matLookup.ContainsKey(propMaterialDef.Name)) {
            mat = matLookup[propMaterialDef.Name];
          }

          // Next resolve from BattleTech_Data at runtime via BTAssetReader
          if (mat == null) {
            mat = GameAssetProvider.Instance.TryLoadMaterial(propMaterialDef.Name);
            if (mat != null && matLookup != null) {
              matLookup[propMaterialDef.Name] = mat;
            }
          }

          if (mat == null) mat = fallbackMaterial;
          Main.Logger.Log("[PropFactory.BuildMaterialsForRenderer] Using Material " + (mat != null ? mat.name : "null") + " after looking for: " + propMaterialDef.Name);

          materials[i] = mat;
        }
      }

      return materials;
    }
  }
}