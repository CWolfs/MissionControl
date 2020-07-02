using UnityEngine;
using UnityEngine.SceneManagement;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

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
      if (Main.Settings.AdditionalPlayerMechs && MissionControl.Instance.IsDroppingCustomControlledPlayerLance()) new AddCustomPlayerMechsBatch(this);
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
        case LogicBlock.LogicType.SCENE_MANIPULATION:
          RunSceneManipulationLogic(logicBlocks, payload);
          MissionControl.Instance.IsMCLoadingFinished = true;
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

    private void RunSceneManipulationLogic(IEnumerable<LogicBlock> logicBlocks, RunPayload payload) {
      EncounterLayerGo = MissionControl.Instance.EncounterLayerGameObject;
      EncounterLayerData = MissionControl.Instance.EncounterLayerData;
      ChunkPlayerLanceGo = EncounterLayerGo.transform.Find(GetPlayerLanceChunkName()).gameObject;
      SpawnerPlayerLanceGo = ChunkPlayerLanceGo.transform.Find(GetPlayerLanceSpawnerName()).gameObject;
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
      Vector3 playerLanceSpawnPosition = SpawnerPlayerLanceGo.transform.position;
      GameObject plot = GetClosestPlot(playerLanceSpawnPosition);

      if (plot == null) {
        Main.Logger.Log($"[{this.GetType().Name}] GetPlotBaseName for map '{mapName}' is empty");
        State = EncounterState.FAILED;
        return "";
      }

      Main.Logger.Log($"[{this.GetType().Name}] Using plot name '{plot.name}'");
      return plot.name;
    }

    public static string GetPlayerLanceChunkName() {
      string type = MissionControl.Instance.CurrentContract.ContractTypeValue.Name;

      if (type == "ArenaSkirmish") {
        return "MultiPlayerSkirmishChunk";
      } else if ((type == "Story_1B_Retreat") || (type == "Story_2_ThreeYearsLater_Default")) {
        return "Gen_PlayerLance";
      }

      return "Chunk_PlayerLance";
    }

    public static string GetPlayerLanceSpawnerName() {
      string type = MissionControl.Instance.CurrentContract.ContractTypeValue.Name;

      if (type == "ArenaSkirmish") {
        return "Player1LanceSpawner";
      } else if ((type == "Story_1B_Retreat") || (type == "FireMission") || (type == "AttackDefend")) {
        return "PlayerLanceSpawner";
      } else if (type == "ThreeWayBattle") {
        return "PlayerLanceSpawner_Battle+";
      }

      return "Spawner_PlayerLance";
    }

    protected void BuildAdditionalLances(string enemyOrientationTargetKey, SpawnLogic.LookDirection enemyLookDirection,
      string allyOrientationKey, SpawnLogic.LookDirection allyLookDirection, float mustBeBeyondDistance, float mustBeWithinDistance) {

      BuildAdditionalLances(enemyOrientationTargetKey, enemyLookDirection, 100f, 400f, allyOrientationKey, allyLookDirection, mustBeBeyondDistance, mustBeWithinDistance);
    }

    protected void BuildAdditionalLances(string enemyOrientationTargetKey, SpawnLogic.LookDirection enemyLookDirection, float mustBeBeyondDistanceOfTarget, float mustBeWithinDistanceOfTarget,
      string allyOrientationKey, SpawnLogic.LookDirection allyLookDirection, float mustBeBeyondDistance, float mustBeWithinDistance) {

      Main.Logger.Log($"[{this.GetType().Name}] Building additional lance rules");

      int numberOfAdditionalEnemyLances = 0;

      if (MissionControl.Instance.AreAdditionalLancesAllowed("enemy")) {
        bool isPrimaryObjective = MissionControl.Instance.CurrentContractType.In(Main.Settings.AdditionalLanceSettings.IsPrimaryObjectiveIn.ToArray());
        bool displayToUser = !Main.Settings.AdditionalLanceSettings.HideObjective;
        bool excludeFromAutocomplete = MissionControl.Instance.CurrentContractType.In(Main.Settings.AdditionalLanceSettings.ExcludeFromAutocomplete.ToArray());

        Main.Logger.Log($"[{this.GetType().Name}] Excluding Additional Lance from contract type's autocomplete? {excludeFromAutocomplete}");

        if (Main.Settings.AdditionalLanceSettings.AlwaysDisplayHiddenObjectiveIfPrimary) {
          displayToUser = (isPrimaryObjective) ? true : displayToUser;
        }

        Main.Logger.Log($"[{this.GetType().Name}] Additional Lances will be primary objectives? {isPrimaryObjective}");
        FactionDef faction = MissionControl.Instance.GetFactionFromTeamType("enemy");

        numberOfAdditionalEnemyLances = Main.Settings.ActiveAdditionalLances.Enemy.SelectNumberOfAdditionalLances(faction, "enemy");
        if (Main.Settings.DebugMode && (Main.Settings.Debug.AdditionalLancesEnemyLanceCount > -1)) numberOfAdditionalEnemyLances = Main.Settings.Debug.AdditionalLancesEnemyLanceCount;

        // Allow Flashpoint contract settings overrides to force their respective setting
        if (Main.Settings.ActiveFlashpointSettings.Has(FlashpointSettingsOverrides.AdditionalLances_EnemyLanceCountOverride)) {
          numberOfAdditionalEnemyLances = Main.Settings.ActiveFlashpointSettings.GetInt(FlashpointSettingsOverrides.AdditionalLances_EnemyLanceCountOverride);
          Main.Logger.Log($"[{this.GetType().Name}] Using Flashpoint settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Enemy lance count will be '{numberOfAdditionalEnemyLances}'.");
        }

        bool showObjectiveOnLanceDetected = Main.Settings.AdditionalLanceSettings.ShowObjectiveOnLanceDetected;

        int objectivePriority = -10;

        for (int i = 0; i < numberOfAdditionalEnemyLances; i++) {
          if (MissionControl.Instance.CurrentContractType == "ArenaSkirmish") {
            new AddPlayer2LanceWithDestroyObjectiveBatch(this, enemyOrientationTargetKey, enemyLookDirection, mustBeBeyondDistanceOfTarget, mustBeWithinDistanceOfTarget,
              $"Destroy Enemy Support Lance {i + 1}", objectivePriority--, isPrimaryObjective, displayToUser, showObjectiveOnLanceDetected, excludeFromAutocomplete);
          } else {
            new AddTargetLanceWithDestroyObjectiveBatch(this, enemyOrientationTargetKey, enemyLookDirection, mustBeBeyondDistanceOfTarget, mustBeWithinDistanceOfTarget,
              $"Destroy {{TEAM_TAR.FactionDef.Demonym}} Support Lance {i + 1}", objectivePriority--, isPrimaryObjective, displayToUser, showObjectiveOnLanceDetected, excludeFromAutocomplete);
          }
        }
      }

      if (MissionControl.Instance.AreAdditionalLancesAllowed("allies")) {
        FactionDef faction = MissionControl.Instance.GetFactionFromTeamType("allies");

        int numberOfAdditionalAllyLances = Main.Settings.ActiveAdditionalLances.Allies.SelectNumberOfAdditionalLances(faction, "allies");

        // Allow Flashpoint contract settings overrides to force their respective setting
        if (Main.Settings.ActiveFlashpointSettings.Has(FlashpointSettingsOverrides.AdditionalLances_AllyLanceCountOverride)) {
          numberOfAdditionalAllyLances = Main.Settings.ActiveFlashpointSettings.GetInt(FlashpointSettingsOverrides.AdditionalLances_AllyLanceCountOverride);
          Main.Logger.Log($"[{this.GetType().Name}] Using Flashpoint settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Ally lance count will be '{numberOfAdditionalAllyLances}'.");
        } else if (Main.Settings.AdditionalLanceSettings.MatchAllyLanceCountToEnemy) {
          Main.Logger.LogDebug($"[{this.GetType().Name}] 'MatchAllyLanceCountToEnemy' is on. Ally lance count will be {numberOfAdditionalEnemyLances}");
          numberOfAdditionalAllyLances = numberOfAdditionalEnemyLances;
        }

        for (int i = 0; i < numberOfAdditionalAllyLances; i++) {
          new AddEmployerLanceBatch(this, allyOrientationKey, allyLookDirection, mustBeBeyondDistance, mustBeWithinDistance);
        }
      }
    }

    protected void MaximiseEncounterBoundary() {
      string mapId = MissionControl.Instance.ContractMapName;
      string contractTypeName = MissionControl.Instance.CurrentContractType;
      float size = Main.Settings.ExtendedBoundaries.GetSizePercentage(mapId, contractTypeName);

      // Allow Flashpoint contract settings overrides to force their respective setting
      if (Main.Settings.ActiveFlashpointSettings.Has(FlashpointSettingsOverrides.ExtendedBoundaries_IncreaseBoundarySizeByPercentage)) {
        size = Main.Settings.ActiveFlashpointSettings.GetInt(FlashpointSettingsOverrides.ExtendedBoundaries_IncreaseBoundarySizeByPercentage);
        Main.Logger.Log($"[{this.GetType().Name}] Using Flashpoint settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. IncreaseBoundarySizeByPercentage will be '{size}'.");
      }

      Main.Logger.Log($"[{this.GetType().Name}] Maximising Boundary Size for '{mapId}.{contractTypeName}' to '{size}'");

      this.EncounterLogic.Add(new MaximiseBoundarySize(this, size));
    }

    private void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>() { Tags.EMPLOYER_TEAM }, IssueAIOrderTo.ToLance, new List<string>() { Tags.PLAYER_1_TEAM }));
    }
  }
}