using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl {
  public class MissionControl {
    private static MissionControl instance;
    public static MissionControl Instance {
      get {
        if (instance == null) instance = new MissionControl();
        return instance;
      }
    }

    public Contract CurrentContract { get; private set; }
    public string ContractMapName { get; private set; }
    public string CurrentContractType { get; private set; } = "INVALID_UNSET";
    public EncounterRules EncounterRules { get; private set; }
    public GameObject EncounterLayerParentGameObject { get; private set; }
    public EncounterLayerParent EncounterLayerParent { get; private set; }
    public GameObject EncounterLayerGameObject { get; private set; }
    public EncounterLayerData EncounterLayerData { get; private set; }
    public HexGrid HexGrid { get; private set; }

    public bool IsContractValid { get; private set; } = false;

    private Dictionary<string, List<Type>> AvailableEncounters = new Dictionary<string, List<Type>>();

    private MissionControl() {
      LoadEncounterRules();
    }

    private void LoadEncounterRules() {
      AddEncounter("AmbushConvoy", typeof(AmbushConvoyEncounterRules));
      AddEncounter("Assassinate", typeof(AssassinateEncounterRules));
      AddEncounter("CaptureBase", typeof(CaptureBaseEncounterRules));
      AddEncounter("CaptureEscort", typeof(CaptureEscortEncounterRules));
      AddEncounter("DefendBase", typeof(DestroyBaseEncounterRules));
      AddEncounter("DestroyBase", typeof(DestroyBaseEncounterRules));
      AddEncounter("Rescue", typeof(RescueEncounterRules));
      AddEncounter("SimpleBattle", typeof(SimpleBattleEncounterRules));
    
      // Skirmish
      AddEncounter("ArenaSkirmish", typeof(ArenaSkirmishEncounterRules));
    }

    public void AddEncounter(string contractType, Type encounter) {
      if (!AvailableEncounters.ContainsKey(contractType)) AvailableEncounters.Add(contractType, new List<Type>());
      AvailableEncounters[contractType].Add(encounter);  
    }

    public List<string> GetAllContractTypes() {
      return new List<string>(AvailableEncounters.Keys);
    }

    public void InitSceneData() {
      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      if (!EncounterLayerParentGameObject) EncounterLayerParentGameObject = GameObject.Find("EncounterLayerParent");
      EncounterLayerParent = EncounterLayerParentGameObject.GetComponent<EncounterLayerParent>();

      EncounterLayerData = GetActiveEncounter();
      EncounterLayerGameObject = EncounterLayerData.gameObject;
      EncounterLayerData.CalculateEncounterBoundary();

      if (HexGrid == null) HexGrid = ReflectionHelper.GetPrivateStaticField(typeof(WorldPointGameLogic), "hexGrid") as HexGrid;
    }

    public void SetContract(Contract contract) {
      Main.Logger.Log($"[MissionControl] Setting contract '{contract.Name}'");
      CurrentContract = contract;
      Main.Logger.Log($"[MissionControl] Contract map is '{contract.mapName}'");
      ContractMapName = contract.mapName;
      SetContractType(CurrentContract.ContractType);
    }

    /*
      Future proofed method to allow for string custom contract type names
      instead of relying only on the enum values
    */
    public bool SetContractType(ContractType contractType) {
      List<Type> encounters = null;

      string type = Enum.GetName(typeof(ContractType), contractType);
      CurrentContractType = type;

      if (AvailableEncounters.ContainsKey(type)) {
        encounters = AvailableEncounters[type];
      } else {
        Main.Logger.LogError($"[MissionControl] Unknown contract / encounter type of '{type}'");
        EncounterRules = null;
        return false;
      }

      int index = UnityEngine.Random.Range(0, encounters.Count - 1);
      Type selectedEncounter = encounters[index];
      Main.Logger.Log($"[MissionControl] Setting contract type to '{type}' and using Encounter Rule of '{selectedEncounter.Name}'");
      SetEncounterRule(selectedEncounter);

      IsContractValid = true;
      return true;
    }

    private void SetEncounterRule(Type encounterRules) {
      EncounterRules = (EncounterRules)Activator.CreateInstance(encounterRules);
      EncounterRules.Build();
    }

    public void RunEncounterRules(LogicBlock.LogicType type, RunPayload payload = null) {
      if (EncounterRules != null) {
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
    }

    private EncounterLayerData GetActiveEncounter() {
      if (EncounterLayerData) return EncounterLayerData;

      Contract activeContract = UnityGameInstance.BattleTechGame.Combat.ActiveContract;
      string encounterObjectGuid = activeContract.encounterObjectGuid;
      EncounterLayerData selectedEncounterLayerData = EncounterLayerParent.GetLayerByGuid(encounterObjectGuid);
      
      return selectedEncounterLayerData;
    }

    public bool AreAdditionalLancesAllowed(string teamType) {
      bool areLancesAllowed = Main.Settings.AdditionalLances.GetValidContractTypes(teamType).Contains(CurrentContractType);
      if (Main.Settings.DebugMode) Main.Logger.Log($"[AreAdditionalLancesAllowed] {areLancesAllowed}");
      return areLancesAllowed;
    }
  }
}