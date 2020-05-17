using UnityEngine;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DestroyBaseJointAssaultEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }

    public DestroyBaseJointAssaultEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DestroyBaseJointAssaultEncounterRules] Setting up rule object references");
      BuildRandomSpawn();
      BuildAdditionalLances("PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    private void BuildRandomSpawn() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[DestroyBaseJointAssaultEncounterRules] Building player spawn rule");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase", 400));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["PlotBase"] = GameObject.Find(GetPlotBaseName(mapName));
    }
  }
}