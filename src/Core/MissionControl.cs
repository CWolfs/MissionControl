using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;

using MissionControl.Data;
using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.Utils;
using MissionControl.EncounterFactories;
using MissionControl.ContractTypeBuilders;

using Newtonsoft.Json.Linq;

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
    public ContractTypeValue CurrentContractTypeValue { get; private set; }

    public EncounterRules EncounterRules { get; private set; }
    public string EncounterRulesName { get; private set; }
    public GameObject EncounterLayerParentGameObject { get; private set; }
    public EncounterLayerParent EncounterLayerParent { get; private set; }
    public GameObject EncounterLayerGameObject { get; private set; }
    public EncounterLayerData EncounterLayerData { get; private set; }

    // Only populated for custom contract types
    public EncounterLayer_MDD EncounterLayerMDD { get; private set; }
    public bool IsCustomContractType { get; set; } = false;

    public HexGrid HexGrid { get; private set; }

    public bool IsContractValid { get; private set; } = false;
    public bool IsMCLoadingFinished { get; set; } = false;

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

      AddEncounter("FireMission", typeof(FireMissionEncounterRules));

      AddEncounter("AttackDefend", typeof(AttackDefendEncounterRules));

      AddEncounter("ThreeWayBattle", typeof(BattlePlusEncounterRules));

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
      if (EncounterLayerData == null) { // If no EncounterLayer matches the Contract Type GUID, it's a custom contract type
        EncounterLayerData = ConstructCustomContractType();
        IsCustomContractType = true;
      } else {
        IsCustomContractType = false;
      }

      EncounterLayerGameObject = EncounterLayerData.gameObject;
      EncounterLayerData.CalculateEncounterBoundary();

      if (HexGrid == null) HexGrid = ReflectionHelper.GetPrivateStaticField(typeof(WorldPointGameLogic), "_hexGrid") as HexGrid;
    }

    private EncounterLayerData ConstructCustomContractType() {
      CurrentContract = UnityGameInstance.BattleTechGame.Combat.ActiveContract;
      MetadataDatabase mdd = MetadataDatabase.Instance;

      EncounterLayerMDD = mdd.SelectEncounterLayerByGuid(CurrentContract.encounterObjectGuid);
      CurrentContractTypeValue = CurrentContract.ContractTypeValue;

      EncounterLayerData = EncounterLayerFactory.CreateEncounterLayer(CurrentContract);
      EncounterLayerData.gameObject.transform.parent = EncounterLayerParent.transform;

      BuildConstructTypeEncounter(EncounterLayerData.gameObject);

      return EncounterLayerData;
    }

    private void BuildConstructTypeEncounter(GameObject encounterLayerGo) {
      CurrentContract = UnityGameInstance.BattleTechGame.Combat.ActiveContract;
      string contractTypeName = CurrentContract.ContractTypeValue.Name;

      if (DataManager.Instance.AvailableCustomContractTypeBuilds.ContainsKey(contractTypeName)) {
        JObject contractTypeBuild = DataManager.Instance.GetAvailableCustomContractTypeBuilds(contractTypeName, EncounterLayerMDD.EncounterLayerID);
        ContractTypeBuilder contractTypeBuilder = new ContractTypeBuilder(encounterLayerGo, contractTypeBuild);
        contractTypeBuilder.Build();
      } else {
        Main.Logger.LogError($"[MissionControl] Cannot build contract type of '{contractTypeName}'. No contract type build file exists.");
      }
    }

    public void SetContract(Contract contract) {
      Main.Logger.Log($"[MissionControl] Setting contract '{contract.Name}'");
      CurrentContract = contract;

      if (AllowMissionControl()) {
        IsMCLoadingFinished = false;
        SetActiveAdditionalLances(contract);
        Main.Logger.Log($"[MissionControl] Contract map is '{contract.mapName}'");
        ContractMapName = contract.mapName;
        SetContractType(CurrentContract.ContractTypeValue);
        AiManager.Instance.ResetCustomBehaviourVariableScopes();
      } else {
        Main.Logger.Log($"[MissionControl] Mission Control is not allowed to run. Possibly a story mission or flashpoint contract.");
        EncounterRules = null;
        EncounterRulesName = null;
        IsMCLoadingFinished = true;
      }

      ContractStats.Clear();
      ClearOldContractData();
    }

    private void ClearOldContractData() {
      Main.Logger.Log($"[MissionControl] Clearing old contract data");

      // Clear old lance data
      if (CurrentContract != null) {
        CurrentContract.Override.targetTeam.lanceOverrideList =
          CurrentContract.Override.targetTeam.lanceOverrideList.Where(lanceOverride => !(lanceOverride is MLanceOverride)).ToList();
        CurrentContract.Override.employerTeam.lanceOverrideList =
          CurrentContract.Override.employerTeam.lanceOverrideList.Where(lanceOverride => !(lanceOverride is MLanceOverride)).ToList();
      }
    }

    public void SetActiveAdditionalLances(Contract contract) {
      if (Main.Settings.AdditionalLanceSettings.SkullValueMatters) {
        if (!IsSkirmish(contract) || (IsSkirmish(contract) && !Main.Settings.AdditionalLanceSettings.UseGeneralProfileForSkirmish)) {
          int difficulty = contract.Override.finalDifficulty;
          Main.LogDebug($"[MissionControl] Difficulty '{difficulty}' (Skull value '{(float)difficulty / 2f}')");
          if (Main.Settings.AdditionalLanceSettings.BasedOnVisibleSkullValue) {
            difficulty = contract.Override.GetUIDifficulty();
          }
          Main.LogDebug($"[MissionControl] Visible Difficulty '{contract.Override.GetUIDifficulty()}' (Skull value '{(float)contract.Override.GetUIDifficulty() / 2f}')");

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
    public void SetContractType(ContractTypeValue contractTypeValue) {
      if (AllowMissionControl()) {
        List<Type> encounters = null;

        string type = contractTypeValue.Name;
        CurrentContractType = type;

        if (AvailableEncounters.ContainsKey(type)) {
          encounters = AvailableEncounters[type];

          int index = UnityEngine.Random.Range(0, encounters.Count);
          Type selectedEncounter = encounters[index];
          Main.Logger.Log($"[MissionControl] Setting contract type to '{type}' and using Encounter Rule of '{selectedEncounter.Name}'");
          SetEncounterRule(selectedEncounter);
        } else {
          Main.Logger.Log($"[MissionControl] Unknown contract / encounter type of '{type}'. Using fallback ruleset.");
          SetEncounterRule(typeof(FallbackEncounterRules));
        }

        IsContractValid = true;
      } else {
        Main.Logger.Log($"[MissionControl] Mission Control is not allowed to run. Possibly a story mission or flashpoint contract.");
        EncounterRules = null;
        EncounterRulesName = null;
        IsMCLoadingFinished = true;
      }
    }

    private void SetEncounterRule(Type encounterRules) {
      if (AllowMissionControl()) {
        EncounterRules = (EncounterRules)Activator.CreateInstance(encounterRules);
        EncounterRulesName = encounterRules.Name.Replace("EncounterRules", "");
        EncounterRules.Build();
        EncounterRules.ActivatePostFeatures();
      } else {
        Main.Logger.Log($"[MissionControl] Mission Control is not allowed to run. Possibly a story mission or flashpoint contract.");
        EncounterRules = null;
        EncounterRulesName = null;
        IsMCLoadingFinished = true;
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
      string encounterObjectGuid = activeContract.encounterObjectGuid;  // Contract Type GUID
      EncounterLayerData selectedEncounterLayerData = EncounterLayerParent.GetLayerByGuid(encounterObjectGuid);

      return selectedEncounterLayerData;
    }

    public bool AreAdditionalLancesAllowed(string teamType) {
      if (Main.Settings.AdditionalLanceSettings.Enable) {
        bool areLancesAllowed = !(this.CurrentContract.IsFlashpointContract && Main.Settings.AdditionalLanceSettings.DisableIfFlashpointContract);
        if (areLancesAllowed) areLancesAllowed = Main.Settings.ExtendedLances.GetValidContractTypes().Contains(CurrentContractType);
        if (areLancesAllowed) areLancesAllowed = Main.Settings.AdditionalLanceSettings.DisableWhenMaxTonnage.AreLancesAllowed((int)this.CurrentContract.Override.lanceMaxTonnage);
        if (areLancesAllowed) areLancesAllowed = Main.Settings.ActiveAdditionalLances.GetValidContractTypes(teamType).Contains(CurrentContractType);

        Main.LogDebug($"[AreAdditionalLancesAllowed] for {teamType}: {areLancesAllowed}");
        return areLancesAllowed;
      }
      Main.Logger.Log($"[MissionControl] AdditionalLances are disabled.");
      return false;
    }

    public bool IsExtendedBoundariesAllowed() {
      if (Main.Settings.ExtendedBoundaries.Enable) {
        bool isExtendedBoundariesAllowed = Main.Settings.ExtendedBoundaries.GetValidContractTypes().Contains(CurrentContractType);
        return isExtendedBoundariesAllowed;
      }
      return false;
    }

    public bool IsExtendedLancesAllowed() {
      if (Main.Settings.ExtendedLances.Enable) {
        bool isExtendedLancesAllowed = Main.Settings.ExtendedLances.GetValidContractTypes().Contains(CurrentContractType);
        return isExtendedLancesAllowed;
      }
      return false;
    }

    public bool IsRandomSpawnsAllowed() {
      if (Main.Settings.RandomSpawns.Enable) {
        bool isRandomSpawnsAllowed = Main.Settings.RandomSpawns.GetValidContractTypes().Contains(CurrentContractType);
        return isRandomSpawnsAllowed;
      }
      return false;
    }

    public bool IsSkirmish(Contract contract) {
      return !contract.ContractTypeValue.IsSinglePlayerProcedural && contract.ContractTypeValue.IsSkirmish;
    }

    public bool IsSkirmish() {
      if (CurrentContract != null) {
        return IsSkirmish(CurrentContract);
      }
      return false;
    }

    public bool ShouldUseElites(FactionDef faction, string teamType) {
      Config.Lance activeAdditionalLances = Main.Settings.ActiveAdditionalLances.GetActiveAdditionalLanceByTeamType(teamType);
      return Main.Settings.AdditionalLanceSettings.UseElites && activeAdditionalLances.EliteLances.ShouldEliteLancesBeSelected(faction);
    }

    public FactionDef GetFactionFromTeamType(string teamType) {
      switch (teamType.ToLower()) {
        case "enemy":
          return MissionControl.Instance.CurrentContract.Override.targetTeam.FactionDef;
        case "allies":
          return MissionControl.Instance.CurrentContract.Override.employerTeam.FactionDef;
      }
      return null;
    }

    public bool AllowMissionControl() {
      if (this.CurrentContract.IsStoryContract) return false;
      if (this.CurrentContract.IsRestorationContract) return false;
      if (!this.CurrentContract.IsFlashpointContract && !this.CurrentContract.IsFlashpointCampaignContract) return true;
      return (this.CurrentContract.IsFlashpointContract || this.CurrentContract.IsFlashpointCampaignContract)
        && !Main.Settings.AdditionalLanceSettings.DisableIfFlashpointContract;
    }

    public bool IsDroppingCustomControlledPlayerLance() {
      SpawnableUnit[] units = CurrentContract.Lances.GetLanceUnits(EncounterRules.EMPLOYER_TEAM_ID);
      if (units.Length > 0) return true;

      units = CurrentContract.Lances.GetLanceUnits(EncounterRules.PLAYER_TEAM_ID);
      if (units.Length > 4) return true;

      return false;
    }
  }
}