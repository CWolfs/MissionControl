using UnityEngine;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

namespace MissionControl.EncounterFactories {
  public class EncounterLayerFactory {
    public static EncounterLayerData CreateEncounterLayer(Contract contract) {
      EncounterLayer_MDD encounterLayerMDD = MetadataDatabase.Instance.SelectEncounterLayerByGuid(contract.encounterObjectGuid);

      GameObject encounterLayerGo = new GameObject(encounterLayerMDD.Name);
      EncounterLayerData encounterLayer = encounterLayerGo.AddComponent<EncounterLayerData>();
      encounterLayer.encounterObjectGuid = contract.encounterObjectGuid;

      encounterLayerGo.AddComponent<PlotOverride>();
      encounterLayerGo.AddComponent<ContractObjectiveGameLogic>();

      ContractAdapter contractAdapter = encounterLayerGo.AddComponent<ContractAdapter>();
      contractAdapter.contractOverride = contract.Override;
      contractAdapter.ApplyContractToLayer(encounterLayer, false);

      return encounterLayer;
    }
  }
}