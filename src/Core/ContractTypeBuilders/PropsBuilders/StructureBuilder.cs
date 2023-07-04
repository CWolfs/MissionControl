using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class StructureBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject structure;

    private string structureName;
    private JObject position;
    private JObject rotation;

    public StructureBuilder(ContractTypeBuilder contractTypeBuilder, JObject structure) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.structure = structure;

      structureName = structure["Name"].ToString();
      position = structure.ContainsKey("Position") ? (JObject)structure["Position"] : null;
      rotation = structure.ContainsKey("Rotation") ? (JObject)structure["Rotation"] : null;
    }

    public override void Build() {
      if (!DataManager.Instance.StructureDefs.ContainsKey(structureName)) {
        Main.Logger.LogError($"[StructureBuilder.Build] No PropStructureDef exists with the name '{structureName}'");
      }

      Main.Logger.Log($"[StructureBuilder.Build] Building '{structureName}' Building");
      if (!DataManager.Instance.StructureDefs.ContainsKey(structureName)) {
        Main.Logger.LogError($"[StructureBuilder.Build] PropStructureDef '{structureName}' does not exist");
        return;
      }

      PropStructureDef propStructureDef = DataManager.Instance.StructureDefs[structureName];

      StructureFactory structureFactory = new StructureFactory(propStructureDef);
      GameObject structureGo = structureFactory.CreateStructure(structureName);

      if (this.position != null) {
        SetPosition(structureGo, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(structureGo, this.rotation);
      }

      // structureFactory.AddToCameraFadeGroup();
    }
  }
}