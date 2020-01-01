using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class RescueEncounterRules : EncounterRules {
    public RescueEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[RescueEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("OccupyRegion1VIPGo", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[RescueEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "OccupyRegion1VIPGo"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["OccupyRegion1VIPGo"] = EncounterLayerData.gameObject.FindRecursive("Chunk_OccupyRegion_1_VIP");
    }
  }
}