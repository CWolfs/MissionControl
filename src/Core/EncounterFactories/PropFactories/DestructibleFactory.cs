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
    private PropDestructibleFlimsyDef PropFlimsyDef { get; set; }

    private GameObject destructibleFlimsyGroupGO;

    public DestructibleFactory() { }

    public DestructibleFactory(PropDestructibleFlimsyDef propFlimsyDef) {
      PropFlimsyDef = propFlimsyDef;
    }

    private GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject gameObject = new GameObject((name == null) ? "CustomName" : name);
      gameObject.transform.SetParent(parent.transform, false);

      return gameObject;
    }

    public GameObject CreateDestructibleFlimsyGroup(string name, GameObject parent) {
      Main.Logger.Log("[BuildingFactory.CreateFlimsyGroup] About to create flimsy group " + name);
      this.destructibleFlimsyGroupGO = CreateGameObject(parent, $"DestructibleFlimsyGroup_{name}");
      destructibleFlimsyGroupGO.SetActive(false);

      destructibleFlimsyGroupGO.AddComponent<SnapToTerrain>();
      this.destructibleFlimsyGroupGO.AddComponent<DestructibleFlimsyGroup>();

      destructibleFlimsyGroupGO.SetActive(true);
      return this.destructibleFlimsyGroupGO;
    }

    public GameObject CreateDestructible(GameObject parentGO, string name) {
      PropModelDef propModelDef = PropFlimsyDef.GetPropModelDef();

      DestructibleFlimsyGroup destructibleFlimsyGroup = parentGO.GetComponent<DestructibleFlimsyGroup>();

      GameObject destructibleGO = CreateGameObject(parentGO, $"Destructible_{name}");
      destructibleGO.SetActive(false);

      // CreateColAndLODs(destructibleGO, propModelDef);
      if (CreateDestructibleFlimsy(destructibleGO, PropFlimsyDef) == null) {
        destructibleGO.transform.SetParent(null, false);
        MonoBehaviour.Destroy(destructibleGO);
        return null;
      }

      DestructibleObject destructibleObject = destructibleGO.AddComponent<DestructibleObject>();
      destructibleObject.destructType = DestructibleObject.DestructType.flimsyGroup;
      destructibleObject.structSize = propModelDef.DestructibleSize;  // TODO: Check if this should change for destructible flimsies - always a certain size?
      destructibleObject.structMaterial = propModelDef.DestructibleMaterial;
      destructibleObject.flimsyDestructType = propModelDef.FlimsyDestructibleType; // TODO: Check if this should change for destructible flimsies - always a certain size?
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.embeddedFlimsyChildren = new List<DestructibleObject>();
      destructibleObject.damageAssetGroup = GetRandomDamageAssetGroup();
      destructibleObject.decalObjects = new List<GameObject>();
      destructibleObject.decalSpawners = new List<BTDecalSpawner>();
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.destructibleFlimsyGroup = destructibleFlimsyGroup;
      destructibleObject.destructList = destructibleObject.GetComponentsInChildren<DestructibleObject>().ToList();

      // Setup LOD Group
      LODGroup lodGroup = SetupLODGroup(destructibleGO);

      destructibleGO.transform.localScale = propModelDef.Scale.Value; // Sets the Model's scale
      destructibleGO.SetActive(true);
      return destructibleGO;
    }

    private GameObject CreateDestructibleFlimsy(GameObject flimsyParentGO, PropDestructibleFlimsyDef propFlimsyDef) {
      PropModelDef propModelDef = propFlimsyDef.GetPropModelDef();

      Main.Logger.Log("[BuildingFactory.CreateFlimsy] About to create flimsy " + propFlimsyDef.Key);
      GameObject flimsyGO = CreateGameObject(flimsyParentGO, $"Destructible_{propFlimsyDef.Key}");
      flimsyGO.SetActive(false);

      MeshFilter mf = flimsyGO.AddComponent<MeshFilter>();
      flimsyGO.AddComponent<MeshRenderer>();

      Rigidbody rb = flimsyGO.AddComponent<Rigidbody>();
      rb.mass = propFlimsyDef.Mass;

      BoxCollider boxCollider = flimsyGO.AddComponent<BoxCollider>();

      DestructibleObject destructibleObject = flimsyGO.AddComponent<DestructibleObject>();
      destructibleObject.isFlimsy = true;
      destructibleObject.flimsyGroupChild = true;
      destructibleObject.destructType = DestructibleObject.DestructType.flimsyChild;
      destructibleObject.structSize = propModelDef.DestructibleSize;
      destructibleObject.flimsyDestructType = propModelDef.FlimsyDestructibleType;
      destructibleObject.structMaterial = propModelDef.DestructibleMaterial;
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.pieceSize = DestructibleObject.PieceSize.small; // TODO: Expose this

      if (!AttachFlimsyMesh(flimsyGO, propFlimsyDef)) {
        flimsyGO.transform.SetParent(null, false);
        MonoBehaviour.Destroy(flimsyGO);
        return null;
      }
      CalculateBounds(mf, boxCollider);
      CreatePropLights(flimsyGO, propModelDef);

      flimsyGO.transform.localPosition = propFlimsyDef.Position;
      flimsyGO.transform.localEulerAngles = propFlimsyDef.Rotation;

      if (propFlimsyDef.AllowModelMeshOffsets && propModelDef.MeshOffsets.ContainsKey("LOD0")) {
        flimsyGO.transform.localPosition += propModelDef.MeshOffsets["LOD0"];
      }

      flimsyGO.SetActive(true);
      return flimsyGO;
    }

    private bool AttachFlimsyMesh(GameObject flimsyGO, PropDestructibleFlimsyDef flimsyDef) {
      PropModelDef propModelDef = flimsyDef.GetPropModelDef();
      Mesh flimsyLOD0Mesh = ResolveMesh(propModelDef, $"{propModelDef.MeshName}_LOD0", "LOD0", isFlimsyBase: false);
      if (flimsyLOD0Mesh == null) {
        flimsyLOD0Mesh = ResolveMesh(propModelDef, propModelDef.MeshName, "Flimsy", isFlimsyBase: true);
      }

      if (flimsyLOD0Mesh == null) {
        Main.Logger.LogError($"[DestructibleFactory.AttachFlimsyMesh] Could not resolve flimsy mesh '{propModelDef.MeshName}' for '{propModelDef.Key}'");
        return false;
      }

      MeshFilter flimsyLOD0MF = flimsyGO.GetComponent<MeshFilter>();
      MeshRenderer flimsyLOD0MR = flimsyGO.GetComponent<MeshRenderer>();

      Material[] materials = BuildMaterialsForRenderer(flimsyLOD0Mesh, propModelDef, propModelDef.Materials, placeholderMaterial);
      flimsyLOD0MR.materials = materials;
      flimsyLOD0MF.mesh = flimsyLOD0Mesh;
      return true;
    }
  }
}