using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Messages;
using MissionControl.Result;
using MissionControl.Conditional;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class ContractTypeBuilder {
    public JObject ContractTypeBuild { get; set; }
    public GameObject EncounterLayerGo { get; set; }
    public string ContractTypeKey { get; set; } = "UNSET";

    private const string TEAMS_ID = "Teams";
    private const string PLOTS_ID = "Plots";
    private const string CHUNKS_ID = "Chunks";
    private const string TRIGGERS_ID = "Triggers";

    public ContractTypeBuilder(GameObject encounterLayerGo, JObject contractTypeBuild) {
      this.ContractTypeBuild = contractTypeBuild;
      this.EncounterLayerGo = encounterLayerGo;
      ContractTypeKey = contractTypeBuild["Key"].ToString();
    }

    public bool Build() {
      Main.LogDebug($"[ContractTypeBuild] Building '{ContractTypeKey}'");

      BuildTeamsData();
      BuildPlotsData();

      EncounterDataManager.Instance.HandlePlotsAndMapMetadataUpdate();

      BuildChunks();
      BuildTriggers();

      Validate();

      return true;
    }

    private void BuildTeamsData() {
      if (ContractTypeBuild.ContainsKey(TEAMS_ID)) {
        JArray teamsData = (JArray)ContractTypeBuild[TEAMS_ID];
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] There are '{teamsData.Count}' team data entries defined.");
        foreach (JObject teamData in teamsData.Children<JObject>()) {
          BuildTeamData(teamData);
        }
      }
    }

    private void BuildPlotsData() {
      if (ContractTypeBuild.ContainsKey(PLOTS_ID)) {
        JArray plotsData = (JArray)ContractTypeBuild[PLOTS_ID];
        PlotOverride plotOverride = MissionControl.Instance.EncounterLayerData.GetComponent<PlotOverride>();
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] There are '{plotsData.Count}' plot data entries defined.");

        foreach (JObject plotData in plotsData.Children<JObject>()) {
          BuildPlotData(plotOverride, plotData);
        }
      }
    }

    private void BuildChunks() {
      if (ContractTypeBuild.ContainsKey(CHUNKS_ID)) {
        JArray chunksArray = (JArray)ContractTypeBuild[CHUNKS_ID];
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] There are '{chunksArray.Count}' chunk(s) defined.");
        foreach (JObject chunk in chunksArray.Children<JObject>()) {
          BuildChunk(chunk);
        }
      }
    }

    private void BuildTriggers() {
      if (ContractTypeBuild.ContainsKey(TRIGGERS_ID)) {
        JArray triggersArray = (JArray)ContractTypeBuild[TRIGGERS_ID];
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] There are '{triggersArray.Count}' trigger(s) defined.");
        foreach (JObject trigger in triggersArray.Children<JObject>()) {
          BuildTrigger(trigger);
        }
      }
    }

    private void BuildTeamData(JObject teamData) {
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Team Data for '{teamData["Team"]}'");
      TeamDataBuilder teamDataBuilder = new TeamDataBuilder(this, teamData);
      teamDataBuilder.Build();
    }

    private void BuildPlotData(PlotOverride plotOverride, JObject plotData) {
      string plotName = plotData["Name"].ToString();
      string plotVariant = plotData.ContainsKey("Variant") ? plotData["Variant"].ToString() : "Default";
      if (plotVariant == "None") plotVariant = "Default";
      bool isActive = plotData.ContainsKey("IsActive") ? (bool)plotData["IsActive"] : true;

      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Plot Data for '{plotName}:{plotVariant}:{isActive}'");

      if (isActive) {
        plotOverride.plotOverrideEntryList.Add(new PlotOverrideEntry() {
          plotName = plotName,
          plotVariant = plotVariant,
        });
      }
    }

    private void BuildChunk(JObject chunk) {
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Chunk is '{chunk["Name"]}'");
      string name = chunk["Name"].ToString();
      string type = chunk["Type"].ToString();
      string subType = chunk["SubType"].ToString();
      string status = (chunk.ContainsKey("StartingStatus")) ? chunk["StartingStatus"].ToString() : null;
      List<string> conflictsWith = chunk.ContainsKey("ConflictsWith") ? ((JArray)chunk["ConflictsWith"]).ToObject<List<string>>() : null;
      JArray onActiveExecute = (chunk.ContainsKey("OnActiveExecute")) ? (JArray)chunk["OnActiveExecute"] : null;
      bool controlledByContract = (chunk.ContainsKey("ControlledByContract")) ? (bool)chunk["ControlledByContract"] : false;
      string guid = (chunk.ContainsKey("Guid")) ? chunk["Guid"].ToString() : null;
      JObject position = (JObject)chunk["Position"];
      JArray children = (JArray)chunk["Children"];

      EncounterObjectStatus? startingStatus = (status == null) ? null : (EncounterObjectStatus?)((EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), status));

      ChunkTypeBuilder chunkTypeBuilder = new ChunkTypeBuilder(this, name, type, subType, startingStatus, controlledByContract, guid, position, children);
      GameObject chunkGo = chunkTypeBuilder.Build();
      if (chunkGo == null) {
        Main.Logger.LogError("[ContractTypeBuild.{ContractTypeKey}] Chunk creation failed. GameObject is null");
      }

      if (conflictsWith != null) {
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] There are '{conflictsWith.Count} conflicting chunk(s) defined on chunk '{name}'");
        foreach (string chunkGuid in conflictsWith) {
          GenericCompoundConditional genericCompoundConditional = ScriptableObject.CreateInstance<GenericCompoundConditional>();
          EncounterObjectStatus statusType = EncounterObjectStatus.Active;
          EncounterObjectMatchesStateConditional conditional = ScriptableObject.CreateInstance<EncounterObjectMatchesStateConditional>();
          conditional.EncounterGuid = guid;
          conditional.State = statusType;
          List<EncounterConditionalBox> conditionalBoxList = new List<EncounterConditionalBox>() { new EncounterConditionalBox(conditional) };
          genericCompoundConditional.conditionalList = conditionalBoxList.ToArray();

          SetChunkObjectivesAsPrimary result = ScriptableObject.CreateInstance<SetChunkObjectivesAsPrimary>();
          result.EncounterGuid = chunkGuid;
          result.Primary = false;

          GenericTriggerBuilder genericTrigger = new GenericTriggerBuilder(this, "ConflictAvoidanceTrigger", (MessageCenterMessageType)MessageTypes.OnEncounterStateChanged,
            genericCompoundConditional, "Avoids a conflicting chunk's objectives by making them secondary", new List<DesignResult>() { result });
          genericTrigger.Build();
        }
      }

      if (onActiveExecute != null) {
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] There are '{onActiveExecute.Count} activators(s) defined on chunk '{name}'");
        foreach (JObject activator in onActiveExecute.Children<JObject>()) {
          BuildChunkNodeActivators(chunkGo, activator);
        }
      }

      foreach (JObject child in children.Children<JObject>()) {
        BuildNode(chunkGo, child);
      }
    }

    private void BuildChunkNodeActivators(GameObject parent, JObject activator) {
      string type = activator["Type"].ToString();
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Activator type is '{type}'");

      NodeBuilder nodeBuilder = null;

      switch (type) {
        case "Dialogue": nodeBuilder = new DialogueActivatorBuilder(this, parent, activator); break;
        case "SetChunkStateAtRandom": nodeBuilder = new SetChunkStateAtRandomActivatorBuilder(this, parent, activator); break;
        default: break;
      }

      if (nodeBuilder != null) {
        nodeBuilder.Build();
      } else {
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] No valid chunk child activator was built for '{type}' on chunk '{parent.gameObject.name}'");
      }
    }

    private void BuildNode(GameObject parent, JObject child) {
      string type = child["Type"].ToString();
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Child type is '{type}'");

      NodeBuilder nodeBuilder = null;

      switch (type) {
        case "Spawner": nodeBuilder = new SpawnBuilder(this, parent, child); break;
        case "Objective": nodeBuilder = new ObjectiveBuilder(this, parent, child); break;
        case "Region": nodeBuilder = new RegionBuilder(this, parent, child); break;
        case "Dialogue": nodeBuilder = new DialogueBuilder(this, parent, child); break;
        case "SwapPlacement": nodeBuilder = new PlacementBuilder(this, parent, child); break;
        case "ContractEdit": nodeBuilder = new ContractEditBuilder(this, parent, child); break;
        case "CombatState": nodeBuilder = new CombatStateBuilder(this, parent, child); break;
        default: break;
      }

      if (nodeBuilder != null) {
        nodeBuilder.Build();
      } else {
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] No valid node was built for '{type}' on chunk '{parent.gameObject.name}'");
      }
    }

    private void BuildTrigger(JObject trigger) {
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Trigger is '{trigger["Name"]}'");
      string name = trigger["Name"].ToString();
      string type = (trigger.ContainsKey("Type")) ? trigger["Type"].ToString() : "Generic";

      TriggerBuilder triggerBuilder = null;

      switch (type) {
        case "Generic": triggerBuilder = new GenericTriggerBuilder(this, trigger, name); break;
        default: break;
      }

      if (triggerBuilder != null) {
        triggerBuilder.Build();
      } else {
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] No valid trigger was built for '{type}'");
      }
    }

    private void Validate() {
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Validating");
      // MissionControl.Instance.EncounterLayerData.Validate();
    }
  }
}