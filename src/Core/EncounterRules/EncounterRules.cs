using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Data;
using MissionControl.Logic;
using MissionControl.Trigger;
using MissionControl.Config;

namespace MissionControl.Rules {
  public abstract class EncounterRules {
    public enum EncounterState { NOT_STARTED, RUNNING, FAILED, FINISHED };

    public const string PLAYER_TEAM_ID = "bf40fd39-ccf9-47c4-94a6-061809681140";
    public const string PLAYER_2_TEAM_ID = "757173dd-b4e1-4bb5-9bee-d78e623cc867";
    public const string EMPLOYER_TEAM_ID = "ecc8d4f2-74b4-465d-adf6-84445e5dfc230";
    public const string TARGET_TEAM_ID = "be77cadd-e245-4240-a93e-b99cc98902a5";
    public const string TARGETS_ALLY_TEAM_ID = "31151ed6-cfc2-467e-98c4-9ae5bea784cf";
    public const string NEUTRAL_TO_ALL_TEAM_ID = "61612bb3-abf9-4586-952a-0559fa9dcd75";
    public const string HOSTILE_TO_ALL_TEAM_ID = "3c9f3a20-ab03-4bcb-8ab6-b1ef0442bbf0";

    protected GameObject EncounterLayerGo { get; set; }
    protected EncounterLayerData EncounterLayerData { get; private set; }
    protected GameObject ChunkPlayerLanceGo { get; set; }
    public GameObject SpawnerPlayerLanceGo { get; set; }

    public List<LogicBlock> EncounterLogic { get; private set; } = new List<LogicBlock>();
    public Dictionary<string, GameObject> ObjectLookup { get; private set; } = new Dictionary<string, GameObject>();
    public List<string> ObjectReferenceQueue { get; private set; } = new List<string>();

    public EncounterState State { get; protected set; } = EncounterState.NOT_STARTED;

    public EncounterRules() {
      ActivatePreFeatures();
    }

    public void ActivatePreFeatures() {
      if (MissionControl.Instance.IsExtendedBoundariesAllowed()) MaximiseEncounterBoundary();
    }

    public void ActivatePostFeatures() {
      if (MissionControl.Instance.AreAdditionalPlayerMechsAllowed() && MissionControl.Instance.IsDroppingCustomControlledPlayerLance()) new AddCustomPlayerMechsBatch(this);
      if (MissionControl.Instance.IsExtendedLancesAllowed()) new AddExtraLanceSpawnsForExtendedLancesBatch(this);
      if (MissionControl.Instance.IsDynamicWithdrawAllowed()) new AddDynamicWithdrawBatch(this);
      BuildAi();
    }

    public abstract void Build();

    public abstract void LinkObjectReferences(string mapName);

    public virtual void Run(LogicBlock.LogicType type, RunPayload payload) {
      IEnumerable<LogicBlock> logicBlocks = EncounterLogic.Where(logic => logic.Type == type);

      switch (type) {
        case LogicBlock.LogicType.RESOURCE_REQUEST:
          State = EncounterState.RUNNING;
          RunGeneralLogic(logicBlocks, payload);
          break;
        case LogicBlock.LogicType.CONTRACT_OVERRIDE_MANIPULATION:
          RunGeneralLogic(logicBlocks, payload);
          break;
        case LogicBlock.LogicType.ENCOUNTER_MANIPULATION:
          RunGeneralLogic(logicBlocks, payload);
          break;
        case LogicBlock.LogicType.REQUEST_LANCE_COMPLETE:
          RunGeneralLogic(logicBlocks, payload);
          break;
        case LogicBlock.LogicType.SCENE_MANIPULATION:
          RunSceneManipulationLogic(logicBlocks, payload);
          MissionControl.Instance.SetFinishedLoading();
          break;
        default:
          Main.Logger.LogError($"[EncounterRules] Unknown logic type '{type}'");
          break;
      }
    }

    private void RunGeneralLogic(IEnumerable<LogicBlock> logicBlocks, RunPayload payload) {
      foreach (LogicBlock logicBlock in logicBlocks) {
        logicBlock.Run(payload);
      }
    }

