using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class ArenaSkirmishEncounterRules : EncounterRules {
    public ArenaSkirmishEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[ArenaSkirmishEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("Player2LanceSpawner", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[ArenaSkirmishEncounterRules] Building spawns rules");

      EncounterLogic.Add(new SpawnLanceAnywhere(this, "Player2LanceSpawner", "SpawnerPlayerLance", true));
      EncounterLogic.Add(new SpawnLanceAnywhere(this, "SpawnerPlayerLance", "Player2LanceSpawner", true));
      EncounterLogic.Add(new LookAtTarget(this, "Player2LanceSpawner", "SpawnerPlayerLance", true));
      EncounterLogic.Add(new LookAtTarget(this, "SpawnerPlayerLance", "Player2LanceSpawner", true));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["Player2LanceSpawner"] = EncounterLayerData.gameObject.FindRecursive("Player2LanceSpawner");
    }
  }
}