using UnityEngine;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

using MissionControl.EncounterFactories;

using BattleTech;

namespace MissionControl.ContractTypeBuilders {
  public class NavigationBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject build;

    private GameObject parent;
    private string name;
    private string subType;

    public NavigationBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject build) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.build = build;

      this.parent = parent;
      this.name = build["Name"].ToString();
      this.subType = build["SubType"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "Route": BuildRoute(); break;
        default: Main.LogDebug($"[NavigationBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildRoute() {
      string guid = build["Guid"].ToString();
      JObject position = build.ContainsKey("Position") ? (JObject)build["Position"] : null;
      JObject rotation = build.ContainsKey("Rotation") ? (JObject)build["Rotation"] : null;
      string transitType = build["TransitType"].ToString();
      List<string> routePointGUIDs = build["RoutePointGuids"].ToObject<List<string>>();
      JObject routePointPositions = build.ContainsKey("RoutePointPositions") ? (JObject)build["RoutePointPositions"] : null;

      RouteGameLogic routeGameLogic = NavigationFactory.CreateRoute(this.parent, this.name, guid);

      // Create route points
      for (int i = 0; i < routePointGUIDs.Count; i++) {
        string routePointGUID = routePointGUIDs[i];
        JObject routePointPosition = routePointPositions.ContainsKey(routePointGUID) ? (JObject)routePointPositions[routePointGUID] : null;
        RoutePointGameLogic routePointGameLogic = NavigationFactory.CreateRoutePoint(routeGameLogic.gameObject, $"RoutePoint{i + 1}", routePointGUID);
        if (routePointPosition != null) SetPosition(routePointGameLogic.gameObject, routePointPosition);
      }

      if (position != null) {
        SetPosition(routeGameLogic.gameObject, position);

        // Due to the route points being snapped to the terrain itself from the 'UpdateHexPosition' then we will anchor the route at 0
        Vector3 p = routeGameLogic.gameObject.transform.position;
        routeGameLogic.gameObject.transform.position = new Vector3(p.x, 0f, p.z);
      }

      if (rotation != null) SetRotation(routeGameLogic.gameObject, rotation);

      // Update point hex position
      foreach (RoutePointGameLogic routePointGameLogic in routeGameLogic.routePointList) {
        routePointGameLogic.UpdateHexPosition();
      }
    }
  }
}