    /* DEPRECATED IN MC 1.2.0 */
    public static string GetPlayerLanceChunkName() {
      Main.Logger.LogWarning($"[MC 1.2+ DEPRECATION] 'EncounterRules.GetPlayerLanceChunkName()' IS DEPRECATED. USE 'EncounterRules.GetPlayerLanceChunkGameObject(GameObject encounterLayerGo)' INSTEAD. IT WILL BE REMOVED IN A FUTURE UPDATE.");
      Main.Logger.LogWarning($"[MC 1.2+ DEPRECATION] 'EncounterRules.GetPlayerLanceChunkName()' IS DEPRECATED. USE 'EncounterRules.GetPlayerLanceChunkGameObject(GameObject encounterLayerGo)' INSTEAD. IT WILL BE REMOVED IN A FUTURE UPDATE.");
      Main.Logger.LogWarning($"[MC 1.2+ DEPRECATION] 'EncounterRules.GetPlayerLanceChunkName()' IS DEPRECATED. USE 'EncounterRules.GetPlayerLanceChunkGameObject(GameObject encounterLayerGo)' INSTEAD. IT WILL BE REMOVED IN A FUTURE UPDATE.");

      GameObject encounterLayerGo = MissionControl.Instance.EncounterLayerGameObject;
      GameObject chunkPlayerLanceGo = EncounterRules.GetPlayerLanceChunkGameObject(encounterLayerGo);
      return chunkPlayerLanceGo.name;
    }

    /* DEPRECATED IN MC 1.2.0 */
    public static string GetPlayerLanceSpawnerName() {
      Main.Logger.LogWarning($"[MC 1.2+ DEPRECATION] 'EncounterRules.GetPlayerLanceSpawnerName()' IS DEPRECATED. USE 'EncounterRules.GetPlayerSpawnerGameObject(GameObject encounterLayerGo)' INSTEAD. IT WILL BE REMOVED IN A FUTURE UPDATE.");
      Main.Logger.LogWarning($"[MC 1.2+ DEPRECATION] 'EncounterRules.GetPlayerLanceSpawnerName()' IS DEPRECATED. USE 'EncounterRules.GetPlayerSpawnerGameObject(GameObject encounterLayerGo)' INSTEAD. IT WILL BE REMOVED IN A FUTURE UPDATE.");
      Main.Logger.LogWarning($"[MC 1.2+ DEPRECATION] 'EncounterRules.GetPlayerLanceSpawnerName()' IS DEPRECATED. USE 'EncounterRules.GetPlayerSpawnerGameObject(GameObject encounterLayerGo)' INSTEAD. IT WILL BE REMOVED IN A FUTURE UPDATE.");

      GameObject encounterLayerGo = MissionControl.Instance.EncounterLayerGameObject;
      GameObject chunkPlayerLanceGo = EncounterRules.GetPlayerLanceChunkGameObject(encounterLayerGo);
      GameObject SpawnerPlayerLanceGo = GetPlayerSpawnerGameObject(chunkPlayerLanceGo);
      return SpawnerPlayerLanceGo.name;
    }

    public static GameObject GetPlayerLanceChunkGameObject(GameObject encounterLayerGo) {
      string type = MissionControl.Instance.CurrentContract.ContractTypeValue.Name;

      if (type == "ArenaSkirmish") {
        return encounterLayerGo.transform.Find("MultiPlayerSkirmishChunk").gameObject;
      }

      return encounterLayerGo.GetComponentInChildren<PlayerLanceChunkGameLogic>().gameObject;
    }

    public static GameObject GetPlayerSpawnerGameObject(GameObject chunkPlayerLanceGo) {
      string type = MissionControl.Instance.CurrentContract.ContractTypeValue.Name;

      if (type == "ArenaSkirmish") {
        return chunkPlayerLanceGo.transform.Find("Player1LanceSpawner").gameObject;
      }

      return chunkPlayerLanceGo.GetComponentInChildren<PlayerLanceSpawnerGameLogic>().gameObject;
    }

    public static GameObject GetAnyLanceSpawnerGameObject(GameObject encounterLayerGo) {
      return encounterLayerGo.GetComponentInChildren<LanceSpawnerGameLogic>().gameObject;
    }

