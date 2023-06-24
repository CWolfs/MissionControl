using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using Harmony;

namespace MissionControl.EncounterFactories {
  public class EncounterLayerFactory {
    public static EncounterLayerData CreateEncounterLayer(Contract contract) {
      EncounterLayer_MDD encounterLayerMDD = MissionControl.Instance.EncounterLayerMDD;

      GameObject encounterLayerGo = new GameObject(encounterLayerMDD.Name);
      EncounterLayerData encounterLayer = encounterLayerGo.AddComponent<EncounterLayerData>();
      encounterLayer.encounterObjectGuid = contract.encounterObjectGuid;
      encounterLayer.encounterName = MissionControl.Instance.CurrentContractTypeValue.Name;
      encounterLayer.encounterDescription = MissionControl.Instance.EncounterLayerMDD.Description;
      encounterLayer.EDITOR_SetSupportedContractTypeID(MissionControl.Instance.CurrentContractTypeValue.Name);
      encounterLayer.version = MissionControl.Instance.CurrentContractTypeValue.Version;

      // Need to dump the serialised binary data into a mock EncounterLayerData then throw it away to get the important bits
      GameObject mockGo = new GameObject("MockGo");
      EncounterLayerData mockLayer = mockGo.AddComponent<EncounterLayerData>();
      List<EncounterLayerIdentifier> layerIdList = UnityGameInstance.BattleTechGame.Combat.MapMetaData.encounterLayerIdentifierList;
      mockLayer.LoadMapData(layerIdList[0], UnityGameInstance.BattleTechGame.DataManager);
      encounterLayer.mapEncounterLayerDataCells = mockLayer.mapEncounterLayerDataCells;
      encounterLayer.inclineMeshData = mockLayer.inclineMeshData;
      MonoBehaviour.Destroy(mockLayer);

      // Clear out all old regions from copied encounter layer data
      for (int j = 0; j < encounterLayer.mapEncounterLayerDataCells.GetLength(1); j++) {
        for (int k = 0; k < encounterLayer.mapEncounterLayerDataCells.GetLength(0); k++) {
          List<string> regionGuidList = encounterLayer.mapEncounterLayerDataCells[j, k].regionGuidList;
          if (regionGuidList != null) regionGuidList.Clear();
        }
      }

      encounterLayerGo.AddComponent<PlotOverride>();

      return encounterLayer;
    }
  }
}