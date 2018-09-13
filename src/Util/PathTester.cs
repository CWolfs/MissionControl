using System;
using UnityEngine;

using BattleTech;
using HBS.Collections;

namespace SpawnVariation.Utils {
  public class PathTester {
    public static Mech CreateTestMech() {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      string spawnerId = Guid.NewGuid().ToString();
      string uniqueId = $"{spawnerId}.9999999999";
      
      HeraldryDef heraldryDef = null;
      combatState.DataManager.Heraldries.TryGet(HeraldryDef.HeraldyrDef_SinglePlayerSkirmishPlayer1, out heraldryDef);

      MechDef mechDef = null;
      combatState.DataManager.MechDefs.TryGet("mechdef_spider_SDR-5V", out mechDef);
      // mechDef.CheckDependenciesAfterLoad(new DataManagerRequestCompleteMessage(BattleTechResourceType.MechDef, null));

      PilotDef pilot = combatState.DataManager.PilotDefs.Get("pilot_default");

      Mech mech = new Mech(mechDef, pilot, new TagSet(), uniqueId, combatState, spawnerId, heraldryDef);
      return mech;
    }

    public static bool IsSpawnValid(Vector3 position, Vector3 validityPosition) {
      Mech mech = CreateTestMech();
      UnityGameInstance.BattleTechGame.DataManager.RequestResource(BattleTechResourceType.Prefab, mech.MechDef.Chassis.PrefabIdentifier, new BattleTech.Data.PrewarmRequest());
      UnityGameInstance.BattleTechGame.DataManager.ProcessRequests();
      // UnityGameInstance.BattleTechGame.DataManager.PrecachePrefab(mech.MechDef.Chassis.PrefabIdentifier, BattleTechResourceType.Prefab, 1);
      mech.Init(position, 0, mech.thisUnitChecksEncounterCells);
      mech.InitGameRep(null);
      return true;
    }
  }
}