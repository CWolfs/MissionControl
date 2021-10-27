using System.Collections.Generic;

using BattleTech.Framework;

namespace MissionControl.Logic {
  public class AddLanceToPlayer2Team : LanceLogic {
    private string lanceGuid;
    private List<string> unitGuids;

    public AddLanceToPlayer2Team(string lanceGuid, List<string> unitGuids) {
      this.lanceGuid = lanceGuid;
      this.unitGuids = unitGuids;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddLanceToPlayer2TeamTeam] Adding lance to target lance");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      TeamOverride teamOverride = contractOverride.player2Team;

      LanceOverride lanceOverride = SelectAppropriateLanceOverride("enemy").Copy();
      lanceOverride.name = $"Lance_Enemy_OpposingForce_{lanceGuid}";

      if (unitGuids.Count > 4) {
        for (int i = 4; i < unitGuids.Count; i++) {
          UnitSpawnPointOverride unitSpawnOverride = lanceOverride.unitSpawnPointOverrideList[0].Copy();
          lanceOverride.unitSpawnPointOverrideList.Add(unitSpawnOverride);
        }
      }

      for (int i = 0; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
        string unitGuid = unitGuids[i];
        UnitSpawnPointRef unitSpawnRef = new UnitSpawnPointRef();
        unitSpawnRef.EncounterObjectGuid = unitGuid;
        lanceOverride.unitSpawnPointOverrideList[i].unitSpawnPoint = unitSpawnRef;
      }

      LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
      lanceSpawnerRef.EncounterObjectGuid = lanceGuid;
      lanceOverride.lanceSpawner = lanceSpawnerRef;

      lanceOverride.RunMadLibs(contractOverride.contract, teamOverride);

      teamOverride.lanceOverrideList.Add(lanceOverride);
    }
  }
}