    private void RunSceneManipulationLogic(IEnumerable<LogicBlock> logicBlocks, RunPayload payload) {
      EncounterLayerGo = MissionControl.Instance.EncounterLayerGameObject;
      EncounterLayerData = MissionControl.Instance.EncounterLayerData;

      ChunkPlayerLanceGo = GetPlayerLanceChunkGameObject(EncounterLayerGo);
      SpawnerPlayerLanceGo = GetPlayerSpawnerGameObject(ChunkPlayerLanceGo);

      ObjectLookup["ChunkPlayerLance"] = ChunkPlayerLanceGo;
      ObjectLookup["SpawnerPlayerLance"] = SpawnerPlayerLanceGo;

      string mapName = MissionControl.Instance.ContractMapName;

      LinkObjectReferences(mapName);

      foreach (string objectName in ObjectReferenceQueue) {
        ObjectLookup[objectName] = EncounterLayerData.gameObject.FindRecursive(objectName);
      }

      if (State == EncounterState.RUNNING) {
        try {
          foreach (SceneManipulationLogic sceneManipulationLogic in logicBlocks) {
            sceneManipulationLogic.Run(payload);
          }
        } catch (Exception e) {
          Main.Logger.LogError($"[{this.GetType().Name}] Encounter has failed. A LogicBlock failed. Full stack trace - {e}");
        }
      } else {
        Main.Logger.LogError($"[{this.GetType().Name}] Encounter has failed");
        return;
      }

      State = EncounterState.FINISHED;
    }

    public List<string> GenerateGuids(int amountRequired) {
      List<string> guids = new List<string>();

      for (int i = 0; i < amountRequired; i++) {
        guids.Add(Guid.NewGuid().ToString());
      }

      return guids;
    }

    private bool IsPlotValidForEncounter(Transform plotTransform, string name = "PlotVariant") {
      Transform selectedPlotTransform = plotTransform.FindIgnoreCaseStartsWith(name);

      if (selectedPlotTransform == null) {
        return false;
      }

      GameObject selectedPlotGo = selectedPlotTransform.gameObject;
      if (selectedPlotGo.activeSelf) return true;

      return false;
    }

    private GameObject CheckAllPlotsForValidPlot(GameObject plotsParent, Vector3 origin, string name) {
      GameObject closestPlot = null;
      float closestDistance = -1;

      foreach (Transform t in plotsParent.transform) {
        Vector3 plotPosition = t.position;
        if (EncounterLayerData.IsInEncounterBounds(plotPosition)) {
          if (IsPlotValidForEncounter(t, name)) {
            float distance = Vector3.Distance(t.position, origin);
            if (closestDistance == -1 || closestDistance < distance) {
              closestDistance = distance;
              closestPlot = t.gameObject;
            }
          }
        }
      }

      return closestPlot;
    }

    protected GameObject GetClosestPlot(Vector3 origin) {
      GameObject plotsParentGo = GameObject.Find("PlotParent");
      GameObject closestPlot = null;

      closestPlot = CheckAllPlotsForValidPlot(plotsParentGo, origin, "plotVariant_");

      if (closestPlot == null) {
        closestPlot = CheckAllPlotsForValidPlot(plotsParentGo, origin, "PlotVariant");
      }

      // If no plot is found, select on a secondary fallback
      if (closestPlot == null) {
        closestPlot = CheckAllPlotsForValidPlot(plotsParentGo, origin, "Default");
      }

      return closestPlot;
    }

    protected string GetPlotBaseName(string mapName) {
      GameObject plotGO = GetPlotBaseGO(mapName);

      if (plotGO == null) {
        Main.Logger.Log($"[{this.GetType().Name}] GetPlotBaseName for map '{mapName}' is empty");
        State = EncounterState.FAILED;
        return null;
      }

      Main.Logger.Log($"[{this.GetType().Name}] Using plot name '{plotGO.name}'");
      return plotGO.name;
    }

    protected GameObject GetPlotBaseGO(string mapName) {
      Vector3 playerLanceSpawnPosition = SpawnerPlayerLanceGo.transform.position;
      GameObject plotGO = GetClosestPlot(playerLanceSpawnPosition);

      if (plotGO == null) {
        Main.Logger.Log($"[{this.GetType().Name}] GetPlotBaseGO for map '{mapName}' is empty");
        State = EncounterState.FAILED;
        return null;
      }

      Main.Logger.Log($"[{this.GetType().Name}] Using plot '{plotGO.name}'");
      return plotGO;
    }

