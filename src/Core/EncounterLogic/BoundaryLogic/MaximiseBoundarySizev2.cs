using UnityEngine;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class MaximiseBoundarySizeV2 : SceneManipulationLogic {
    private float size = 0;

    public MaximiseBoundarySizeV2(EncounterRules encounterRules, float size) : base(encounterRules) {
      this.size = size;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[MaximiseBoundarySizeV2.Run] Setting Boundary Size to '{size}'");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;

      if (size > 0.75f) { // use the full map if size is greater than 75%
        MatchBoundarySizeToMapSize(encounterLayerData);
      } else if (size > 0f) {
        SetBoundarySizeToCustom(encounterLayerData, size);
      }
    }

    protected override bool GetObjectReferences() { return true; }

    private void SetBoundarySizeToCustom(EncounterLayerData encounterLayerData, float size) {
      EncounterBoundaryChunkGameLogic encounterBoundaryChunk = encounterLayerData.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
      if (encounterBoundaryChunk != null) {
        Main.Logger.Log($"[MaximiseBoundarySizeV2.SetBoundarySizeToCustom] Increasing Boundary Size by '{size * 100}%'");
        EncounterObjectGameLogic[] childEncounterObjectGameLogicList = encounterBoundaryChunk.childEncounterObjectGameLogicList;

        const float mapBorderSize = 50f;
        const float mapSize = 2048f;
        const int mapSide = (int)(mapSize - mapBorderSize);
        const float halfMapSide = mapSide / 2f;

        for (int i = 0; i < childEncounterObjectGameLogicList.Length; i++) {
          EncounterBoundaryRectGameLogic encounterBoundaryRectGameLogic = childEncounterObjectGameLogicList[i] as EncounterBoundaryRectGameLogic;

          if (encounterBoundaryRectGameLogic == null) {
            Main.Logger.Log($"[MaximiseBoundarySizeV2] Encountered null boundary rect in list, skipping.");
            continue;
          }

          int xSizeFactor = (int)(encounterBoundaryRectGameLogic.width * (1f + size));
          int zSizeFactor = (int)(encounterBoundaryRectGameLogic.height * (1f + size));

          Vector3 position = encounterBoundaryRectGameLogic.transform.position;

          Main.Logger.Log($"[MaximiseBoundarySizeV2.SetBoundarySizeToCustom] Boundary [X,Z] originally was [{position.x}, {position.z}]");

          // Implement asymmetric growth: grow from center, but clamp edges to map limits
          // This allows maximum growth while preventing overflow

          // Calculate desired edges after symmetric growth from center
          float desiredLeftEdge = position.x - (xSizeFactor / 2f);
          float desiredRightEdge = position.x + (xSizeFactor / 2f);
          float desiredBottomEdge = position.z - (zSizeFactor / 2f);
          float desiredTopEdge = position.z + (zSizeFactor / 2f);

          // Clamp edges to map boundaries
          float clampedLeftEdge = Mathf.Max(desiredLeftEdge, -halfMapSide);
          float clampedRightEdge = Mathf.Min(desiredRightEdge, halfMapSide);
          float clampedBottomEdge = Mathf.Max(desiredBottomEdge, -halfMapSide);
          float clampedTopEdge = Mathf.Min(desiredTopEdge, halfMapSide);

          // Calculate actual size and position from clamped edges
          int actualWidth = (int)(clampedRightEdge - clampedLeftEdge);
          int actualHeight = (int)(clampedTopEdge - clampedBottomEdge);
          float actualPosX = (clampedRightEdge + clampedLeftEdge) / 2f;
          float actualPosZ = (clampedTopEdge + clampedBottomEdge) / 2f;

          encounterBoundaryRectGameLogic.width = actualWidth;
          encounterBoundaryRectGameLogic.height = actualHeight;
          encounterBoundaryRectGameLogic.transform.position = new Vector3(actualPosX, encounterBoundaryRectGameLogic.transform.position.y, actualPosZ);

          Main.Logger.Log($"[MaximiseBoundarySizeV2.SetBoundarySizeToCustom] Boundary [X,Z] is now [{actualPosX}, {actualPosZ}] with size [{actualWidth}, {actualHeight}]");
        }
        encounterLayerData.CalculateEncounterBoundary();
      }
    }

    private void MatchBoundarySizeToMapSize(EncounterLayerData encounterLayerData) {
      EncounterBoundaryChunkGameLogic encounterBoundaryChunk = encounterLayerData.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
      if (encounterBoundaryChunk != null) {
        Main.Logger.Log($"[MaximiseBoundarySizeV2.SetBoundarySizeToMedium] Setting Boundary Size to Maximum Map Size");
        EncounterObjectGameLogic[] childEncounterObjectGameLogicList = encounterBoundaryChunk.childEncounterObjectGameLogicList;

        const float mapBorderSize = 50f;
        const float mapSize = 2048f;
        const int mapSide = (int)(mapSize - mapBorderSize);

        for (int i = 0; i < childEncounterObjectGameLogicList.Length; i++) {
          EncounterBoundaryRectGameLogic encounterBoundaryRectGameLogic = childEncounterObjectGameLogicList[i] as EncounterBoundaryRectGameLogic;

          if (encounterBoundaryRectGameLogic != null) {
            encounterBoundaryRectGameLogic.width = mapSide;
            encounterBoundaryRectGameLogic.height = mapSide;
            encounterBoundaryRectGameLogic.transform.position = new Vector3(0, encounterBoundaryRectGameLogic.transform.position.y, 0);
          } else {
            Main.Logger.Log($"[MaximiseBoundarySizeV2] This encounter has no boundary to maximise.");
          }
        }
        encounterLayerData.CalculateEncounterBoundary();
      }
    }
  }
}