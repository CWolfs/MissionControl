using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

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

      encounterLayerGo.AddComponent<PlotOverride>();

      CreateContractObjectives(encounterLayerGo);

      return encounterLayer;
    }

    public static void CreateContractObjectives(GameObject encounterLayerGo) {
      List<ContractObjectiveOverride> contractObjectives = MissionControl.Instance.CurrentContract.Override.contractObjectiveList;

      foreach (ContractObjectiveOverride contractObjective in contractObjectives) {
        ContractObjectiveGameLogic contractObjectiveGameLogic = encounterLayerGo.AddComponent<ContractObjectiveGameLogic>();
        contractObjectiveGameLogic.encounterObjectGuid = contractObjective.GUID;
        contractObjectiveGameLogic.description = contractObjective.description;
      }
    }
  }
}