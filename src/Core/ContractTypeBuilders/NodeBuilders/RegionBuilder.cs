using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;

using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class RegionBuilder : NodeBuilder {
    private const int DEFAULT_WIDTH = 800;
    private const int DEFAULT_LENGTH = 800;

    private ContractTypeBuilder contractTypeBuilder;
    private JObject objective;

    private GameObject parent;
    private string name;
    private string subType;
    private int width;
    private int length;
    private JObject position;
    private JObject rotation;
    private string regionDefId;

    public RegionBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject objective) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.objective = objective;

      this.parent = parent;
      this.name = objective["Name"].ToString();
      this.subType = objective["SubType"].ToString();
      this.width = objective.ContainsKey("Width") ? (int)objective["Width"] : DEFAULT_WIDTH;
      this.length = objective.ContainsKey("Length") ? (int)objective["Length"] : DEFAULT_LENGTH;
      this.position = objective.ContainsKey("Position") ? (JObject)objective["Position"] : null;
      this.rotation = objective.ContainsKey("Rotation") ? (JObject)objective["Rotation"] : null;
      this.regionDefId = objective.ContainsKey("RegionDefId") ? (string)objective["RegionDefId"] : "regionDef_TargetZone";
    }

    public override void Build() {
      switch (subType) {
        case "Boundary": BuildBoundary(); break;
        case "Normal": BuildNormal(); break;
        default: Main.LogDebug($"[RegionBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildBoundary() {
      EncounterBoundaryRectGameLogic boundaryLogic = BoundaryFactory.CreateEncounterBoundary(this.parent, this.name, Guid.NewGuid().ToString(), this.width, this.length);

      if (this.position != null) {
        SetPosition(boundaryLogic.gameObject, this.position);
      }
    }

    public void BuildNormal() {
      string regionGuid = objective["Guid"].ToString();
      string objectiveGuid = objective["ObjectiveGuid"].ToString();
      float radius = objective.ContainsKey("Radius") ? (float)objective["Radius"] : (float)70;

      RegionGameLogic regionLogic = RegionFactory.CreateRegion(this.parent, regionGuid, objectiveGuid, this.name, regionDefId, radius);
      GameObject regionGo = regionLogic.gameObject;

      if (position != null) SetPosition(regionGo, position);
      if (rotation != null) SetRotation(regionGo, rotation);

      regionLogic.Regenerate();
    }
  }
}