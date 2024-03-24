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
    private JObject scale;
    private JArray props;

    public GameObject Parent { get; set; }

    public PropGroupBuilder(ContractTypeBuilder contractTypeBuilder, JObject propGroup, GameObject parent = null) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.propGroup = propGroup;

      name = propGroup["Name"].ToString();
      position = propGroup.ContainsKey("Position") ? (JObject)propGroup["Position"] : null;
      rotation = propGroup.ContainsKey("Rotation") ? (JObject)propGroup["Rotation"] : null;
      scale = propGroup.ContainsKey("Scale") ? (JObject)propGroup["Scale"] : null;
      props = propGroup.ContainsKey("Props") ? (JArray)propGroup["Props"] : null;

      Parent = parent;
    }

    public override void Build() {
      Main.Logger.Log($"[PropGroupBuilder.Build] Building '{name}' Prop Group");

      GameObject propGroupGO = new GameObject("PropGroup_" + name);
      propGroupGO.transform.parent = (Parent == null) ? PropFactory.MCPropGroupParent.transform : Parent.transform;
      propGroupGO.transform.localPosition = Vector3.zero;

      // TODO: Should position and rotation be set after the props are populated in? I think so as I need to do that for scale for it to make sense

      if (this.position != null) {
        SetPosition(propGroupGO, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(propGroupGO, this.rotation);
      }

      if (this.props != null) {
        BuildProps(props, propGroupGO);
      }

      if (this.scale != null) {
        SetScale(propGroupGO, this.scale);
      }

    }

    private void BuildProps(JArray props, GameObject propGroupParent) {
      Main.LogDebug($"[PropGroupBuilder.BuildProps] There are '{props.Count}' prop builds for group '{name}'");

      foreach (JObject prop in props.Children<JObject>()) {
        string type = prop["Type"].ToString();

        switch (type) {
          case "PropGroup": {
            PropGroupBuilder propGroupBuilder = new PropGroupBuilder(contractTypeBuilder, prop, propGroupParent);
            propGroupBuilder.Build();
            break;
          }
          case "Dropship": {
            DropshipBuilder dropshipBuilder = new DropshipBuilder(contractTypeBuilder, prop, propGroupParent);
            dropshipBuilder.Build();
            break;
          }
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