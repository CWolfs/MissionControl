using BattleTech;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class FireMissionEncounterRules : EncounterRules {
    public FireMissionEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[FireMissionEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("ChunkBeaconRegion2", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
        "SpawnerPlayerLance", SpawnLogic.LookDirection.TOWARDS_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[FireMissionEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerPlayerLance", "ChunkBeaconRegion1",
        SpawnLogic.LookDirection.TOWARDS_TARGET, 400, 600, true));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["ChunkBeaconRegion1"] = EncounterLayerData.gameObject.FindRecursive("Chunk_BeaconRegion_1");
      ObjectLookup["ChunkBeaconRegion2"] = EncounterLayerData.gameObject.FindRecursive("Chunk_BeaconRegion_2");
    }
  }
}