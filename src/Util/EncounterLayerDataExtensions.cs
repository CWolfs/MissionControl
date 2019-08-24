using UnityEngine;

using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

public static class EncounterLayerDataExtensions {
  public static MapEncounterLayerDataCell GetSafeCellAt(this EncounterLayerData layerData, int x, int z) {
    if (layerData.IsWithinBounds(x, z)) {
      return layerData.GetCellAt(x, z);
    }

    return null;
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