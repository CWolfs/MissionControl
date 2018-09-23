using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using ContractCommand.Logic;
using ContractCommand.Rules;
using ContractCommand.Utils;

namespace ContractCommand {
  public class EncounterManager {
    private static EncounterManager instance;

    public ContractType CurrentContractType { get; private set; } = ContractType.INVALID_UNSET;
    public EncounterRule EncounterRules { get; private set; }
    public GameObject EncounterLayerParentGameObject { get; private set; }
    public EncounterLayerParent EncounterLayerParent { get; private set; }
    public GameObject EncounterLayerGameObject { get; private set; }
    public EncounterLayerData EncounterLayerData { get; private set; }
    public HexGrid HexGrid { get; private set; }

    public bool IsContractValid { get; private set; } = false;

    public static EncounterManager GetInstance() { 
      if (instance == null) instance = new EncounterManager();
      return instance;
    }

    private EncounterManager() { }

    public void InitSceneData() {
      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      if (!EncounterLayerParentGameObject) EncounterLayerParentGameObject = GameObject.Find("EncounterLayerParent");
      EncounterLayerParent = EncounterLayerParentGameObject.GetComponent<EncounterLayerParent>();

      EncounterLayerData = GetActiveEncounter();
      EncounterLayerGameObject = EncounterLayerData.gameObject;
      EncounterLayerData.CalculateEncounterBoundary();

      if (HexGrid == null) HexGrid = ReflectionHelper.GetPrivateStaticField(typeof(WorldPointGameLogic), "hexGrid") as HexGrid;
    }

    public bool SetContractType(ContractType contractType) {
      CurrentContractType = contractType;

      switch (CurrentContractType) {
        case ContractType.Rescue: {
          Main.Logger.Log($"[EncounterManager] Setting contract type to 'Rescue'");
          SetEncounterRules(new RescueEncounterRules());
          break;
        }
        case ContractType.DefendBase: {
          Main.Logger.Log($"[EncounterManager] Setting contract type to 'DefendBase'");
          SetEncounterRules(new DefendBaseEncounterRules());
          break;
        }
        case ContractType.DestroyBase: {
          Main.Logger.Log($"[EncounterManager] Setting contract type to 'DestroyBase'");
          SetEncounterRules(new DestroyBaseEncounterRules());
          break;
        }
        default: {
          Main.Logger.LogError($"[EncounterManager] Unknown contract / encounter type of {contractType}");
          return false;
        }
      }

      IsContractValid = true;
      return true;
    }

    private void SetEncounterRules(EncounterRule encounterRules) {
      EncounterRules = encounterRules;
    }

    public void RunEncounterRules(LogicBlock.LogicType type, RunPayload payload = null) {
      switch (type) {
        case LogicBlock.LogicType.RESOURCE_REQUEST: {
          EncounterRules.Run(LogicBlock.LogicType.RESOURCE_REQUEST, payload);
          break;
        }
        case LogicBlock.LogicType.CONTRACT_OVERRIDE_MANIPULATION: {
          EncounterRules.Run(LogicBlock.LogicType.CONTRACT_OVERRIDE_MANIPULATION, payload);
          break;
        }
        case LogicBlock.LogicType.ENCOUNTER_MANIPULATION: {
          EncounterRules.Run(LogicBlock.LogicType.ENCOUNTER_MANIPULATION, payload);
          break; 
        }
        case LogicBlock.LogicType.SCENE_MANIPULATION: {
          EncounterRules.Run(LogicBlock.LogicType.SCENE_MANIPULATION, payload);
          break;
        }
        default: {
          Main.Logger.LogError($"[RunEncounterRules] Unknown type of '{type.ToString()}'");
          break;
        }
      }
    }

    private EncounterLayerData GetActiveEncounter() {
      if (EncounterLayerData) return EncounterLayerData;

      Contract activeContract = UnityGameInstance.BattleTechGame.Combat.ActiveContract;
      string encounterObjectGuid = activeContract.encounterObjectGuid;
      EncounterLayerData selectedEncounterLayerData = EncounterLayerParent.GetLayerByGuid(encounterObjectGuid);
      
      return selectedEncounterLayerData;
    }
  }
}