using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

namespace MissionControl.Logic {
  public class SetBoundarySize : LogicBlock {

    public SetBoundarySize() {
      this.Type = LogicType.ENCOUNTER_MANIPULATION;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[SetBoundarySize] Setting Boundary Size");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      MatchBoundarySizeToMapSize(encounterLayerData);
    }

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
            encounterBoundaryRectGameLogic.transform.position = new Vector3(0, encounterBoundaryRectGameLogic.transform.position.y, 0);
          } else {
            Main.Logger.Log($"[SetBoundarySize] This encounter has no boundary to maximise.");
          }
        }
			}
    }
  }
}