using UnityEngine;

using System;

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

    public RegionBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject objective) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.objective = objective;

      this.parent = parent;
      this.name = objective["Name"].ToString();
      this.subType = objective["SubType"].ToString();
      this.width = objective.ContainsKey("Width") ? (int)objective["Width"] : DEFAULT_WIDTH;
      this.length = objective.ContainsKey("Length") ? (int)objective["Length"] : DEFAULT_LENGTH;
      this.position = objective.ContainsKey("Position") ? (JObject)objective["Position"] : null;
    }

    public override void Build() {
      switch (subType) {
        case "Boundary": BuildBoundary(); break;
        default: Main.LogDebug($"[RegionBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildBoundary() {
      EncounterBoundaryRectGameLogic boundaryLogic = BoundaryFactory.CreateEncounterBoundary(this.parent, this.name, Guid.NewGuid().ToString(), this.width, this.length);

      if (this.position != null) {
        SetPosition(boundaryLogic.gameObject, this.position);
      }
    }
  }
}