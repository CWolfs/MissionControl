using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class CaptureBaseJointAssaultEncounterRules : EncounterRules {
    public CaptureBaseJointAssaultEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[CaptureBaseJointAssaultEncounterRules] Setting up rule object references");
      BuildRandomSpawns();
      BuildAdditionalLances("PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[CaptureBaseJointAssaultEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["PlotBase"] = EncounterLayerData.gameObject.FindRecursive("Chunk_OccupyRegion_Base");
    }
  }
}