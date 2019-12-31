using UnityEngine;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class LookAtTarget : SceneManipulationLogic {
    private string focusKey = "";
    private string orientationTargetKey = "";
    private GameObject focus;
    private GameObject orientationTarget;
    private bool isLance = true;

    public LookAtTarget(EncounterRules encounterRules, string focusKey, string orientationTargetKey, bool isLance) : base(encounterRules) {
      this.focusKey = focusKey;
      this.orientationTargetKey = orientationTargetKey;
      this.isLance = isLance;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[LookAtTarget] For {focus.name} to look at {orientationTarget.name}");

      if (isLance) SaveSpawnPositions(focus);
      RotateToTarget(focus, orientationTarget);
      if (isLance) RestoreLanceMemberSpawnPositions(focus);
    }

    protected override bool GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(focusKey, out focus);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (focus == null) {
        Main.Logger.LogWarning($"[LookAtTarget] Object reference for focus '{focusKey}' is null. This will be handled gracefully.");
        return false;
      }

      if (orientationTarget == null) {
        Main.Logger.LogWarning($"[LookAtTarget] Object reference for orientation target '{orientationTarget}' is null. This will be handled gracefully.");
        return false;
      }

      return true;
    }
  }
}