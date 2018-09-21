using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using SpawnVariation.Logic;
using SpawnVariation.Rules;
using SpawnVariation.Utils;

namespace SpawnVariation {
  public class SpawnManager {
    private static SpawnManager instance;

    public ContractType CurrentContractType { get; private set; } = ContractType.INVALID_UNSET;
    public EncounterRule EncounterRules { get; private set; }
    public GameObject EncounterLayerParentGameObject { get; private set; }
    public EncounterLayerParent EncounterLayerParent { get; private set; }
    public GameObject EncounterLayerGameObject { get; private set; }
    public EncounterLayerData EncounterLayerData { get; private set; }
    public HexGrid HexGrid { get; private set; }

    public bool IsContractValid { get; private set; } = false;

    public static SpawnManager GetInstance() { 
      if (instance == null) instance = new SpawnManager();
      return instance;
    }

    private SpawnManager() { }

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
          Main.Logger.Log($"[SpawnManager] Setting contract type to 'Rescue'");
          SetEncounterRules(new RescueEncounterRules());
          break;
        }
        case ContractType.DefendBase: {
          Main.Logger.Log($"[SpawnManager] Setting contract type to 'DefendBase'");
          SetEncounterRules(new DefendBaseEncounterRules());
          break;
        }
        case ContractType.DestroyBase: {
          Main.Logger.Log($"[SpawnManager] Setting contract type to 'DestroyBase'");
          SetEncounterRules(new DestroyBaseEncounterRules());
          break;
        }
        default: {
          Main.Logger.LogError($"[SpawnManager] Unknown contract / encounter type of {contractType}");
          return false;
        }
      }

      IsContractValid = true;
      return true;
    }

    private void SetEncounterRules(EncounterRule encounterRules) {
      EncounterRules = encounterRules;
    }

    public void RunEncounterRules() {
      EncounterRules.Run(LogicBlock.LogicType.SCENE_MANIPULATION);
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