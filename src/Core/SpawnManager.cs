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
    public EncounterRules EncounterRules { get; private set; }
    public GameObject EncounterLayerParentGameObject { get; private set; }
    public GameObject EncounterLayerGameObject { get; private set; }
    public EncounterLayerData EncounterLayerData { get; private set; }
    public HexGrid HexGrid { get; private set; }

    public static SpawnManager GetInstance() { 
      if (instance == null) instance = new SpawnManager();
      return instance;
    }

    private SpawnManager() {
      Init();
    }

    public void Init() {
      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      if (!EncounterLayerParentGameObject) EncounterLayerParentGameObject = GameObject.Find("EncounterLayerParent");
      EncounterLayerGameObject = GetActiveEncounterGameObject();
      EncounterLayerData = EncounterLayerGameObject.GetComponent<EncounterLayerData>();
      EncounterLayerData.CalculateEncounterBoundary();

      if (HexGrid == null) HexGrid = ReflectionHelper.GetPrivateStaticField(typeof(WorldPointGameLogic), "hexGrid") as HexGrid;
    }

    public bool SetContractType(ContractType contractType) {
      if (!EncounterLayerParentGameObject) Init();
      CurrentContractType = contractType;

      switch (CurrentContractType) {
        case ContractType.Rescue: {
          Main.Logger.Log($"[SpawnManager] Setting contract type to 'Rescue'");
          SetEncounterRules(new RescueEncounterRules());
          return true;
        }
        case ContractType.DefendBase: {
          Main.Logger.Log($"[SpawnManager] Setting contract type to 'DefendBase'");
          SetEncounterRules(new DefendBaseEncounterRules());
          return true;
        }
        case ContractType.DestroyBase: {
          Main.Logger.Log($"[SpawnManager] Setting contract type to 'DestroyBase'");
          SetEncounterRules(new DestroyBaseEncounterRules());
          return true;  
        }
        default: {
          Main.Logger.LogError($"[SpawnManager] Unknown contract / encounter type of {contractType}");
          return false;
        }
      }
    }

    private void SetEncounterRules(EncounterRules encounterRules) {
      EncounterRules = encounterRules;
    }

    public void UpdateSpawns() {
      EncounterRules.UpdateSpawns();
    }

    private GameObject GetActiveEncounterGameObject() {
      if (EncounterLayerGameObject) return EncounterLayerGameObject;

      foreach (Transform t in EncounterLayerParentGameObject.transform) {
        GameObject child = t.gameObject;
        if (child.activeSelf) {
          EncounterLayerGameObject = t.gameObject;
          return EncounterLayerGameObject;
        }
      }
      return null;
    }
  }
}