using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class MaximiseBoundarySize : SceneManipulationLogic {
    private float size = 0;

    public MaximiseBoundarySize(EncounterRules encounterRules, float size) : base(encounterRules) {
      this.size = size;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[MaximiseBoundarySize.Run] Setting Boundary Size to '{size}'");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;

      if (size > 0f) {
        SetBoundarySizeToMedium(encounterLayerData, size);
      } else if (size >= 0.5f) {  // If you're going to extend by 4 times (50% width and depth) you might as well go full map
        MatchBoundarySizeToMapSize(encounterLayerData);
      }
    }

    protected override bool GetObjectReferences() { return true; }

    private void SetBoundarySizeToMedium(EncounterLayerData encounterLayerData, float size) {
      EncounterBoundaryChunkGameLogic encounterBoundaryChunk = encounterLayerData.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
      if (encounterBoundaryChunk != null) {
        Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Increasing Boundary Size by '{size * 100}%'");
        List<RectHolder> encounterBoundaryRectList = new List<RectHolder>();
        EncounterObjectGameLogic[] childEncounterObjectGameLogicList = encounterBoundaryChunk.childEncounterObjectGameLogicList;

        float mapBorderSize = 50f;
        float mapSize = 2048f;
        int mapSide = (int)(mapSize - mapBorderSize);

        for (int i = 0; i < childEncounterObjectGameLogicList.Length; i++) {
          EncounterBoundaryRectGameLogic encounterBoundaryRectGameLogic = childEncounterObjectGameLogicList[i] as EncounterBoundaryRectGameLogic;

          int mediumSize = (int)(encounterBoundaryRectGameLogic.width * (1f + size));
          int movementFactor = (int)(encounterBoundaryRectGameLogic.width * size);

          int mediumXSize = mediumSize;
          int mediumZSize = mediumSize;

          if (mediumSize > mapSide) {
            Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Medium size would be greater than map size. Using map size.'");
            MatchBoundarySizeToMapSize(encounterLayerData);
          } else {
            if (encounterBoundaryRectGameLogic != null) {
              Vector3 position = encounterBoundaryRectGameLogic.transform.position;

              Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Boundary [X,Z] is [{position.x}, {position.z}]");

              float xPosition = position.x - movementFactor;
              float zPosition = position.z + movementFactor;

              encounterBoundaryRectGameLogic.width = (int)mediumSize;
              encounterBoundaryRectGameLogic.height = (int)mediumSize;

              encounterBoundaryRectGameLogic.transform.position = new Vector3(xPosition, encounterBoundaryRectGameLogic.transform.position.y, zPosition);
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
        Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToMedium] Setting Boundary Size to Maximum Map Size");
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