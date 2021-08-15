using System.Collections.Generic;

using BattleTech.Framework;

using MissionControl.Data;

namespace MissionControl.Logic {
  public class AddLanceToAllyTeam : LanceLogic {
    private string lanceGuid;
    private List<string> unitGuids;
    private MLanceOverride manuallySpecifiedLance;

    public AddLanceToAllyTeam(string lanceGuid, List<string> unitGuids, MLanceOverride manuallySpecifiedLance = null) {
      this.lanceGuid = lanceGuid;
      this.unitGuids = unitGuids;
      this.manuallySpecifiedLance = manuallySpecifiedLance;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddLanceToAllyTeam] Adding lance to ally lance");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      TeamOverride teamOverride = contractOverride.employerTeam;
      TeamOverride targetTeamOverride = contractOverride.targetTeam;

      LanceOverride lanceOverride = (manuallySpecifiedLance == null) ? SelectAppropriateLanceOverride("allies").Copy() : manuallySpecifiedLance;
      lanceOverride.name = $"Lance_Ally_Force_{lanceGuid}";

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