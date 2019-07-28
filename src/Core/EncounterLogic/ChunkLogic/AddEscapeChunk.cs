using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.EncounterFactories;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddEscapeChunk : ChunkLogic {
    private LogicState state;
    private string debugDescription;

    public AddEscapeChunk(LogicState state) {
      this.state = state;
      this.debugDescription = $"Your lance must reach a region on the map. You complete the objective when your entire surviving lance is in the region.By default, the starting status of this chunk is inactive. The intended use is to be the last bit of a ContractObjective. You will have to activate the EscapeChunk manually when the other objectives are complete. Also, since the player lance lives outside of this chunk, there's no way to build it into the prefab. Wire up the player lance in the OccupyRegionObjective.";
    }

    public override void Run(RunPayload payload) {
      if (!state.GetBool("Chunk_Escape_Exists")) {
        Main.Logger.Log($"[AddEscapeChunk] Adding encounter structure");
        EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
        EmptyCustomChunkGameLogic emptyCustomChunk = ChunkFactory.CreateEmptyCustomChunk("Chunk_Escape");
        emptyCustomChunk.encounterObjectGuid = System.Guid.NewGuid().ToString();
        emptyCustomChunk.startingStatus = EncounterObjectStatus.Inactive;
        emptyCustomChunk.notes = debugDescription;

        EscapeRegionFactory.CreateEscapeRegion(emptyCustomChunk.gameObject);
      } else {
        Main.Logger.Log($"[AddEscapeChunk] 'Escape_Chunk' already exists in map. No need to recreate.");
      }
    }
  }
}