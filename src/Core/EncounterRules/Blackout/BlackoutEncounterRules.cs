using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class BlackoutEncounterRules : EncounterRules {
    public BlackoutEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[BlackoutEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("RoamingForce", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
        "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[BlackoutEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "FirstRegion", 200f, 800f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["FirstRegion"] = EncounterLayerData.gameObject.FindRecursive("Region_Investigate_Blackout");
      ObjectLookup["RoamingForce"] = EncounterLayerData.gameObject.FindRecursive("Spawner_Enemy_RoamingForce");
    }
  }
}