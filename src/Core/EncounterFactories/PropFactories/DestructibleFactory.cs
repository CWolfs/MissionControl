using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering;

using MissionControl.Data;
using MissionControl.Utils;

namespace MissionControl.EncounterFactories {
  public class DestructibleFactory : PropFactory {
    private string destructibleName = "UNNAMED";

    private PropFlimsyDef PropFlimsyDef { get; set; }

    private GameObject destructibleFlimsyGroupGO;

    // private GameObject destructDecalParent;
    private GameObject destructSplit;
    private GameObject destructShell;

    public DestructibleFactory(PropFlimsyDef propFlimsyDef) {
      PropFlimsyDef = propFlimsyDef;
    }

    private GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.parent = parent.transform;
      gameObject.transform.localPosition = Vector3.zero;

      return gameObject;
    }

    private GameObject CreateDestructibleFlimsyGroup(string name) {
      this.destructibleFlimsyGroupGO = CreateGameObject(DestructibleFactory.MCDestructibleParent, name);
      destructibleFlimsyGroupGO.SetActive(false);

      destructibleFlimsyGroupGO.AddComponent<SnapToTerrain>();

      // CreateDestructible(this.destructibleFlimsyGroupGO, $"Destructible_{destructibleName}");

      DestructibleFlimsyGroup destructibleFlimsyGroup = this.destructibleFlimsyGroupGO.AddComponent<DestructibleFlimsyGroup>();
      destructibleFlimsyGroup.BakeDestructionAssets();

      destructibleFlimsyGroupGO.SetActive(true);
      return this.destructibleFlimsyGroupGO;
    }

    private GameObject CreateDestructible(GameObject parentGO, string name) {
      PropModelDef propModelDef = PropFlimsyDef.GetPropModelDef();

      DestructibleFlimsyGroup destructibleFlimsyGroup = parentGO.GetComponent<DestructibleFlimsyGroup>();

      GameObject destructibleGO = CreateGameObject(parentGO, name);
      destructibleGO.SetActive(false);

      // CreateColAndLODs(destructibleGO, propModelDef);
      CreateDestructibleFlimsy(destructibleGO, PropFlimsyDef);

      DestructibleObject destructibleObject = destructibleGO.AddComponent<DestructibleObject>();
      destructibleObject.destructType = DestructibleObject.DestructType.flimsyGroup;
      destructibleObject.structSize = propModelDef.DestructibleSize;  // TODO: Check if this should change for destructible flimsies - always a certain size?
      destructibleObject.structMaterial = propModelDef.DestructibleMaterial;
      destructibleObject.flimsyDestructType = propModelDef.FlimsyDestructibleType; // TODO: Check if this should change for destructible flimsies - always a certain size?
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.embeddedFlimsyChildren = new List<DestructibleObject>();
      destructibleObject.shellInstance = destructShell;
      destructibleObject.damageAssetGroup = allDamageAssetGroups[UnityEngine.Random.Range(0, allDamageAssetGroups.Length)];
      destructibleObject.decalObjects = new List<GameObject>();
      destructibleObject.decalSpawners = new List<BTDecalSpawner>();
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.destructibleFlimsyGroup = destructibleFlimsyGroup;
      destructibleObject.destructList = destructibleObject.GetComponentsInChildren<DestructibleObject>().ToList();

      // Setup LOD Group
      LODGroup lodGroup = SetupLODGroup(destructibleGO);

      destructibleGO.SetActive(true);
      return destructibleGO;
    }

    private void CreateDestructibleFlimsy(GameObject flimsyParentGO, PropFlimsyDef propFlimsyDef) {
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

    private void AttachFlimsyMesh(GameObject flimsyGO, PropFlimsyDef flimsyDef) {
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
  }
}