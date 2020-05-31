using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class AssassinateEncounterRules : EncounterRules {
    public AssassinateEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AssassinateEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("AssassinateSpawn", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
        "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[AssassinateEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "AssassinateSpawn"));
      EncounterLogic.Add(new SpawnLanceAnywhere(this, "AssassinateSpawn", "SpawnerPlayerLance", 400, true));
      EncounterLogic.Add(new SpawnObjectAnywhere(this, "TargetEscapeZone", "AssassinateSpawn", 480));
      EncounterLogic.Add(new LookAtTarget(this, "SpawnerPlayerLance", "AssassinateSpawn", true));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["AssassinateSpawn"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_AssassinationTarget");
      ObjectLookup["TargetEscapeZone"] = EncounterLayerData.gameObject.FindRecursive("Region_TargetEscapeZone");
    }
  }
}