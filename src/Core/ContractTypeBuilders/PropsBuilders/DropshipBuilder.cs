using UnityEngine;

using System;

using BattleTech;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DropshipBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject dropship;

    private string dropshipName;
    private string dropshipKey;
    private string guid;
    private string customName;
    private int customStructurePoints;
    private DropshipAnimationState startingDropshipState;
    private string teamGUID;
    private JObject position;
    private JObject rotation;
    private JObject scale;

    public GameObject Parent { get; set; }

    public DropshipBuilder(ContractTypeBuilder contractTypeBuilder, JObject dropship, GameObject parent) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.dropship = dropship;

      dropshipName = dropship["Name"].ToString();
      dropshipKey = dropship["Key"].ToString();
      this.guid = dropship.ContainsKey("GUID") ? dropship["GUID"].ToString() : Guid.NewGuid().ToString();
      customName = dropship.ContainsKey("CustomName") ? dropship["CustomName"].ToString() : null;
      customStructurePoints = dropship.ContainsKey("CustomStructurePoints") ? (int)dropship["CustomStructurePoints"] : 0;

      string startingDropshipStateRaw = dropship.ContainsKey("StartingState") ? dropship["StartingState"].ToString() : null;
      startingDropshipState = startingDropshipStateRaw != null ? (DropshipAnimationState)Enum.Parse(typeof(DropshipAnimationState), startingDropshipStateRaw) : DropshipAnimationState.Landed;

      string teamRaw = dropship.ContainsKey("Team") ? dropship["Team"].ToString() : null;
      teamGUID = teamRaw != null ? TeamUtils.GetTeamGuid(teamRaw) : TeamUtils.WORLD_TEAM_ID;

      position = dropship.ContainsKey("Position") ? (JObject)dropship["Position"] : null;
      rotation = dropship.ContainsKey("Rotation") ? (JObject)dropship["Rotation"] : null;
      scale = dropship.ContainsKey("Scale") ? (JObject)dropship["Scale"] : null;

      Parent = parent;
    }

    public override void Build() {
      Main.Logger.Log($"[DropshipBuilder.Build] Building '{dropshipKey}' Dropship");
      if (!DataManager.Instance.DropshipDefs.ContainsKey(dropshipKey)) {
        Main.Logger.LogError($"[DropshipBuilder.Build] PropDropshipDef '{dropshipKey}' does not exist");
        return;
      }

      PropDropshipDef propDropshipDef = DataManager.Instance.DropshipDefs[dropshipKey];

      DropshipFactory buildingFactory = new DropshipFactory(propDropshipDef, guid, customName, customStructurePoints, startingDropshipState, teamGUID);
      GameObject dropshipGO = buildingFactory.CreateDropship(Parent, dropshipKey);

      if (dropshipGO == null) {
        Main.Logger.LogError($"[DropshipBuilder.Build] Failed to create '{dropshipKey}' Dropship. Maybe loading from Asset bundle still.");
        return;
      }

      if (this.position != null) {
        SetPosition(dropshipGO, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(dropshipGO, this.rotation);
      }

      if (this.scale != null) {
        SetScale(dropshipGO, this.scale);
      }
    }
  }
}