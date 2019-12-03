using UnityEngine;

using BattleTech;
using BattleTech.Data;

namespace MissionControl.EncounterFactories {
  public class EncounterLayerFactory {
    public static EncounterLayerData CreateEncounterLayer(Contract contract) {
      EncounterLayer_MDD encounterLayerMDD = MetadataDatabase.Instance.SelectEncounterLayerByGuid(contract.encounterObjectGuid);

      GameObject encounterLayerGo = new GameObject(encounterLayerMDD.Name);
      EncounterLayerData encounterLayer = encounterLayerGo.AddComponent<EncounterLayerData>();
      encounterLayer.encounterObjectGuid = contract.encounterObjectGuid;

      return encounterLayer;
    }
  }
}