using UnityEngine;

using System;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.EncounterFactories;

namespace MissionControl.Logic {
  public class AddWithdrawChunk : ChunkLogic {
    private LogicState state;
    private string debugDescription;
    private string chunkGuid;
    private string objectiveGuid;
    private string regionGameLogicGuid;

    public AddWithdrawChunk(LogicState state, string chunkGuid, string objectiveGuid, string regionGameLogicGuid = null) {
      this.state = state;
      this.objectiveGuid = objectiveGuid;
      this.chunkGuid = chunkGuid;
      this.regionGameLogicGuid = (regionGameLogicGuid == null) ? Guid.NewGuid().ToString() : regionGameLogicGuid;
      this.debugDescription = $"Your lance must reach a region on the map. You complete the objective when your entire surviving lance is in the region.By default, the starting status of this chunk is inactive. The intended use is to be the last bit of a ContractObjective. You will have to activate the EscapeChunk manually when the other objectives are complete. Also, since the player lance lives outside of this chunk, there's no way to build it into the prefab. Wire up the player lance in the OccupyRegionObjective.";
    }

    public override void Run(RunPayload payload) {
      if (!state.GetBool("Chunk_Withdraw_Exists")) {
        Main.Logger.Log($"[AddWithdrawChunk] Adding encounter structure");

        string playerSpawnerGuid = GetPlayerSpawnGuid();

        EmptyCustomChunkGameLogic emptyCustomChunk = ChunkFactory.CreateEmptyCustomChunk("Chunk_Withdraw");
        GameObject escapeChunkGo = emptyCustomChunk.gameObject;
        emptyCustomChunk.encounterObjectGuid = chunkGuid;
        emptyCustomChunk.startingStatus = EncounterObjectStatus.Inactive;
        emptyCustomChunk.notes = debugDescription;

        RegionFactory.CreateRegion(escapeChunkGo, regionGameLogicGuid, objectiveGuid, "Region_Withdraw", "regionDef_EvacZone");

        bool useDropship = true;
        OccupyRegionObjective occupyRegionObjective = ObjectiveFactory.CreateOccupyRegionObjective(
          objectiveGuid,
          escapeChunkGo,
          null,
          playerSpawnerGuid,
          regionGameLogicGuid,
          "Withdraw",
          "Get to the Evac Zone",
          $"with {ProgressFormat.UNITS_OCCUPYING_SO_FAR}/{ProgressFormat.NUMBER_OF_UNITS_TO_OCCUPY} unit(s)",
          "The objective for the player to withdraw and complete, or withdraw, the mission",
          0,
          0,
          DurationType.AfterMoveComplete,
          useDropship,
          new string[] { MissionControl.Instance.IsCustomContractType ? "Player 1" : "player_unit" },
          new string[] { "opposing_unit" }
        );

        ObjectiveFactory.CreateContractObjective(occupyRegionObjective);
      } else {
        Main.Logger.Log($"[AddWithdrawChunk] 'Chunk_Withdraw' already exists in map. No need to recreate.");
      }
    }
  }
}