using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class CaptureEscortAdditionalBlockersEncounterRules : EncounterRules {
    public CaptureEscortAdditionalBlockersEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[CaptureEscortAdditionalBlockersEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("EnemyBlockingLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[CaptureEscortAdditionalBlockersEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "EscortRegion"));
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "HunterLance", "EscortExtractionRegion", 200, true));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["EnemyBlockingLance"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_BlockingForce");
      ObjectLookup["EscortRegion"] = EncounterLayerData.gameObject.FindRecursive("Region_Occupy");
      ObjectLookup["HunterLance"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_Hunter");
      ObjectLookup["EscortExtractionRegion"] = EncounterLayerData.gameObject.FindRecursive("Region_Extraction");
    }
  }
}