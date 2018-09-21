using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using SpawnVariation.Rules;
using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class AddLanceToPlayerTeam : LanceLogic {
    private string lanceGuid;
    private List<string> unitGuids;

    public AddLanceToPlayerTeam(string lanceGuid, List<string> unitGuids) {
      this.lanceGuid = lanceGuid;
      this.unitGuids = unitGuids;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddLanceToPlayerTeam] Adding lance to target lance");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      TeamOverride teamOverride = contractOverride.player1Team;
      TeamOverride targetTeamOverride = contractOverride.targetTeam;

      List<LanceOverride> lanceOverrideList = targetTeamOverride.lanceOverrideList;
      if (lanceOverrideList.Count > 0) {
        LanceOverride lanceOverride = lanceOverrideList[0].Copy();

        lanceOverride.name = "Lance_Player_Reinforcements";

        for (int i = 0; i < unitGuids.Count; i++) {
          string unitGuid = unitGuids[i];
          UnitSpawnPointRef unitSpawnRef = new UnitSpawnPointRef();
          unitSpawnRef.EncounterObjectGuid = unitGuid;
          lanceOverride.unitSpawnPointOverrideList[i].unitSpawnPoint = unitSpawnRef;
        }
        
        LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
        lanceSpawnerRef.EncounterObjectGuid = lanceGuid;
        lanceOverride.lanceSpawner = lanceSpawnerRef;

        teamOverride.lanceOverrideList.Add(lanceOverride);
      } else {
        Main.Logger.LogError("[AddLanceToPlayerTeam] Team Override has no lances available to copy. TODO: Generate new lance from stored JSON data");
      }
    }
  }
}