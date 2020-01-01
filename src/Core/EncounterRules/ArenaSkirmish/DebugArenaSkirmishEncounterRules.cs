using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DebugArenaSkirmishEncounterRules : EncounterRules {
    public DebugArenaSkirmishEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DebugArenaSkirmishEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("Player2LanceSpawner", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[DebugArenaSkirmishEncounterRules] Building spawns rules");

      EncounterLogic.Add(new SpawnLanceAnywhere(this, "Player2LanceSpawner", "SpawnerPlayerLance", true));
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerPlayerLance", "Player2LanceSpawner", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 150f, true));
      EncounterLogic.Add(new LookAtTarget(this, "Player2LanceSpawner", "SpawnerPlayerLance", true));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["Player2LanceSpawner"] = EncounterLayerData.gameObject.FindRecursive("Player2LanceSpawner");
    }
  }
}