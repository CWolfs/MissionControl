using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DestructibleBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject destructibleGroup;

    private string destructibleGroupName;
    private JObject position;
    private JObject rotation;
    private JArray props;

    public GameObject Parent { get; set; }

    public DestructibleBuilder(ContractTypeBuilder contractTypeBuilder, JObject destructibleGroup, GameObject parent) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.destructibleGroup = destructibleGroup;

      destructibleGroupName = destructibleGroup["Name"].ToString();
      position = destructibleGroup.ContainsKey("Position") ? (JObject)destructibleGroup["Position"] : null;
      rotation = destructibleGroup.ContainsKey("Rotation") ? (JObject)destructibleGroup["Rotation"] : null;
      props = destructibleGroup.ContainsKey("Props") ? (JArray)destructibleGroup["Props"] : null;

      Parent = parent;
    }

    public override void Build() {
      // Build the group parent
      DestructibleFactory destructibleGroupFactory = new DestructibleFactory();
      GameObject destructibleGroupGO = destructibleGroupFactory.CreateDestructibleFlimsyGroup(destructibleGroupName, Parent);

      // Build all the flimsy destructibles
      if (props != null) {
        foreach (JObject prop in props.Children<JObject>()) {
          string type = prop["Type"].ToString();

          switch (type) {
            case "Building": {
              string buildingName = prop["Name"].ToString();
              string buildingKey = prop["Key"].ToString();
              JObject position = prop.ContainsKey("Position") ? (JObject)prop["Position"] : null;
              JObject rotation = prop.ContainsKey("Rotation") ? (JObject)prop["Rotation"] : null;

              if (!DataManager.Instance.BuildingDefs.ContainsKey(buildingKey)) {
                Main.Logger.LogError($"[DestructibleBuilder.Build] No building exists with key '{buildingKey}'. Check a PropBuildingDef exists with that key in the 'props/buildings' folder");
              }

              PropBuildingDef propBuildingDef = DataManager.Instance.BuildingDefs[buildingKey];
              BuildingFactory buildingFactory = new BuildingFactory(propBuildingDef);

              GameObject destructibleGO = buildingFactory.CreateBuilding(destructibleGroupGO, $"Building_{propBuildingDef.Key}", DestructibleObject.DestructType.flimsyStruct);

              if (position != null) {
                SetPosition(destructibleGO, position, exactPosition: true);
              }

              if (rotation != null) {
                SetRotation(destructibleGO, rotation);
              }
              break;
            }
            case "Destructible": {
              string destructibleName = prop["Name"].ToString();
              string destructibleKey = prop["Key"].ToString();
              JObject position = prop.ContainsKey("Position") ? (JObject)prop["Position"] : null;
              JObject rotation = prop.ContainsKey("Rotation") ? (JObject)prop["Rotation"] : null;

              if (!DataManager.Instance.DestructibleDefs.ContainsKey(destructibleKey)) {
                Main.Logger.LogError($"[DestructibleBuilder.Build] No destructible exists with key '{destructibleKey}'. Check a PropDestructionDef exists with that key in the 'props/destructible' folder");
              }

              PropDestructibleFlimsyDef propDestructibleDef = DataManager.Instance.DestructibleDefs[destructibleKey];
              DestructibleFactory destructibleFactory = new DestructibleFactory(propDestructibleDef);

              GameObject destructibleGO = destructibleFactory.CreateDestructible(destructibleGroupGO, propDestructibleDef.Key);

              if (position != null) {
                SetPosition(destructibleGO, position, exactPosition: true);
              }

              if (rotation != null) {
                SetRotation(destructibleGO, rotation);
              }
              break;
            }
          }
        }
      }

      DestructibleFlimsyGroup destructibleFlimsyGroup = destructibleGroupGO.GetComponent<DestructibleFlimsyGroup>();
      if (destructibleFlimsyGroup != null) destructibleFlimsyGroup.BakeDestructionAssets();

      if (this.position != null) {
        SetPosition(destructibleGroupGO, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(destructibleGroupGO, this.rotation);
      }
    }
  }
}