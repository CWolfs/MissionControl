using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using SpawnVariation.Logic;
using SpawnVariation.Rules;

namespace SpawnVariation {
  public class SpawnManager {
    private static SpawnManager instance;

    public ContractType CurrentContractType { get; private set; } = ContractType.INVALID_UNSET;
    public EncounterRules EncounterRules { get; private set; }

    public static SpawnManager GetInstance() { 
      if (instance == null) instance = new SpawnManager();
      return instance;
    }

    private SpawnManager() { }

    public void SetContractType(ContractType contractType) {
      CurrentContractType = contractType;

      switch (CurrentContractType) {
        case ContractType.Rescue: {
          Main.Logger.Log($"[SpawnManager] Setting contract type to 'Rescue'");
          SetEncounterRules(new RescueEncounterRules());
          break;
        }
        default: {
          Main.Logger.LogError($"[SpawnManager] Unknown contract / encounter type of {contractType}");
          break;
        }
      }
    }

    private void SetEncounterRules(EncounterRules encounterRules) {
      EncounterRules = encounterRules;
    }

    public void UpdateSpawns() {
      EncounterRules.UpdateSpawns();
    }
  }
}