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
    private JObject region;

    private GameObject parent;
    private string name;
    private string subType;
    private int width;
    private int length;
    private JObject position;
    private JObject rotation;
    private string regionDefId;

    public RegionBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject region) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.region = region;

      this.parent = parent;
      this.name = region["Name"].ToString();
      this.subType = region["SubType"].ToString();
      this.width = region.ContainsKey("Width") ? (int)region["Width"] : DEFAULT_WIDTH;
      this.length = region.ContainsKey("Length") ? (int)region["Length"] : DEFAULT_LENGTH;
      this.position = region.ContainsKey("Position") ? (JObject)region["Position"] : null;
      this.rotation = region.ContainsKey("Rotation") ? (JObject)region["Rotation"] : null;
      this.regionDefId = region.ContainsKey("RegionDefId") ? (string)region["RegionDefId"] : "regionDef_TargetZone";
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
      string regionGuid = region["Guid"].ToString();
      string objectiveGuid = region.ContainsKey("ObjectiveGuid") ? region["ObjectiveGuid"].ToString() : null;
      float radius = region.ContainsKey("Radius") ? (float)region["Radius"] : (float)70;
      bool showPreviewOfRegion = region.ContainsKey("ShowPreviewOfRegionWhenInactive") ? (bool)region["ShowPreviewOfRegionWhenInactive"] : false;
      bool showHexWhenActive = region.ContainsKey("ShowHexWhenActive") ? (bool)region["ShowHexWhenActive"] : true;
      bool alwaysShowRegionWhenActive = region.ContainsKey("AlwaysShowRegionWhenActive") ? (bool)region["AlwaysShowRegionWhenActive"] : false;

      RegionGameLogic regionLogic = RegionFactory.CreateRegion(this.parent, regionGuid, objectiveGuid, this.name, regionDefId, radius, showHexWhenActive, alwaysShowRegionWhenActive, showPreviewOfRegion);
      GameObject regionGo = regionLogic.gameObject;

      if (position != null) SetPosition(regionGo, position);
      // if (rotation != null) SetRotation(regionGo, rotation);

      regionLogic.Regenerate();
    }
  }
}