    protected void BuildAdditionalLances(string enemyOrientationTargetKey, SpawnLogic.LookDirection enemyLookDirection,
      string allyOrientationKey, SpawnLogic.LookDirection allyLookDirection, float mustBeBeyondDistance, float mustBeWithinDistance) {

      BuildAdditionalLances(enemyOrientationTargetKey, enemyLookDirection, 100f, 400f, allyOrientationKey, allyLookDirection, mustBeBeyondDistance, mustBeWithinDistance);
    }

    /*
    There are three ways to get ALs 
      - Default - rolling AL chances and selecting a lance
      - Per-contract Overrides - specify the specific lances by LanceDef or MC lance key
      - API override - a caller specifies the lance count and LanceOverrides
    */
    protected void BuildAdditionalLances(string enemyOrientationTargetKey, SpawnLogic.LookDirection enemyLookDirection, float mustBeBeyondDistanceOfTarget, float mustBeWithinDistanceOfTarget,
      string allyOrientationKey, SpawnLogic.LookDirection allyLookDirection, float mustBeBeyondDistance, float mustBeWithinDistance) {

      Main.Logger.Log($"[{this.GetType().Name}] Building additional lance rules");

      int numberOfAdditionalEnemyLances = 0;

      if (MissionControl.Instance.AreAdditionalLancesAllowed("enemy")) {
        List<string> manuallySpecifiedLances = new List<string>();
        List<MLanceOverride> manuallySpecifiedLanceOverrides = new List<MLanceOverride>();

        bool isPrimaryObjective = MissionControl.Instance.CurrentContractType.In(Main.Settings.AdditionalLanceSettings.IsPrimaryObjectiveIn.ToArray());
        bool displayToUser = !Main.Settings.AdditionalLanceSettings.HideObjective;
        bool excludeFromAutocomplete = MissionControl.Instance.CurrentContractType.In(Main.Settings.AdditionalLanceSettings.ExcludeFromAutocomplete.ToArray());
        bool showObjectiveOnLanceDetected = Main.Settings.AdditionalLanceSettings.ShowObjectiveOnLanceDetected;
        int objectivePriority = -10;

        Main.Logger.Log($"[{this.GetType().Name}] Excluding Additional Lance from contract type's autocomplete? {excludeFromAutocomplete}");

        if (Main.Settings.AdditionalLanceSettings.AlwaysDisplayHiddenObjectiveIfPrimary) {
          displayToUser = (isPrimaryObjective) ? true : displayToUser;
        }

        Main.Logger.Log($"[{this.GetType().Name}] Additional Lances will be primary objectives? {isPrimaryObjective}");
        FactionDef faction = MissionControl.Instance.GetFactionFromTeamType("enemy");

        bool reportOverrideNotAllowed = MissionControl.Instance.IsAnyStoryOrFlashpointContract() && MissionControl.Instance.API.HasOverriddenAdditionalLances("enemy");
        if (reportOverrideNotAllowed) Main.Logger.LogDebug($"[{this.GetType().Name}] API override detected for enemy Additional Lance but a story or flashpoint contract is loaded. API overrides are not allowed for flashpoints or story contracts.");

        if (!MissionControl.Instance.IsAnyStoryOrFlashpointContract() && MissionControl.Instance.API.HasOverriddenAdditionalLances("enemy")) {
          // API DRIVEN
          numberOfAdditionalEnemyLances = MissionControl.Instance.API.GetOverriddenAdditionalLanceCount("enemy");
          manuallySpecifiedLanceOverrides = MissionControl.Instance.API.GetOverriddenAdditionalLances("enemy");
          Main.Logger.Log($"[{this.GetType().Name}] [API OVERRIDDEN] Enemy additional Lance count will be '{numberOfAdditionalEnemyLances}'");
        } else {
          // PER-CONTRACT OVERRIDE OR DEFAULT DRIVEN
          numberOfAdditionalEnemyLances = GetNumberOfLances("enemy", faction);

          if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.AdditionalLances_EnemyLancesOverride)) {
            manuallySpecifiedLances = Main.Settings.ActiveContractSettings.GetList<string>(ContractSettingsOverrides.AdditionalLances_EnemyLancesOverride);
            Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'.");

            foreach (string lanceKey in manuallySpecifiedLances) {
              MLanceOverride lanceOverride = DataManager.Instance.GetLanceOverride(lanceKey);
              manuallySpecifiedLanceOverrides.Add(lanceOverride);
            }
          }
        }

