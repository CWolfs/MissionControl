using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class BuildingBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject building;

    private string buildingName;
    private string buildingKey;
    private string customName;
    private int customStructurePoints;
    private JObject position;
    private JObject rotation;

    public GameObject Parent { get; set; }

    public BuildingBuilder(ContractTypeBuilder contractTypeBuilder, JObject building, GameObject parent) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.building = building;

      buildingName = building["Name"].ToString();
      buildingKey = building["Key"].ToString();
      customName = building.ContainsKey("CustomName") ? building["CustomName"].ToString() : null;
      customStructurePoints = building.ContainsKey("CustomStructurePoints") ? (int)building["CustomStructurePoints"] : 0;
      position = building.ContainsKey("Position") ? (JObject)building["Position"] : null;
      rotation = building.ContainsKey("Rotation") ? (JObject)building["Rotation"] : null;

      Parent = parent;
    }

    public override void Build() {
      Main.Logger.Log($"[BuildingBuilder.Build] Building '{buildingKey}' Building");
      if (!DataManager.Instance.BuildingDefs.ContainsKey(buildingKey)) {
        Main.Logger.LogError($"[BuildingBuilder.Build] PropBuildingDef '{buildingKey}' does not exist");
        return;
      }

      PropBuildingDef propBuildingDef = DataManager.Instance.BuildingDefs[buildingKey];

      BuildingFactory buildingFactory = new BuildingFactory(propBuildingDef, customName, customStructurePoints);
      GameObject facilityGo = buildingFactory.CreateFacility(buildingKey, Parent);

      DestructibleObject destructibleObject = facilityGo.GetComponentInChildren<DestructibleObject>();
      GameObject destructionParentGO = destructibleObject.destructionParent.gameObject;

      if (this.position != null) {
        SetPosition(facilityGo, this.position, exactPosition: true);
        SetPosition(destructionParentGO, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(facilityGo, this.rotation);
        SetRotation(destructionParentGO, this.rotation);
      }

      buildingFactory.AddToCameraFadeGroup();
    }
  }
}