using UnityEngine;

using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class PropGroupBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject propGroup;

    private string name;
    private JObject position;
    private JObject rotation;
    private JArray props;

    public PropGroupBuilder(ContractTypeBuilder contractTypeBuilder, JObject propGroup) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.propGroup = propGroup;

      name = propGroup["Name"].ToString();
      position = propGroup.ContainsKey("Position") ? (JObject)propGroup["Position"] : null;
      rotation = propGroup.ContainsKey("Rotation") ? (JObject)propGroup["Rotation"] : null;
      props = propGroup.ContainsKey("Props") ? (JArray)propGroup["Props"] : null;
    }

    public override void Build() {
      Main.Logger.Log($"[PropGroupBuilder.Build] Building '{name}' Prop Group");

      GameObject propGroupGO = new GameObject("PropGroup_" + name);
      propGroupGO.transform.parent = PropFactory.MCPropGroupParent.transform;
      propGroupGO.transform.localPosition = Vector3.zero;

      if (this.position != null) {
        SetPosition(propGroupGO, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(propGroupGO, this.rotation);
      }

      if (this.props != null) {
        BuildProps(props, propGroupGO);
      }
    }

    private void BuildProps(JArray props, GameObject propGroupParent) {
      Main.LogDebug($"[PropGroupBuilder.BuildProps] There are '{props.Count}' prop builds for group '{name}'");

      foreach (JObject prop in props.Children<JObject>()) {
        string type = prop["Type"].ToString();

        switch (type) {
          case "Building": {
            BuildingBuilder buildingBuilder = new BuildingBuilder(contractTypeBuilder, prop, propGroupParent);
            buildingBuilder.Build();
            break;
          }
          case "Structure": {
            StructureBuilder buildingBuilder = new StructureBuilder(contractTypeBuilder, prop, propGroupParent);
            buildingBuilder.Build();
            break;
          }
          case "DestructibleGroup": {
            DestructibleBuilder destructibleBuilder = new DestructibleBuilder(contractTypeBuilder, prop, propGroupParent);
            destructibleBuilder.Build();
            break;
          }
        }
      }
    }
  }
}