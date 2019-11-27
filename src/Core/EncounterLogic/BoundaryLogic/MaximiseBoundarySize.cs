using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class MaximiseBoundarySize : SceneManipulationLogic {
    private string size = "UNSET";

    public MaximiseBoundarySize(EncounterRules encounterRules, string size) : base(encounterRules) {
      this.size = size;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[MaximiseBoundarySize.Run] Setting Boundary Size to '{size}'");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;

      if (size.ToLower() == "medium") {
        SetBoundarySizeToMedium(encounterLayerData);
      } else if (size.ToLower() == "large") {
        MatchBoundarySizeToMapSize(encounterLayerData);
      }
    }

    protected override void GetObjectReferences() { }

    private void SetBoundarySizeToMedium(EncounterLayerData encounterLayerData) {
      EncounterBoundaryChunkGameLogic encounterBoundaryChunk = encounterLayerData.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
      if (encounterBoundaryChunk != null) {
        Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Setting Boundary Size to 'Medium'");
        List<RectHolder> encounterBoundaryRectList = new List<RectHolder>();
        EncounterObjectGameLogic[] childEncounterObjectGameLogicList = encounterBoundaryChunk.childEncounterObjectGameLogicList;

        float mapBorderSize = 50f;
        float mapSize = 2048f;
        int mapSide = (int)(mapSize - mapBorderSize);

        for (int i = 0; i < childEncounterObjectGameLogicList.Length; i++) {
          EncounterBoundaryRectGameLogic encounterBoundaryRectGameLogic = childEncounterObjectGameLogicList[i] as EncounterBoundaryRectGameLogic;

          int mediumSize = (int)(encounterBoundaryRectGameLogic.width * 1.25f);
          int movementFactor = (int)(encounterBoundaryRectGameLogic.width * 0.25f);

          if (mediumSize > mapSide) {
            Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Medium size would be greater than map size. Using map size.'");
            MatchBoundarySizeToMapSize(encounterLayerData);
          } else {
            if (encounterBoundaryRectGameLogic != null) {
              encounterBoundaryRectGameLogic.width = (int)mediumSize;
              encounterBoundaryRectGameLogic.height = (int)mediumSize;

              Vector3 position = encounterBoundaryRectGameLogic.transform.position;
              float xPositon = position.x + movementFactor;
              float zPosition = position.z - movementFactor;

              if (xPositon > -25) {
                Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] X width is to the map boundary. Stopping at the boundary.'");
                xPositon = -25;
              }

              if (zPosition < 25) {
                Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Z width is to the map boundary. Stopping at the boundary.'");
                zPosition = 25;
              }

              encounterBoundaryRectGameLogic.transform.position = new Vector3(xPositon, encounterBoundaryRectGameLogic.transform.position.y, zPosition);
              encounterLayerData.CalculateEncounterBoundary();
            } else {
              Main.Logger.Log($"[MaximiseBoundarySize] This encounter has no boundary to maximise.");
            }
          }
        }
      }
    }

    private void MatchBoundarySizeToMapSize(EncounterLayerData encounterLayerData) {
      EncounterBoundaryChunkGameLogic encounterBoundaryChunk = encounterLayerData.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
      if (encounterBoundaryChunk != null) {
        Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Setting Boundary Size to 'Large'");
        List<RectHolder> encounterBoundaryRectList = new List<RectHolder>();
        EncounterObjectGameLogic[] childEncounterObjectGameLogicList = encounterBoundaryChunk.childEncounterObjectGameLogicList;

        float mapBorderSize = 50f;
        float mapSize = 2048f;
        int mapSide = (int)(mapSize - mapBorderSize);

        for (int i = 0; i < childEncounterObjectGameLogicList.Length; i++) {
          EncounterBoundaryRectGameLogic encounterBoundaryRectGameLogic = childEncounterObjectGameLogicList[i] as EncounterBoundaryRectGameLogic;

          if (encounterBoundaryRectGameLogic != null) {
            encounterBoundaryRectGameLogic.width = (int)mapSide;
            encounterBoundaryRectGameLogic.height = (int)mapSide;
            encounterBoundaryRectGameLogic.transform.position = new Vector3(-25, encounterBoundaryRectGameLogic.transform.position.y, 25);
            encounterLayerData.CalculateEncounterBoundary();
          } else {
            Main.Logger.Log($"[MaximiseBoundarySize] This encounter has no boundary to maximise.");
          }
        }
      }
    }
  }
}