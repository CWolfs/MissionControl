using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

using System;

namespace MissionControl.ContractTypeBuilders {
  public class StructureBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject structure;
    private string guid;
    private string structureName;
    private string structureKey;
    private string teamGUID;
    private JObject position;
    private JObject rotation;
    private JObject scale;

    public GameObject Parent { get; set; }

    public StructureBuilder(ContractTypeBuilder contractTypeBuilder, JObject structure, GameObject parent) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.structure = structure;

      structureName = structure["Name"].ToString();
      structureKey = structure["Key"].ToString();
      guid = structure.ContainsKey("GUID") ? structure["GUID"].ToString() : Guid.NewGuid().ToString();
      position = structure.ContainsKey("Position") ? (JObject)structure["Position"] : null;
      rotation = structure.ContainsKey("Rotation") ? (JObject)structure["Rotation"] : null;
      scale = structure.ContainsKey("Scale") ? (JObject)structure["Scale"] : null;

      string teamRaw = structure.ContainsKey("Team") ? structure["Team"].ToString() : null;
      teamGUID = teamRaw != null ? TeamUtils.GetTeamGuid(teamRaw) : TeamUtils.WORLD_TEAM_ID;

      Parent = parent;
    }

    public override void Build() {
      Main.Logger.Log($"[StructureBuilder.Build] Building '{structureKey}' Structure");
      if (!DataManager.Instance.StructureDefs.ContainsKey(structureKey)) {
        Main.Logger.LogError($"[StructureBuilder.Build] PropStructureDef '{structureKey}' does not exist");
        return;
      }

      PropStructureDef propStructureDef = DataManager.Instance.StructureDefs[structureKey];

      StructureFactory structureFactory = new StructureFactory(propStructureDef);
      GameObject structureGo = structureFactory.CreateStructure(structureKey, Parent, guid, teamGUID);

      if (this.position != null) {
        SetPosition(structureGo, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(structureGo, this.rotation);
      }

      if (this.scale != null) {
        SetScale(structureGo, this.scale);
      }
    }
  }
}