        MissionControl.Instance.Metrics.NumberOfTargetAdditionalLances = numberOfAdditionalEnemyLances;

        for (int i = 1; i <= numberOfAdditionalEnemyLances; i++) {
          if (MissionControl.Instance.CurrentContractType == "ArenaSkirmish") {
            new AddPlayer2LanceWithDestroyObjectiveBatch(this, enemyOrientationTargetKey, enemyLookDirection, mustBeBeyondDistanceOfTarget, mustBeWithinDistanceOfTarget,
              $"Destroy Enemy Support Lance {i}", objectivePriority--, isPrimaryObjective, displayToUser, showObjectiveOnLanceDetected, excludeFromAutocomplete);
          } else {
            if (manuallySpecifiedLanceOverrides.Count >= i) {
              MLanceOverride lanceOverride = manuallySpecifiedLanceOverrides[i - 1];

              Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Resolved Enemy lance will be '{lanceOverride.LanceKey}'.");
              new AddTargetLanceWithDestroyObjectiveBatch(this, enemyOrientationTargetKey, enemyLookDirection, mustBeBeyondDistanceOfTarget, mustBeWithinDistanceOfTarget,
                $"Destroy {{TEAM_TAR.FactionDef.Demonym}} Support Lance {i}", objectivePriority--, isPrimaryObjective, displayToUser, showObjectiveOnLanceDetected, excludeFromAutocomplete, lanceOverride);
            } else {
              new AddTargetLanceWithDestroyObjectiveBatch(this, enemyOrientationTargetKey, enemyLookDirection, mustBeBeyondDistanceOfTarget, mustBeWithinDistanceOfTarget,
                $"Destroy {{TEAM_TAR.FactionDef.Demonym}} Support Lance {i}", objectivePriority--, isPrimaryObjective, displayToUser, showObjectiveOnLanceDetected, excludeFromAutocomplete);
            }
          }
        }
      }

