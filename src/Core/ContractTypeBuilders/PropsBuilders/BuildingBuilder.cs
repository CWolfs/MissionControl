using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace MissionControl.ContractTypeBuilders {
  public class BuildingBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject building;

    private string buildingName;
    private string buildingKey;
    private string guid;
    private List<string> tags;
    private string customName;
    private int customStructurePoints;
    private string teamGUID;
    private JObject position;
    private JObject rotation;
    private JObject scale;

    public GameObject Parent { get; set; }

    public BuildingBuilder(ContractTypeBuilder contractTypeBuilder, JObject building, GameObject parent) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.building = building;
      this.guid = building.ContainsKey("GUID") ? building["GUID"].ToString() : Guid.NewGuid().ToString();
      buildingName = building["Name"].ToString();
      buildingKey = building["Key"].ToString();
      customName = building.ContainsKey("CustomName") ? building["CustomName"].ToString() : null;
      customStructurePoints = building.ContainsKey("CustomStructurePoints") ? (int)building["CustomStructurePoints"] : 0;
      position = building.ContainsKey("Position") ? (JObject)building["Position"] : null;
      rotation = building.ContainsKey("Rotation") ? (JObject)building["Rotation"] : null;
      scale = building.ContainsKey("Scale") ? (JObject)building["Scale"] : null;
      tags = building.ContainsKey("Tags") ? building["Tags"].ToObject<List<string>>() : new List<string>();

      string teamRaw = building.ContainsKey("Team") ? building["Team"].ToString() : null;
      teamGUID = teamRaw != null ? TeamUtils.GetTeamGuid(teamRaw) : TeamUtils.WORLD_TEAM_ID;

      Parent = parent;
    }

    public override void Build() {
      Main.Logger.Log($"[BuildingBuilder.Build] Building '{buildingKey}' Building");
      if (!DataManager.Instance.BuildingDefs.ContainsKey(buildingKey)) {
        Main.Logger.LogError($"[BuildingBuilder.Build] PropBuildingDef '{buildingKey}' does not exist");
        return;
      }

      PropBuildingDef propBuildingDef = DataManager.Instance.BuildingDefs[buildingKey];

      BuildingFactory buildingFactory = new BuildingFactory(propBuildingDef, guid, tags, customName, customStructurePoints);
      GameObject facilityGo = buildingFactory.CreateFacility(buildingKey, Parent, teamGUID);

      DestructibleObject destructibleObject = facilityGo.GetComponentInChildren<DestructibleObject>();
      GameObject destructionParentGO = destructibleObject.destructionParent.gameObject;

      if (this.position != null) {
        SetPosition(facilityGo, this.position, exactPosition: true);

        JObject destructionPosition = new JObject();
        destructionPosition.Add("Type", "World");

        JObject positionValue = new JObject();
        positionValue.Add("x", facilityGo.transform.position.x);
        positionValue.Add("y", facilityGo.transform.position.y);
        positionValue.Add("z", facilityGo.transform.position.z);

        destructionPosition.Add("Value", positionValue);

        SetPosition(destructionParentGO, destructionPosition, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(facilityGo, this.rotation);
        SetRotation(destructionParentGO, this.rotation);
      }

      if (this.scale != null) {
        SetScale(facilityGo, this.scale);
        SetScale(destructionParentGO, this.scale);
      }

      buildingFactory.AddToCameraFadeGroup();
    }
  }
}