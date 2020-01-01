using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class SimpleBattleEncounterRules : EncounterRules {
    public SimpleBattleEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[SimpleBattleEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("LanceEnemyOpposingForce", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
        "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[SimpleBattleEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "LanceEnemyOpposingForce", 400f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["LanceEnemyOpposingForce"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_OpposingForce");
    }
  }
}