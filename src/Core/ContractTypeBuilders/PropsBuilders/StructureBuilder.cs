using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class StructureBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject structure;

    private string structureName;
    private string structureKey;
    private JObject position;
    private JObject rotation;

    public StructureBuilder(ContractTypeBuilder contractTypeBuilder, JObject structure) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.structure = structure;

      structureName = structure["Name"].ToString();
      structureKey = structure["Key"].ToString();
      position = structure.ContainsKey("Position") ? (JObject)structure["Position"] : null;
      rotation = structure.ContainsKey("Rotation") ? (JObject)structure["Rotation"] : null;
    }

    public override void Build() {
      Main.Logger.Log($"[StructureBuilder.Build] Building '{structureKey}' Structure");
      if (!DataManager.Instance.StructureDefs.ContainsKey(structureKey)) {
        Main.Logger.LogError($"[StructureBuilder.Build] PropStructureDef '{structureKey}' does not exist");
        return;
      }

      PropStructureDef propStructureDef = DataManager.Instance.StructureDefs[structureKey];

      StructureFactory structureFactory = new StructureFactory(propStructureDef);
      GameObject structureGo = structureFactory.CreateStructure(structureKey);

      if (this.position != null) {
        SetPosition(structureGo, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(structureGo, this.rotation);
      }
    }
  }
}