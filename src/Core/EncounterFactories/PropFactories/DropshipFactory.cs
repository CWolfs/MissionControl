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
      Main.Logger.LogError("[DropshipFactory.CreateCustomDropship] Custom dropships are not yet supported");
      return null;

      // BuildingRepresentation buildingRepresentation = dropshipGO.AddComponent<BuildingRepresentation>();

      // DropshipGameLogic dropshipGameLogic = dropshipGO.AddComponent<DropshipGameLogic>();
      // dropshipGameLogic.buildingDefId = PropDropshipDef.BuildingDefID; // Supports direct BuildingDefIds (e.g. buildingdef_Military_Large) or general values (e.g. ObstructionGameLogic.buildingDef_SolidObstruction)
      // dropshipGameLogic.teamDefinitionGuid = TeamUtils.WORLD_TEAM_ID;
      // dropshipGameLogic.currentAnimationState = StartingState;

      // string obstructionGuid = Guid.NewGuid().ToString();
      // dropshipGameLogic.encounterObjectGuid = obstructionGuid;

      // // Track the custom buildings to bypass the min 8 cell hit count for buildings to be added to the proper building list of a MapEncounterLayerDataCell
      // MissionControl.Instance.CustomBuildingGuids.Add(obstructionGuid);

      // dropshipGameLogic.overrideBuildingName = CustomName;
      // dropshipGameLogic.overrideStructurePoints = CustomStructurePoints;

      // // Health is set by buildingDefId, or a manual override 'overrideStructurePoints' on Building set via ObstructionGameLogic
      // DestructibleObjectGroup destructibleObjectGroup = dropshipGO.AddComponent<DestructibleObjectGroup>();
      // destructibleObjectGroup.BakeDestructionAssets();

      // dropshipGO.AddComponent<Animator>();
      // dropshipGO.AddComponent<MechCustomization>();
      // dropshipGO.AddComponent<MeshCollider>();

      // return dropshipGO;
    }

    private void CreateVanillaDropship(GameObject parent) {
      Main.Logger.Log("[DropshipFactory.CreateVanillaDropship] Creating vanilla dropship " + PropDropshipDef.Key);
      PropModelDef propModelDef = PropDropshipDef.GetPropModelDef();
      string completeBundleName = propModelDef.CompleteBundleName;

      GameObject prefab = DataManager.Instance.SynchronouslyLoadPrefab(parent, completeBundleName);

      if (prefab != null) {
        prefab.transform.localPosition = Vector3.zero;
        prefab.transform.localEulerAngles = Vector3.zero;
        prefab.transform.localScale = propModelDef.Scale.Value;

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
  }
}