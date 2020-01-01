using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class AmbushConvoyEncounterRules : EncounterRules {
    public AmbushConvoyEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AmbushConvoyEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("ConvoyUnit1Spawn", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[AmbushConvoyEncounterRules] Building spawns rules");
      if (MissionControl.Instance.IsExtendedBoundariesAllowed()) {
        Main.Logger.Log("[AmbushConvoyEncounterRules] Using Extended Boundary spawn rules");
        EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerPlayerLance", "ConvoyUnit1Spawn", SceneManipulationLogic.LookDirection.TOWARDS_TARGET, 400, 600, true));
      } else {
        EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "ConvoyUnit1Spawn"));
      }
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["ConvoyUnit1Spawn"] = EncounterLayerData.gameObject.FindRecursive("UnitSpawnPoint1");
    }
  }
}