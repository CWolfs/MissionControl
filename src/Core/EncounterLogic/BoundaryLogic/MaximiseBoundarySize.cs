using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class MaximiseBoundarySize : SceneManipulationLogic {

    public MaximiseBoundarySize(EncounterRules encounterRules) : base(encounterRules) { }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[MaximiseBoundarySize] Setting Boundary Size");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      MatchBoundarySizeToMapSize(encounterLayerData);
    }

    protected override void GetObjectReferences() { }

    private void MatchBoundarySizeToMapSize(EncounterLayerData encounterLayerData) {
      EncounterBoundaryChunkGameLogic encounterBoundaryChunk = encounterLayerData.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
			if (encounterBoundaryChunk != null) {
        List<RectHolder> encounterBoundaryRectList = new List<RectHolder>();
        EncounterObjectGameLogic[] childEncounterObjectGameLogicList = encounterBoundaryChunk.childEncounterObjectGameLogicList;

        float mapBorderSize = 50f;
        float mapSize = 2048f;
        int mapSide = (int)(mapSize - mapBorderSize);

        for (int i = 0; i < childEncounterObjectGameLogicList.Length; i++) {
          EncounterBoundaryRectGameLogic encounterBoundaryRectGameLogic = childEncounterObjectGameLogicList[i] as EncounterBoundaryRectGameLogic;
          
          if (encounterBoundaryRectGameLogic != null)	{
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