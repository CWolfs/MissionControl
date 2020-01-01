using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class AttackDefendEncounterRules : EncounterRules {
    public AttackDefendEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AttackDefendEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("SpawnerLanceEnemyTurret", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerLanceFriendlyTurret", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 50f, 150f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[DefendBaseEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerPlayerLance", "SpawnerLanceFriendlyTurret", "SpawnerLanceEnemyTurret", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 150f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["SpawnerLanceFriendlyTurret"] = EncounterLayerData.gameObject.FindRecursive("Lance_Friendly_BaseTurrets");
      ObjectLookup["SpawnerLanceEnemyTurret"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_Turret");
    }
  }
}