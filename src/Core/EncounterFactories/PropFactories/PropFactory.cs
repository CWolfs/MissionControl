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

    private struct FitTransform {
      public bool HasRotation;
      public Quaternion Rotation;
      public float HeightScale;
      public Vector3 PivotShift;
      public Vector3 PositionOffset;
    }

    private readonly Dictionary<string, FitTransform> fitTransformCache = new Dictionary<string, FitTransform>(StringComparer.OrdinalIgnoreCase);

    protected static bool HasFitTransformOverrides(PropModelDef propModelDef) {
      if (propModelDef == null) return false;
      Vector3 r = propModelDef.RotationOffset;
      Vector3 p = propModelDef.PositionOffset;
      return Mathf.Abs(r.x) > 1e-4f || Mathf.Abs(r.y) > 1e-4f || Mathf.Abs(r.z) > 1e-4f ||
             Mathf.Abs(p.x) > 1e-4f || Mathf.Abs(p.y) > 1e-4f || Mathf.Abs(p.z) > 1e-4f ||
             propModelDef.TargetHeight > 0.001f;
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
        ? new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)
        : bounds.center;

      Vector3[] vertices = mesh.vertices;
      for (int i = 0; i < vertices.Length; i++) {
        vertices[i] -= shift;
      }

      mesh.vertices = vertices;
      mesh.RecalculateBounds();
      return mesh;
    }

    private Mesh ApplyFitTransform(Mesh oldMesh, FitTransform fit) {
      if (oldMesh == null) return null;
      if (!oldMesh.isReadable) {
        Main.Logger.LogWarning($"[PropFactory.ApplyFitTransform] Mesh '{oldMesh.name}' is not readable; skipping Fit transform");
        return oldMesh;
      }

      Mesh mesh = MonoBehaviour.Instantiate(oldMesh);
      mesh.name = oldMesh.name.Replace("(Clone)", "");

      Vector3[] vertices = mesh.vertices;
      for (int i = 0; i < vertices.Length; i++) {
        Vector3 v = fit.HasRotation ? (fit.Rotation * vertices[i]) : vertices[i];
        vertices[i] = v * fit.HeightScale - fit.PivotShift + fit.PositionOffset;
      }
      mesh.vertices = vertices;

      if (fit.HasRotation) {
        Vector3[] normals = mesh.normals;
        if (normals != null && normals.Length == vertices.Length) {
          for (int i = 0; i < normals.Length; i++) normals[i] = (fit.Rotation * normals[i]).normalized;
          mesh.normals = normals;
        }
        Vector4[] tangents = mesh.tangents;
        if (tangents != null && tangents.Length == vertices.Length) {
          for (int i = 0; i < tangents.Length; i++) {
            Vector3 t = (fit.Rotation * new Vector3(tangents[i].x, tangents[i].y, tangents[i].z)).normalized;
            tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
          }
          mesh.tangents = tangents;
        }
      }

      mesh.RecalculateBounds();
      return mesh;
    }

    private FitTransform GetOrComputeFitTransform(PropModelDef propModelDef, bool isFlimsyBase, Mesh fallbackRefMesh) {
      string cacheKey = $"{propModelDef.Key}|{isFlimsyBase}";
      if (fitTransformCache.TryGetValue(cacheKey, out FitTransform cached)) return cached;

      // Measure LOD0 (or flimsy base) so LODs, collider, glass, and shell share identical Fit bounds (matching MeshNormaliser)
      Mesh refMesh = ResolveRawUnadjustedMesh(propModelDef, $"{propModelDef.MeshName}_LOD0", "LOD0", requireReadable: true)
        ?? ResolveRawUnadjustedMesh(propModelDef, propModelDef.MeshName, "Flimsy", requireReadable: true)
        ?? fallbackRefMesh;

      Vector3 rotDeg = propModelDef.RotationOffset;
      bool hasRot = Mathf.Abs(rotDeg.x) > 1e-4f || Mathf.Abs(rotDeg.y) > 1e-4f || Mathf.Abs(rotDeg.z) > 1e-4f;
      Quaternion rot = hasRot ? Quaternion.Euler(rotDeg) : Quaternion.identity;

      Vector3 rotMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
      Vector3 rotMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
      if (refMesh != null && refMesh.isReadable) {
        Vector3[] verts = refMesh.vertices;
        for (int i = 0; i < verts.Length; i++) {
          Vector3 p = hasRot ? (rot * verts[i]) : verts[i];
          if (p.x < rotMin.x) rotMin.x = p.x; if (p.y < rotMin.y) rotMin.y = p.y; if (p.z < rotMin.z) rotMin.z = p.z;
          if (p.x > rotMax.x) rotMax.x = p.x; if (p.y > rotMax.y) rotMax.y = p.y; if (p.z > rotMax.z) rotMax.z = p.z;
        }
      } else {
        rotMin = Vector3.zero;
        rotMax = Vector3.zero;
      }

      float rawHeight = rotMax.y - rotMin.y;
      float heightScale = (propModelDef.TargetHeight > 0.001f && rawHeight > 1e-5f)
        ? propModelDef.TargetHeight / rawHeight
        : 1f;

      Vector3 sMin = rotMin * heightScale;
      Vector3 sMax = rotMax * heightScale;
      MeshPivotMode pivotMode = propModelDef.GetEffectivePivotMode(isFlimsyBase);
      Vector3 pivotShift = Vector3.zero;
      if (pivotMode == MeshPivotMode.Center) {
        pivotShift = (sMin + sMax) * 0.5f;
      } else if (pivotMode == MeshPivotMode.Bottom) {
        pivotShift = new Vector3((sMin.x + sMax.x) * 0.5f, sMin.y, (sMin.z + sMax.z) * 0.5f);
      }

      var fit = new FitTransform {
        HasRotation = hasRot,
        Rotation = rot,
        HeightScale = heightScale,
        PivotShift = pivotShift,
        PositionOffset = propModelDef.PositionOffset
      };
      fitTransformCache[cacheKey] = fit;
      return fit;
    }

    private Mesh ResolveRawUnadjustedMesh(PropModelDef propModelDef, string meshName, string slot, bool requireReadable) {
      long pathId = propModelDef.GetMeshPathId(slot);

      if (propModelDef.UsesBundleForMesh && !string.IsNullOrEmpty(propModelDef.BundlePath)) {
        LoadAssetBundle(propModelDef);
        Mesh bundleMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, meshName);
        if (bundleMesh != null) {
          bundleMesh.name = bundleMesh.name.Replace("(Clone)", "");
          return bundleMesh;
        }
      }

      if (pathId == 0) {
        Mesh sceneMesh = FindSceneMesh(meshName);
        if (sceneMesh != null && (!requireReadable || sceneMesh.isReadable)) {
          return sceneMesh;
        }
      }

      if (!string.Equals(propModelDef.AssetSource, "Bundle", StringComparison.OrdinalIgnoreCase)) {
        return GameAssetProvider.Instance.TryLoadMesh(
          meshName,
          makeReadable: requireReadable,
          pivotMode: MeshPivotMode.Original,
          sourceAssetFile: propModelDef.SourceAssetFile,
          pathId: pathId);
      }

      return null;
    }

    protected Mesh FindSceneMesh(string meshName) {
      if (allGameMeshes == null || string.IsNullOrEmpty(meshName)) return null;
      for (int i = 0; i < allGameMeshes.Length; i++) {
        Mesh m = allGameMeshes[i];
        if (m != null && m.name == meshName) return m;
      }
      return null;
    }

    protected CustomUnityModelAsset ResolveCustomFileModel(PropModelDef propModelDef, bool makeReadable = true) {
      if (propModelDef == null || !propModelDef.UsesFileForMesh || string.IsNullOrEmpty(propModelDef.ModelFilePath)) {
        return null;
      }
      return CustomModelProvider.Instance.GetOrLoad(
        propModelDef.Key,
        propModelDef.ModelFilePath,
        propModelDef.BuildTransformSettings(),
        propModelDef.GetColliderMode(),
        makeReadable: makeReadable,
        logWarning: msg => Main.Logger.LogWarning(msg));
    }

    protected Mesh ResolveMesh(PropModelDef propModelDef, string meshName, string slot, bool isFlimsyBase, bool makeReadable = false) {
      if (propModelDef == null) return null;

      // 0. Custom .glb / .obj file model (AssetSource = "File" or auto-detected)
      if (propModelDef.UsesFileForMesh) {
        CustomUnityModelAsset fileAsset = ResolveCustomFileModel(propModelDef, makeReadable: true);
        if (fileAsset != null) {
          switch (slot) {
            case "LOD0":
            case "Flimsy": return fileAsset.Lod0Mesh;
            case "LOD1": return fileAsset.Lod1Mesh;
            case "LOD2": return fileAsset.Lod2Mesh;
            case "COL": return fileAsset.ColMesh ?? fileAsset.Lod0Mesh;
            case "Glass": return fileAsset.GlassMesh;
            case "Shell": return fileAsset.ShellMesh;
            case "ShellCOL": return fileAsset.ShellColMesh ?? fileAsset.ShellMesh;
          }
        }
        return null;
      }

      // 1. Bundle or GameData prop with Fit overrides (RotationOffset / PositionOffset / TargetHeight)
      if (HasFitTransformOverrides(propModelDef)) {
        Mesh rawMesh = ResolveRawUnadjustedMesh(propModelDef, meshName, slot, requireReadable: true);
        if (rawMesh == null) return null;
        FitTransform fit = GetOrComputeFitTransform(propModelDef, isFlimsyBase, rawMesh);
        return ApplyFitTransform(rawMesh, fit);
      }

      MeshPivotMode pivotMode = propModelDef.GetEffectivePivotMode(isFlimsyBase);
      long pathId = propModelDef.GetMeshPathId(slot);

      // 2. Custom mod AssetBundle (when configured)
      if (propModelDef.UsesBundleForMesh && !string.IsNullOrEmpty(propModelDef.BundlePath)) {
        LoadAssetBundle(propModelDef);
        Mesh bundleMesh = AssetBundleLoader.GetAsset<Mesh>(propModelDef.BundlePath, meshName);
        if (bundleMesh != null) {
          bundleMesh.name = bundleMesh.name.Replace("(Clone)", "");
          return pivotMode != MeshPivotMode.Original ? ApplyMeshPivotMode(bundleMesh, pivotMode) : bundleMesh;
        }
      }

      // 3. Active scene memory (zero-I/O fast path when no specific PathId override is set)
      if (pathId == 0) {
        Mesh sceneMesh = FindSceneMesh(meshName);
        if (sceneMesh != null) {
          return pivotMode != MeshPivotMode.Original ? ApplyMeshPivotMode(sceneMesh, pivotMode) : sceneMesh;
        }
      }

      // 4. Runtime GameData reader (BattleTech_Data/sharedassets*.assets)
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
      if (propModelDef.GetColliderMode() == BTAssetReader.Core.Models.CoreColliderMode.Convex) {
        buildingCOLCollider.convex = true;
      }
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
        string propertyType = propertyName.Equals("shaderwords", StringComparison.OrdinalIgnoreCase)
          ? null
          : (propertyNameAndTypeSplit.Length > 1 ? propertyNameAndTypeSplit[1].ToLower() : null);
        JToken propertyValue = kvPair.Value;

        if (propertyName.Equals("tag", StringComparison.OrdinalIgnoreCase) && propertyNameAndTypeSplit.Length > 1) {
          string tagName = propertyNameAndType.Substring(propertyName.Length + 1);
          material.SetOverrideTag(tagName, propertyValue?.ToString() ?? "");
          continue;
        }

        if (propertyType == "bool") {
          bool boolVal = propertyValue != null && propertyValue.Type == JTokenType.Boolean
            ? (bool)propertyValue
            : bool.TryParse(propertyValue?.ToString(), out bool parsedBool) && parsedBool;
          if (propertyName.Equals("enableinstancing", StringComparison.OrdinalIgnoreCase)) {
            material.enableInstancing = boolVal;
          } else if (propertyName.Equals("doublesidedgi", StringComparison.OrdinalIgnoreCase)) {
            material.doubleSidedGI = boolVal;
          }
          continue;
        }

        if (propertyType == "texture") {
          string textureName = propertyValue?.ToString();
          if (string.IsNullOrWhiteSpace(textureName)) {
            material.SetTexture(propertyName, null);
            continue;
          }

          // First look for Texture in bundle
          Texture texture = bundleItem?.BundlePath != null ? AssetBundleLoader.GetAsset<Texture>(bundleItem.BundlePath, textureName) : null;

          // Otherwise look for texture in game memory, then BattleTech_Data
          if (texture == null) texture = UnityGameInstance.Instance.Game.DataManager.TextureManager.GetLoadedTexture(textureName);
          if (texture == null) texture = GameAssetProvider.Instance.TryLoadTexture(textureName);

          material.SetTexture(propertyName, texture);
        } else if (propertyType == "textureoffset") {
          JArray propertyValueArray = propertyValue.ToObject<JArray>();
          Vector2 offset = new Vector2((float)propertyValueArray[0], (float)propertyValueArray[1]);
          material.SetTextureOffset(propertyName, offset);
        } else if (propertyType == "texturescale") {
          JArray propertyValueArray = propertyValue.ToObject<JArray>();
          Vector2 scale = new Vector2((float)propertyValueArray[0], (float)propertyValueArray[1]);
          material.SetTextureScale(propertyName, scale);
        } else if (propertyType == "float") {
          material.SetFloat(propertyName, (float)propertyValue);
        } else if (propertyType == "int") {
          int intVal = (int)propertyValue;
          if (propertyName.Equals("renderqueue", StringComparison.OrdinalIgnoreCase)) {
            material.renderQueue = intVal;
          } else {
            material.SetInt(propertyName, intVal);
          }
        } else if (propertyType == "vector") {
          JArray propertyValueArray = propertyValue.ToObject<JArray>();
          Vector4 vector = new Vector4((float)propertyValueArray[0], (float)propertyValueArray[1], (float)propertyValueArray[2], (float)propertyValueArray[3]);
          material.SetVector(propertyName, vector);
        } else if (propertyType == "color") {
          Color color = ParseMaterialColor(propertyValue);
          material.SetColor(propertyName, color);
        } else if (propertyName.Equals("shaderwords", StringComparison.OrdinalIgnoreCase)) {
          string[] shaderwords = propertyValue.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
          foreach (string rawWord in shaderwords) {
            string word = rawWord.Trim();
            if (string.IsNullOrEmpty(word)) continue;
            if (word.StartsWith("-") && word.Length > 1) {
              material.DisableKeyword(word.Substring(1).Trim());
            } else {
              material.EnableKeyword(word);
            }
          }
        }
      }
    }

    private static Color ParseMaterialColor(JToken token) {
      if (token is JArray arr && arr.Count >= 3) {
        float r = (float)arr[0];
        float g = (float)arr[1];
        float b = (float)arr[2];
        float a = arr.Count >= 4 ? (float)arr[3] : 1f;
        return new Color(r, g, b, a);
      }
      if (token != null && ColorUtility.TryParseHtmlString(token.ToString().Trim(), out Color parsed)) {
        return parsed;
      }
      return Color.white;
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

      CustomUnityModelAsset fileAsset = bundleItem is PropModelDef pmd && pmd.UsesFileForMesh
        ? ResolveCustomFileModel(pmd, makeReadable: true)
        : null;

      if (materialDefs == null || materialDefs.Count == 0) {
        if (fileAsset != null && fileAsset.EmbeddedMaterials != null && fileAsset.EmbeddedMaterials.Length > 0) {
          for (int i = 0; i < meshSubcount; i++) {
            materials[i] = fileAsset.EmbeddedMaterials[Math.Min(i, fileAsset.EmbeddedMaterials.Length - 1)] ?? fallbackMaterial;
          }
          return materials;
        }
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
        } else if (fileAsset != null && fileAsset.EmbeddedMaterials != null && i < fileAsset.EmbeddedMaterials.Length) {
          materials[i] = fileAsset.EmbeddedMaterials[i] ?? fallbackMaterial;
          continue;
        } else {
          Main.Logger.LogWarning($"[PropFactory.BuildMaterialsForRenderer] Not enough supplied material identifiers in prop data to use for submesh '{i}' for mesh '{mesh?.name ?? "null"}'. Duplicating existing mat references.");
          propMaterialDef = materialDefs[UnityEngine.Random.Range(0, materialDefs.Count)];
        }

        if (propMaterialDef == null) {
          materials[i] = fallbackMaterial;
          continue;
        }

        if (string.Equals(propMaterialDef.Name, "$embedded", StringComparison.OrdinalIgnoreCase)) {
          Material baseEmbedded = (fileAsset != null && fileAsset.EmbeddedMaterials != null && fileAsset.EmbeddedMaterials.Length > 0)
            ? fileAsset.EmbeddedMaterials[Math.Min(i, fileAsset.EmbeddedMaterials.Length - 1)]
            : null;
          if (baseEmbedded == null) {
            materials[i] = fallbackMaterial;
          } else if (string.IsNullOrEmpty(propMaterialDef.Shader) && (propMaterialDef.MaterialProperties == null || !propMaterialDef.MaterialProperties.HasValues)) {
            materials[i] = baseEmbedded;
          } else {
            string propSig = propMaterialDef.MaterialProperties != null
              ? propMaterialDef.MaterialProperties.ToString(Newtonsoft.Json.Formatting.None)
              : "";
            string cacheKey = $"$embedded:{(bundleItem is PropModelDef embModel ? embModel.Key : "prop")}:{i}:{propMaterialDef.Shader ?? ""}:{propSig}";
            if (!MissionControl.Instance.GeneratedMaterials.TryGetValue(cacheKey, out Material overridden) || overridden == null) {
              overridden = new Material(baseEmbedded) { name = $"{baseEmbedded.name}_Override" };
              if (!string.IsNullOrEmpty(propMaterialDef.Shader)) {
                Shader shader = bundleItem?.BundlePath != null ? AssetBundleLoader.GetAsset<Shader>(bundleItem.BundlePath, propMaterialDef.Shader) : null;
                if (shader == null) shader = UnityMaterialBuilder.ResolveShader(propMaterialDef.Shader, Application.dataPath, Main.Logger.LogWarning);
                if (shader != null) {
                  overridden.shader = shader;
                } else {
                  Main.Logger.LogWarning($"[PropFactory.BuildMaterialsForRenderer] Couldn't find override Shader '{propMaterialDef.Shader}' for embedded slot {i}; keeping '{overridden.shader?.name}'.");
                }
              }
              SetMaterialProperties(bundleItem, overridden, propMaterialDef.MaterialProperties);
              MissionControl.Instance.GeneratedMaterials[cacheKey] = overridden;
            }
            materials[i] = overridden;
          }
          continue;
        }

        if (!string.IsNullOrEmpty(propMaterialDef.Shader)) {
          Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Shader provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with specific shader '{propMaterialDef.Shader}' and MaterialProperties '{propMaterialDef.MaterialProperties}'");
          Material material = null;

          // Check for cached generated Material first
          if (MissionControl.Instance.GeneratedMaterials.ContainsKey(propMaterialDef.Name)) {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Using cached runtime generated material '{propMaterialDef.Name}'");
            material = MissionControl.Instance.GeneratedMaterials[propMaterialDef.Name];
          } else {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Generating cached runtime generated material '{propMaterialDef.Name}'");

            // First look for Shader in bundle
            Shader shader = bundleItem?.BundlePath != null ? AssetBundleLoader.GetAsset<Shader>(bundleItem.BundlePath, propMaterialDef.Shader) : null;

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
          Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Texture/properties provided for PropMaterialDef '{propMaterialDef.Name}' so building Material with default shader 'BattleTech Standard'");
          Material material = null;

          // Check for cached generated Material first
          if (MissionControl.Instance.GeneratedMaterials.ContainsKey(propMaterialDef.Name)) {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Using cached runtime generated material '{propMaterialDef.Name}'");
            material = MissionControl.Instance.GeneratedMaterials[propMaterialDef.Name];
          } else {
            Main.Logger.Log($"[PropFactory.BuildMaterialsForRenderer] Generating cached runtime generated material '{propMaterialDef.Name}'");

            // Build whole material with specific properties using 'BattleTech Standard' shader
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

    protected GameObject CreatePropLights(GameObject parentGO, PropModelDef propModelDef) {
      if (parentGO == null || propModelDef == null) return null;

      List<PropLightDef> lights = propModelDef.Lights;
      if (lights == null && propModelDef.UsesFileForMesh) {
        CustomUnityModelAsset fileAsset = ResolveCustomFileModel(propModelDef, makeReadable: true);
        if (fileAsset?.EmbeddedLights != null && fileAsset.EmbeddedLights.Count > 0) {
          lights = fileAsset.EmbeddedLights.Select(PropLightDef.FromCore).ToList();
        }
      }

      if (lights == null || lights.Count == 0) return null;

      GameObject lightsParent = CreateGameObject(parentGO, "_lights");
      for (int i = 0; i < lights.Count; i++) {
        PropLightDef lDef = lights[i];
        if (lDef == null) continue;

        GameObject lightGO = CreateGameObject(lightsParent, string.IsNullOrEmpty(lDef.Name) ? $"Light_{i}" : lDef.Name);
        lightGO.transform.localPosition = lDef.Position;
        lightGO.transform.localEulerAngles = lDef.Rotation;

        BTLight btLight = lightGO.AddComponent<BTLight>();
        btLight.lightType = ParseBTLightType(lDef.Type);
        btLight.radius = Mathf.Max(0.5f, lDef.Radius);
        btLight.intensity = Mathf.Max(0.01f, lDef.Intensity) * 3000f;
        btLight.lightColor = lDef.GetUnityColor();
        btLight.spotlightAngleInner = Mathf.Clamp(lDef.InnerAngle, 1f, 89f);
        btLight.spotlightAngleOuter = Mathf.Clamp(lDef.OuterAngle, btLight.spotlightAngleInner + 0.5f, 90f);
        btLight.length = Mathf.Max(0.1f, lDef.Length);
        btLight.castShadows = lDef.CastShadows;
        btLight.contributeSpecular = lDef.ContributeSpecular;
        btLight.contributeVolumetrics = lDef.ContributeVolumetrics;
        btLight.RefreshLightSettings(fullRefresh: true);
      }

      return lightsParent;
    }

    private static BTLight.LightTypes ParseBTLightType(string type) {
      if (string.Equals(type, "Spot", StringComparison.OrdinalIgnoreCase)) return BTLight.LightTypes.Spot;
      if (string.Equals(type, "Linear", StringComparison.OrdinalIgnoreCase)) return BTLight.LightTypes.Linear;
      if (string.Equals(type, "Area", StringComparison.OrdinalIgnoreCase)) return BTLight.LightTypes.Area;
      return BTLight.LightTypes.Point;
    }
  }
}