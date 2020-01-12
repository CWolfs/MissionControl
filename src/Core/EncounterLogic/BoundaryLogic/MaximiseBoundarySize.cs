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
        SetBoundarySizeToCustom(encounterLayerData, size);
      } else if (size > 0.75f) {  // If you're going to extend by a possible 6 times (75% extra width and depth depending on existing boundary) you might as well go full map
        MatchBoundarySizeToMapSize(encounterLayerData);
      }
    }

    protected override bool GetObjectReferences() { return true; }

    private void SetBoundarySizeToCustom(EncounterLayerData encounterLayerData, float size) {
      EncounterBoundaryChunkGameLogic encounterBoundaryChunk = encounterLayerData.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
      if (encounterBoundaryChunk != null) {
        Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToCustom] Increasing Boundary Size by '{size * 100}%'");
        List<RectHolder> encounterBoundaryRectList = new List<RectHolder>();
        EncounterObjectGameLogic[] childEncounterObjectGameLogicList = encounterBoundaryChunk.childEncounterObjectGameLogicList;

        float mapBorderSize = 50f;
        float mapSize = 2048f;
        int mapSide = (int)(mapSize - mapBorderSize);

        for (int i = 0; i < childEncounterObjectGameLogicList.Length; i++) {
          EncounterBoundaryRectGameLogic encounterBoundaryRectGameLogic = childEncounterObjectGameLogicList[i] as EncounterBoundaryRectGameLogic;

          int xSizeFactor = (int)(encounterBoundaryRectGameLogic.width * (1f + size));
          int zSizeFactor = (int)(encounterBoundaryRectGameLogic.height * (1f + size));
          int xMovementFactor = (int)(encounterBoundaryRectGameLogic.width * size);
          int zMovementFactor = (int)(encounterBoundaryRectGameLogic.height * size);

          if (xSizeFactor > mapSide) {
            Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToCustom] Custom size would be greater than map size. Using map size.'");
            MatchBoundarySizeToMapSize(encounterLayerData);
          } else {
            if (encounterBoundaryRectGameLogic != null) {
              Vector3 position = encounterBoundaryRectGameLogic.transform.position;

              Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToCustom] Boundary [X,Z] originally was [{position.x}, {position.z}]");

              float xPosition = 0;
              if (position.x > 0) {
                xPosition = position.x - (xMovementFactor / 2f);
                if (xPosition < 0) xPosition = 0;
              } else if (position.x < 0) {
                xPosition = position.x + (xMovementFactor / 2f);
                if (xPosition > 0) xPosition = 0;
              }

              float zPosition = 0;
              if (position.z > 0) {
                zPosition = position.z - (zMovementFactor / 2f);
                if (zPosition < 0) zPosition = 0;
              } else if (position.z < 0) {
                zPosition = position.z + (zMovementFactor / 2f);
                if (zPosition > 0) zPosition = 0;
              }

              encounterBoundaryRectGameLogic.width = (int)xSizeFactor;
              encounterBoundaryRectGameLogic.height = (int)zSizeFactor;

              encounterBoundaryRectGameLogic.transform.position = new Vector3(xPosition, encounterBoundaryRectGameLogic.transform.position.y, zPosition);

              Main.Logger.Log($"[MaximiseBoundarySize.SetBoundarySizeToCustom] Boundary [X,Z] is now [{xPosition}, {zPosition}]");

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