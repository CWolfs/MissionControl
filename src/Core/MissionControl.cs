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
    public string EncounterRulesName { get; private set; }
    public GameObject EncounterLayerParentGameObject { get; private set; }
    public EncounterLayerParent EncounterLayerParent { get; private set; }
    public GameObject EncounterLayerGameObject { get; private set; }
    public EncounterLayerData EncounterLayerData { get; private set; }
    public HexGrid HexGrid { get; private set; }

    public bool IsContractValid { get; private set; } = false;

    private Dictionary<string, List<Type>> AvailableEncounters = new Dictionary<string, List<Type>>();

    public Dictionary<ContractStats, object> ContractStats = new Dictionary<ContractStats, object>();

    private MissionControl() {
      LoadEncounterRules();
    }

    private void LoadEncounterRules() {
      AddEncounter("AmbushConvoy", typeof(AmbushConvoyEncounterRules));
      AddEncounter("Assassinate", typeof(AssassinateEncounterRules));

      AddEncounter("CaptureBase", typeof(CaptureBaseJointAssaultEncounterRules));
      AddEncounter("CaptureBase", typeof(CaptureBaseAidAssaultEncounterRules));

      AddEncounter("CaptureEscort", typeof(CaptureEscortAdditionalBlockersEncounterRules));
      
      AddEncounter("DefendBase", typeof(DefendBaseEncounterRules));
      
      AddEncounter("DestroyBase", typeof(DestroyBaseJointAssaultEncounterRules));
      AddEncounter("DestroyBase", typeof(DestroyBaseAidAssaultEncounterRules));
      
      AddEncounter("Rescue", typeof(RescueEncounterRules));
      AddEncounter("SimpleBattle", typeof(SimpleBattleEncounterRules));
    
      // Skirmish
      if (Main.Settings.DebugSkirmishMode) AddEncounter("ArenaSkirmish", typeof(DebugArenaSkirmishEncounterRules));
    }

    public void AddEncounter(string contractType, Type encounter) {
      if (!AvailableEncounters.ContainsKey(contractType)) AvailableEncounters.Add(contractType, new List<Type>());
      AvailableEncounters[contractType].Add(encounter);  
    }

    public void ClearEncounters() {
      AvailableEncounters.Clear();
    }

    public void ClearEncounters(string contractType) {
      AvailableEncounters.Remove(contractType);
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
      SetActiveAdditionalLances(contract);
      Main.Logger.Log($"[MissionControl] Contract map is '{contract.mapName}'");
      ContractMapName = contract.mapName;
      SetContractType(CurrentContract.ContractType);
      AiManager.Instance.ResetCustomBehaviourVariableScopes();
      ContractStats.Clear();
    }

    public void SetActiveAdditionalLances(Contract contract) {
      if (Main.Settings.AdditionalLanceSettings.SkullValueMatters) {
        if (!IsSkirmish(contract) || (IsSkirmish(contract) && !Main.Settings.AdditionalLanceSettings.UseGeneralProfileForSkirmish)) {
          int difficulty = contract.Override.finalDifficulty;
          Main.LogDebug($"[MissionControl] Difficulty '{difficulty}' (Skull value '{(float)difficulty / 2f}')");
          if (Main.Settings.AdditionalLanceSettings.BasedOnVisibleSkullValue) {
            difficulty = contract.Override.GetUIDifficulty();
          }
          Main.LogDebug($"[MissionControl] Visisble Difficulty '{contract.Override.GetUIDifficulty()}' (Skull value '{(float)contract.Override.GetUIDifficulty() / 2f}')");

          if (Main.Settings.AdditionalLances.ContainsKey(difficulty)) {
            Main.Logger.Log($"[MissionControl] Using AdditionalLances for difficulty '{difficulty}' (Skull value '{(float)difficulty / 2f}')");
            Main.Settings.ActiveAdditionalLances = Main.Settings.AdditionalLances[difficulty];
          } else {
            Main.Logger.Log($"[MissionControl] No AdditionalLance exists for difficulty '{difficulty}' (Skull value '{(float)difficulty / 2f}'). Using general config.");
            Main.Settings.ActiveAdditionalLances = Main.Settings.AdditionalLances[0];
          }
        } else {
          Main.Logger.Log($"[MissionControl] 'Use General Profile for Skirmish' is on. Using general config.");
          Main.Settings.ActiveAdditionalLances = Main.Settings.AdditionalLances[0];
        }
      } else {
        Main.Logger.Log($"[MissionControl] Skull value doesn't matter for AdditionalLances. Using general config.");
        Main.Settings.ActiveAdditionalLances = Main.Settings.AdditionalLances[0];
      }
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

      int index = UnityEngine.Random.Range(0, encounters.Count);
      Type selectedEncounter = encounters[index];
      Main.Logger.Log($"[MissionControl] Setting contract type to '{type}' and using Encounter Rule of '{selectedEncounter.Name}'");
      SetEncounterRule(selectedEncounter);

      IsContractValid = true;
      return true;
    }

    private void SetEncounterRule(Type encounterRules) {
      if (AllowMissionControl()) {
        EncounterRules = (EncounterRules)Activator.CreateInstance(encounterRules);
        EncounterRulesName = encounterRules.Name.Replace("EncounterRules", "");
        EncounterRules.Build();
        EncounterRules.ActivatePostFeatures();
      } else {
        EncounterRules = null;
        EncounterRulesName = null;
      }
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
      if (Main.Settings.AdditionalLanceSettings.Enable) {
        bool areLancesAllowed = !(this.CurrentContract.IsFlashpointContract && Main.Settings.AdditionalLanceSettings.DisableIfFlashpointContract);
        if (areLancesAllowed) areLancesAllowed = Main.Settings.AdditionalLanceSettings.DisableWhenMaxTonnage.AreLancesAllowed((int)this.CurrentContract.Override.lanceMaxTonnage);
        if (areLancesAllowed) areLancesAllowed = Main.Settings.ActiveAdditionalLances.GetValidContractTypes(teamType).Contains(CurrentContractType);
        
        Main.LogDebug($"[AreAdditionalLancesAllowed] {areLancesAllowed}");
        return areLancesAllowed;
      }
      Main.Logger.Log($"[MissionControl] AdditionalLances are disabled.");
      return false;
    }

    public bool IsSkirmish(Contract contract) {
      string type = Enum.GetName(typeof(ContractType), contract.ContractType);
      return type == "ArenaSkirmish";
    }

    public bool IsSkirmish() {
      if (CurrentContract != null) {
        return IsSkirmish(CurrentContract);
      }
      return false;
    }

    public bool ShouldUseElites(Faction faction, string teamType) {
      Config.Lance activeAdditionalLances = Main.Settings.ActiveAdditionalLances.GetActiveAdditionalLanceByTeamType(teamType);
      return Main.Settings.AdditionalLanceSettings.UseElites && activeAdditionalLances.EliteLances.ShouldEliteLancesBeSelected(faction);
    }
    
    public Faction GetFactionFromTeamType(string teamType) {
      switch (teamType.ToLower()) {
				case "enemy":
					return MissionControl.Instance.CurrentContract.Override.targetTeam.faction;
				case "allies":
					return MissionControl.Instance.CurrentContract.Override.employerTeam.faction;
			}
      return Faction.INVALID_UNSET;
    }

    public bool AllowMissionControl() {
      if (!this.CurrentContract.IsFlashpointContract) return true;
      return this.CurrentContract.IsFlashpointContract && !Main.Settings.AdditionalLanceSettings.DisableIfFlashpointContract;
    }
  }
}