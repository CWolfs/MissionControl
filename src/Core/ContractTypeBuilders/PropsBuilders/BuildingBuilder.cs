using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class BuildingBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject building;

    private string buildingName;
    private JObject position;
    private JObject rotation;

    public BuildingBuilder(ContractTypeBuilder contractTypeBuilder, JObject building) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.building = building;

      buildingName = building["Name"].ToString();
      position = building.ContainsKey("Position") ? (JObject)building["Position"] : null;
      rotation = building.ContainsKey("Rotation") ? (JObject)building["Rotation"] : null;
    }

    public override void Build() {
      if (!DataManager.Instance.BuildingDefs.ContainsKey(buildingName)) {
        Main.Logger.LogError($"[BuildingBuilder.Build] No PropBuildingDef exists with the name '{buildingName}'");
      }

      Main.Logger.Log($"[BuildingBuilder.Build] Building '{buildingName}' Building");
      if (!DataManager.Instance.BuildingDefs.ContainsKey(buildingName)) {
        Main.Logger.LogError($"[BuildingBuilder.Build] PropBuildingDef '{buildingName}' does not exist");
        return;
      }

      PropBuildingDef propBuildingDef = DataManager.Instance.BuildingDefs[buildingName];

      BuildingFactory buildingFactory = new BuildingFactory(propBuildingDef);
      GameObject facilityGo = buildingFactory.CreateFacility(buildingName);

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