      if (MissionControl.Instance.AreAdditionalLancesAllowed("allies")) {
        List<string> manuallySpecifiedLances = new List<string>();
        List<MLanceOverride> manuallySpecifiedLanceOverrides = new List<MLanceOverride>();

        FactionDef faction = MissionControl.Instance.GetFactionFromTeamType("allies");
        int numberOfAdditionalAllyLances = 0;

        bool reportOverrideNotAllowed = MissionControl.Instance.IsAnyStoryOrFlashpointContract() && MissionControl.Instance.API.HasOverriddenAdditionalLances("allies");
        if (reportOverrideNotAllowed) Main.Logger.LogDebug($"[{this.GetType().Name}] API override detected for allies Additional Lance but a story or flashpoint contract is loaded. API overrides are not allowed for flashpoints or story contracts.");

        if (!MissionControl.Instance.IsAnyStoryOrFlashpointContract() && MissionControl.Instance.API.HasOverriddenAdditionalLances("allies")) {
          // API DRIVEN
          numberOfAdditionalAllyLances = MissionControl.Instance.API.GetOverriddenAdditionalLanceCount("allies");
          manuallySpecifiedLanceOverrides = MissionControl.Instance.API.GetOverriddenAdditionalLances("allies");
          Main.Logger.Log($"[{this.GetType().Name}] [API OVERRIDDEN] Ally additional Lance count will be '{numberOfAdditionalAllyLances}'");
        } else {
          // PER-CONTRACT OVERRIDE OR DEFAULT DRIVEN
          numberOfAdditionalAllyLances = GetNumberOfLances("allies", faction);

          if (Main.Settings.AdditionalLanceSettings.MatchAllyLanceCountToEnemy) {
            Main.Logger.LogDebug($"[{this.GetType().Name}] 'MatchAllyLanceCountToEnemy' is on. Ally lance count will be {numberOfAdditionalEnemyLances}");
            numberOfAdditionalAllyLances = numberOfAdditionalEnemyLances;
          }

          if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.AdditionalLances_AllyLancesOverride)) {
            manuallySpecifiedLances = Main.Settings.ActiveContractSettings.GetList<string>(ContractSettingsOverrides.AdditionalLances_AllyLancesOverride);
            Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'.");

            foreach (string lanceKey in manuallySpecifiedLances) {
              MLanceOverride lanceOverride = DataManager.Instance.GetLanceOverride(lanceKey);
              manuallySpecifiedLanceOverrides.Add(lanceOverride);
            }
          }
        }

        MissionControl.Instance.Metrics.NumberOfEmployerAdditionalLances = numberOfAdditionalAllyLances;

        for (int i = 1; i <= numberOfAdditionalAllyLances; i++) {
          if (manuallySpecifiedLanceOverrides.Count >= i) {
            MLanceOverride lanceOverride = manuallySpecifiedLanceOverrides[i - 1];

            Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Resolved Ally lance will be '{lanceOverride.LanceKey}'.");
            new AddEmployerLanceBatch(this, allyOrientationKey, allyLookDirection, mustBeBeyondDistance, mustBeWithinDistance, lanceOverride);
          } else {
            new AddEmployerLanceBatch(this, allyOrientationKey, allyLookDirection, mustBeBeyondDistance, mustBeWithinDistance);
          }
        }
      }
    }

    /*
    Used for:
      - Per-Contract Override, AND
      - Default

      NOT: API overridden
    */
    private int GetNumberOfLances(string teamType, FactionDef faction) {
      // If Per-Contract Override
      // Allow contract-specific settings overrides to force their respective setting
      string AdditionalLances_TeamTypeLanceCountOverride = teamType == "allies" ? ContractSettingsOverrides.AdditionalLances_AllyLanceCountOverride : ContractSettingsOverrides.AdditionalLances_EnemyLanceCountOverride;

      if (Main.Settings.ActiveContractSettings.Has(AdditionalLances_TeamTypeLanceCountOverride)) {
        int numberOfAdditionalLances = Main.Settings.ActiveContractSettings.GetInt(AdditionalLances_TeamTypeLanceCountOverride);
        Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. '{teamType}' lance count will be '{numberOfAdditionalLances}'.");
        return numberOfAdditionalLances;
      }

      // Default
      if (teamType == "allies") {
        int numberOfAdditionalLances = Main.Settings.ActiveAdditionalLances.Allies.SelectNumberOfAdditionalLances(faction, teamType);

        return numberOfAdditionalLances;
      } else if (teamType == "enemy") {
        int numberOfAdditionalLances = Main.Settings.ActiveAdditionalLances.Enemy.SelectNumberOfAdditionalLances(faction, teamType);

        if (Main.Settings.DebugMode && (Main.Settings.Debug.AdditionalLancesEnemyLanceCount > -1)) numberOfAdditionalLances = Main.Settings.Debug.AdditionalLancesEnemyLanceCount;

        return numberOfAdditionalLances;
      } else {
        Main.Logger.LogError($"[EncounterRules.GetNumberOfLances] Unknown team type provided of '{teamType}'.");
      }

      return 0;
    }

    protected void MaximiseEncounterBoundary() {
      string mapId = MissionControl.Instance.ContractMapName;
      string contractTypeName = MissionControl.Instance.CurrentContractType;
      float size = Main.Settings.ExtendedBoundaries.GetSizePercentage(mapId, contractTypeName);

      // Allow contract-specific settings overrides to force their respective setting
      if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedBoundaries_IncreaseBoundarySizeByPercentage)) {
        size = Main.Settings.ActiveContractSettings.GetInt(ContractSettingsOverrides.ExtendedBoundaries_IncreaseBoundarySizeByPercentage);
        Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. IncreaseBoundarySizeByPercentage will be '{size}'.");
      }

      Main.Logger.Log($"[{this.GetType().Name}] Maximising Boundary Size for '{mapId}.{contractTypeName}' to '{size}'");

      this.EncounterLogic.Add(new MaximiseBoundarySize(this, size));
    }

    private void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>() { Tags.EMPLOYER_TEAM }, IssueAIOrderTo.ToLance, new List<string>() { Tags.PLAYER_1_TEAM }));
    }
  }
}