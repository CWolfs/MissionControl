using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using MissionControl.Data;
using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.Utils;
using MissionControl.EncounterFactories;
using MissionControl.ContractTypeBuilders;
using MissionControl.Patches;

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

    public Contract CurrentContract { get; set; }
    public string ContractMapName { get; private set; }
    public string CurrentContractType { get; private set; } = "INVALID_UNSET";
    public ContractTypeValue CurrentContractTypeValue { get; private set; }

    public EncounterRules EncounterRules { get; private set; }
    public string EncounterRulesName { get; private set; }
    public GameObject EncounterLayerParentGameObject { get; private set; }
    public EncounterLayerParent EncounterLayerParent { get; private set; }
    public GameObject EncounterLayerGameObject { get; private set; }
    public EncounterLayerData EncounterLayerData { get; private set; }

    public int PlayerLanceDropDifficultyValue { get; set; }
    public float PlayerLanceDropSkullRating { get; set; }
    public float PlayerLanceDropTonnage { get; set; }

    // Only populated for custom contract types
    public EncounterLayer_MDD EncounterLayerMDD { get; private set; }
    public bool IsCustomContractType { get; set; } = false;
    public List<object[]> QueuedBuildingMounts { get; set; } = new List<object[]>();

    public HexGrid HexGrid { get; private set; }

    public bool IsContractValid { get; private set; } = false;
    public bool IsMCLoadingFinished { get; set; } = false;
    public bool IsLoadingFromSave { get; set; } = false;

    private Dictionary<string, List<Type>> AvailableEncounters = new Dictionary<string, List<Type>>();

    public Dictionary<ContractStats, object> ContractStats = new Dictionary<ContractStats, object>();

    // Used at Contract generation
    public StarSystem System { get; set; }
    public Dictionary<int, List<ContractOverride>> PotentialContracts { get; set; } = new Dictionary<int, List<ContractOverride>>();
    // End

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

      // Custom Contract Types
      AddEncounter("Blackout", typeof(BlackoutEncounterRules));

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
      List<string> contractTypesWithSpecificEncounterRules = new List<string>(AvailableEncounters.Keys);
      List<string> customContractTypes = DataManager.Instance.GetCustomContractTypes();

      List<string> contractTypes = contractTypesWithSpecificEncounterRules.Union(customContractTypes).ToList();
      return contractTypes;
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
      Main.Logger.Log($"[MissionControl] Setting contract '{contract.Name}' for contract type '{contract.ContractTypeValue.Name}'");
      CurrentContract = contract;

      if (AllowMissionControl()) {
        Main.Logger.Log($"[MissionControl] Player drop difficulty: '{PlayerLanceDropDifficultyValue}' (Skull value '{PlayerLanceDropSkullRating}')");
        Main.Logger.Log($"[MissionControl] Player drop tonnage: '{PlayerLanceDropTonnage}' tons");

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
      ClearOldCaches();
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

    private void ClearOldCaches() {
      Main.Logger.Log($"[MissionControl] Clearing old caches");

      AssetBundleManagerGetAssetFromBundlePatch.ClearLookup();
    }

    public void SetActiveAdditionalLances(Contract contract) {
      if (Main.Settings.AdditionalLanceSettings.SkullValueMatters) {
        if (!IsSkirmish(contract) || (IsSkirmish(contract) && !Main.Settings.AdditionalLanceSettings.UseGeneralProfileForSkirmish)) {
          int difficulty = contract.Override.finalDifficulty;
          Main.LogDebug($"[MissionControl] Contract difficulty '{difficulty}' (Skull value '{(float)difficulty / 2f}')");
          if (Main.Settings.AdditionalLanceSettings.BasedOnVisibleSkullValue) {
            difficulty = contract.Override.GetUIDifficulty();
          }
          Main.LogDebug($"[MissionControl] Visible Contract difficulty '{contract.Override.GetUIDifficulty()}' (Skull value '{(float)contract.Override.GetUIDifficulty() / 2f}')");

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

    public void SetFlashpointOverride() {
      string contractId = CurrentContract.Override.ID;
      if (Main.Settings.FlashpointSettingsOverrides.ContainsKey(contractId)) {
        Main.Logger.Log($"[MissionControl] Setting a Flashpoint settings override for '{contractId}'.");
        Main.Settings.ActiveFlashpointSettings = Main.Settings.FlashpointSettingsOverrides[contractId];
      } else {
        Main.Logger.Log($"[MissionControl] No Flashpoint settings override found for '{contractId}'.");
        Main.Settings.ActiveFlashpointSettings = new Config.FlashpointSettingsOverrides();
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
      // Allow Flashpoint contract settings overrides to force their respective setting
      bool areLancesAllowed = IsAnyFlashpointContract() && Main.Settings.ActiveFlashpointSettings.Has("AdditionalLances.Enable") && Main.Settings.ActiveFlashpointSettings.GetBool("AdditionalLances.Enable");
      if (areLancesAllowed) return true;

      if (Main.Settings.AdditionalLanceSettings.Enable) {
        areLancesAllowed = !Main.Settings.AdditionalLanceSettings.IsTeamDisabled(teamType);
        if (areLancesAllowed) areLancesAllowed = !IsAnyFlashpointContract() || (IsAnyFlashpointContract() && Main.Settings.AdditionalLanceSettings.EnableForFlashpoints);
        if (areLancesAllowed) areLancesAllowed = Main.Settings.AdditionalLanceSettings.GetValidContractTypes().Contains(CurrentContractType);
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
        bool isExtendedBoundariesAllowed = !IsAnyFlashpointContract() || (IsAnyFlashpointContract() && Main.Settings.ExtendedBoundaries.EnableForFlashpoints);
        if (isExtendedBoundariesAllowed) isExtendedBoundariesAllowed = Main.Settings.ExtendedBoundaries.GetValidContractTypes().Contains(CurrentContractType);
        return isExtendedBoundariesAllowed;
      }
      return false;
    }

    public bool IsExtendedLancesAllowed() {
      // Allow Flashpoint contract settings overrides to force their respective setting
      bool isExtendedLancesAllowed = IsAnyFlashpointContract() && Main.Settings.ActiveFlashpointSettings.Has("ExtendedLances.Enable") && Main.Settings.ActiveFlashpointSettings.GetBool("ExtendedLances.Enable");
      if (isExtendedLancesAllowed) return true;

      if (Main.Settings.ExtendedLances.Enable) {
        isExtendedLancesAllowed = !IsAnyFlashpointContract() || (IsAnyFlashpointContract() && Main.Settings.ExtendedLances.EnableForFlashpoints);
        if (isExtendedLancesAllowed) isExtendedLancesAllowed = Main.Settings.ExtendedLances.GetValidContractTypes().Contains(CurrentContractType);
        return isExtendedLancesAllowed;
      }
      return false;
    }

    public bool IsRandomSpawnsAllowed() {
      // Allow Flashpoint contract settings overrides to force their respective setting
      bool isRandomSpawnsAllowed = IsAnyFlashpointContract() && Main.Settings.ActiveFlashpointSettings.Has("RandomSpawns.Enable") && Main.Settings.ActiveFlashpointSettings.GetBool("RandomSpawns.Enable");
      if (isRandomSpawnsAllowed) return true;

      if (Main.Settings.RandomSpawns.Enable) {
        isRandomSpawnsAllowed = !IsAnyFlashpointContract() || (IsAnyFlashpointContract() && Main.Settings.RandomSpawns.EnableForFlashpoints);
        if (isRandomSpawnsAllowed) isRandomSpawnsAllowed = Main.Settings.RandomSpawns.GetValidContractTypes().Contains(CurrentContractType);
        return isRandomSpawnsAllowed;
      }
      return false;
    }

    public bool IsDynamicWithdrawAllowed() {
      bool dynamicWithdrawAllowed = !IsAnyFlashpointContract() || (IsAnyFlashpointContract() && Main.Settings.DynamicWithdraw.EnableForFlashpoints);
      if (dynamicWithdrawAllowed) dynamicWithdrawAllowed = Main.Settings.DynamicWithdraw.Enable && !MissionControl.Instance.IsSkirmish();
      return dynamicWithdrawAllowed;
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

    public bool IsAnyFlashpointContract() {
      return this.CurrentContract.IsFlashpointContract || this.CurrentContract.IsFlashpointCampaignContract;
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
      if (IsLoadingFromSave) return false;
      if (CurrentContract.IsStoryContract) return false;
      if (CurrentContract.IsRestorationContract) return false;
      if (!IsAnyFlashpointContract()) return true;
      if (IsAnyFlashpointContract() && Main.Settings.ActiveFlashpointSettings.Enabled) return true;
      if (IsAnyFlashpointContract() && !Main.Settings.EnableFlashpointOverrides) return false;

      return false;
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