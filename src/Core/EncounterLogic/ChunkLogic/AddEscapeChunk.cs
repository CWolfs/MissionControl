using UnityEngine;

using System;

using BattleTech;
using BattleTech.Designed;

using MissionControl.EncounterFactories;

namespace MissionControl.Logic {
  public class AddEscapeChunk : ChunkLogic {
    private LogicState state;
    private string debugDescription;
    private string chunkGuid;
    private string objectiveGuid;

    public AddEscapeChunk(LogicState state, string chunkGuid, string objectiveGuid) {
      this.state = state;
      this.objectiveGuid = objectiveGuid;
      this.chunkGuid = chunkGuid;
      this.debugDescription = $"Your lance must reach a region on the map. You complete the objective when your entire surviving lance is in the region.By default, the starting status of this chunk is inactive. The intended use is to be the last bit of a ContractObjective. You will have to activate the EscapeChunk manually when the other objectives are complete. Also, since the player lance lives outside of this chunk, there's no way to build it into the prefab. Wire up the player lance in the OccupyRegionObjective.";
    }

    public override void Run(RunPayload payload) {
      if (!state.GetBool("Chunk_Escape_Exists")) {
        Main.Logger.Log($"[AddEscapeChunk] Adding encounter structure");

        string playerSpawnerGuid = GetPlayerSpawnGuid();
        string regionGameLogicGuid = Guid.NewGuid().ToString();

        EmptyCustomChunkGameLogic emptyCustomChunk = ChunkFactory.CreateEmptyCustomChunk("Chunk_Escape");
        GameObject escapeChunkGo = emptyCustomChunk.gameObject;
        emptyCustomChunk.encounterObjectGuid = chunkGuid;
        emptyCustomChunk.startingStatus = EncounterObjectStatus.Inactive;
        emptyCustomChunk.notes = debugDescription;

        EscapeRegionFactory.CreateEscapeRegion(escapeChunkGo, regionGameLogicGuid, objectiveGuid);

        bool useDropship = true;
        ObjectiveFactory.CreateOccupyRegionObjective(
          objectiveGuid,
          escapeChunkGo,
          playerSpawnerGuid,
          regionGameLogicGuid,
          "Escape",
          "Get to the Evac Zone",
          $"with {ProgressFormat.UNITS_OCCUPYING_SO_FAR}/{ProgressFormat.NUMBER_OF_UNITS_TO_OCCUPY} unit(s)",
          "The objective for the player to escape and complete, or withdraw, the mission",
          useDropship
        );
      } else {
        Main.Logger.Log($"[AddEscapeChunk] 'Escape_Chunk' already exists in map. No need to recreate.");
      }
    }
  }
}