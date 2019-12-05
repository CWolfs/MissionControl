using UnityEngine;

using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using BattleTech.Serialization;

using HBS.Util;
using HBS.Collections;

using Harmony;

public static class EncounterLayerDataExtensions {
  public static MapEncounterLayerDataCell GetSafeCellAt(this EncounterLayerData layerData, int x, int z) {
    if (layerData.IsWithinBounds(x, z)) {
      return layerData.GetCellAt(x, z);
    }

    return null;
  }

  public static ContractObjectiveGameLogic GetContractObjectiveGameLogicByGUID(this EncounterLayerData layerData, string guid) {
    ContractObjectiveGameLogic[] components = layerData.GetComponentsInChildren<ContractObjectiveGameLogic>();
    foreach (ContractObjectiveGameLogic logic in components) {
      if (logic.encounterObjectGuid == guid) return logic;
    }
    return null;
  }

  public static MapEncounterLayerDataCell[,] GetMapEncounterLayerDataCells(this EncounterLayerData layerData) {
    if (layerData.mapEncounterLayerDataCells != null) return layerData.mapEncounterLayerDataCells;

    Transform parent = layerData.gameObject.transform.parent;
    foreach (Transform t in parent) {
      EncounterLayerData siblingEncounterLayer = t.GetComponent<EncounterLayerData>();
      if (siblingEncounterLayer.mapEncounterLayerDataCells != null) return siblingEncounterLayer.mapEncounterLayerDataCells;
    }

    return null;
  }

  public static void LoadMapData(this EncounterLayerData layerData, EncounterLayerIdentifier encounterLayerIdentifier, DataManager dataManager) {
    EncounterLayerParent component = MissionControl.MissionControl.Instance.EncounterLayerParent;
    MapMetaDataExporter mapMetaExporter = component.GetComponent<MapMetaDataExporter>();

    AccessTools.Method(typeof(MapMetaDataExporter), "LoadMapMetaDataOnly").Invoke(mapMetaExporter, new object[] { UnityGameInstance.BattleTechGame.Combat.MapMetaData, dataManager });

    MapMetaData mapMetaData = mapMetaExporter.mapMetaData;
    InclineMeshData inclineMeshData = mapMetaExporter.inclineMeshData;

    string encounterLayerIdentifierPath = encounterLayerIdentifier.path;
    string encounterLayerDataName = MapMetaDataExporter.GetEncounterLayerDataName(encounterLayerIdentifier.name);

    MissionControl.Main.LogDebug("[LoadMapData] Borrowing Map Data form Layer Data " + encounterLayerDataName);

    encounterLayerIdentifierPath = dataManager.ResourceLocator.EntryByID(encounterLayerDataName, BattleTechResourceType.LayerData, false).FilePath;
    byte[] data = File.ReadAllBytes(encounterLayerIdentifierPath);
    EncounterLayerData layerByGuid = layerData;
    Serializer.Deserialize<EncounterLayerData>(data, SerializationTarget.Exported, TargetMaskOperation.HAS_ANY, layerByGuid);
    layerByGuid.ReattachReferences();
    using (SerializationStream serializationStream = new SerializationStream(File.ReadAllBytes(encounterLayerIdentifierPath.Replace(".bin", "_RaycastInfo.bin")))) {
      layerByGuid.LoadRaycastInfo(serializationStream);
    }
    int length = mapMetaData.mapTerrainDataCells.GetLength(1);
    int length2 = mapMetaData.mapTerrainDataCells.GetLength(0);
    for (int i = 0; i < length; i++) {
      for (int j = 0; j < length2; j++) {
        mapMetaData.mapTerrainDataCells[i, j].MapEncounterLayerDataCell = layerByGuid.mapEncounterLayerDataCells[i, j];
        mapMetaData.mapTerrainDataCells[i, j].UpdateCachedValues();
      }
    }
    string inclineMeshPathFromEncounterLayerDataPath = encounterLayerIdentifierPath.Replace(".bin", "_InclineMesh.IMS");
    inclineMeshData = new InclineMeshData();
    if (inclineMeshData.LoadFromPath(inclineMeshPathFromEncounterLayerDataPath)) {
      inclineMeshData.mapMetaData = mapMetaData;
      layerByGuid.inclineMeshData = inclineMeshData;
    } else {
      layerByGuid.inclineMeshData = null;
    }

    mapMetaExporter.mapTags = new TagSet(mapMetaData.mapTags);
    MissionControl.Main.LogDebug("[LoadMapData] Load Complete");
  }

  /*
  // TODO: Fix these deletes
  public static List<ITaggedItem> GetAllTaggedItems(GameObject go) {
    List<MonoBehaviour> items = (List<MonoBehaviour>)go.GetComponentsInChildren<MonoBehaviour>().ToList().Where(component => component is ITaggedItem);
    return items.Cast<ITaggedItem>().ToList();
  }

  public static void DeleteChunk(this EncounterLayerData layerData, string name) {
    EncounterChunkGameLogic[] chunkGameLogics = layerData.GetComponentsInChildren<EncounterChunkGameLogic>();
    chunkGameLogics.ToList().ForEach(encounterGameChunkLogic => {
      if (name == encounterGameChunkLogic.name) {
        MissionControl.Main.LogDebug("[EncounterLayerDataExtensions.DeleteChunk] Deleting chunk: " + encounterGameChunkLogic.DisplayName);
        List<ITaggedItem> items = GetAllTaggedItems(encounterGameChunkLogic.gameObject);
        for (int i = 0; i < items.Count; i++) {
          ITaggedItem item = items[i];
          MissionControl.Main.LogDebug("[EncounterLayerDataExtensions.DeleteChunk] Deleting item: " + item.DisplayName);
          UnityGameInstance.Instance.Game.Combat.ItemRegistry.RemoveItem(item.GUID);
          GameObject.Destroy(((MonoBehaviour)item).gameObject);
        }
        UnityGameInstance.Instance.Game.Combat.ItemRegistry.RemoveItem(encounterGameChunkLogic.GUID);
        GameObject.Destroy(encounterGameChunkLogic.gameObject);
      }
    });
  }

  public static bool DeleteObjective(this EncounterLayerData layerData, string name) {
    ContractObjectiveGameLogic[] existingContractObjectives = layerData.GetComponents<ContractObjectiveGameLogic>();
    existingContractObjectives.ToList().ForEach(obj => {
      obj.objectiveRefList.ForEach(objective => {
        ObjectiveGameLogic objectiveGameLogic = objective.encounterObject;
        if (objectiveGameLogic.name == name) {
          MissionControl.Main.LogDebug("[EncounterLayerDataExtensions.DeleteObjective] Deleting objective: " + objectiveGameLogic.name);
          UnityGameInstance.Instance.Game.Combat.ItemRegistry.RemoveItem(objectiveGameLogic.GUID);
          GameObject.Destroy(objectiveGameLogic.gameObject);
        }
      });
    });
    return false;
  }
  */
}