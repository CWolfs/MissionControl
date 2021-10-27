using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Data;
using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddLanceToTargetTeam : LanceLogic {
    private string lanceGuid;
    private List<string> unitGuids;
    private MLanceOverride manuallySpecifiedLance;

    public AddLanceToTargetTeam(string lanceGuid, List<string> unitGuids, MLanceOverride manuallySpecifiedLance = null) {
      this.lanceGuid = lanceGuid;
      this.unitGuids = unitGuids;
      this.manuallySpecifiedLance = manuallySpecifiedLance;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddLanceToTargetTeam] Adding lance to target lance");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      TeamOverride teamOverride = contractOverride.targetTeam;

      LanceOverride lanceOverride = (manuallySpecifiedLance == null) ? SelectAppropriateLanceOverride("enemy").Copy() : manuallySpecifiedLance.Copy();
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