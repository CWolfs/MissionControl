using UnityEngine;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class LookAwayFromTarget : SceneManipulationLogic {
    private string focusKey = "";
    private string orientationTargetKey = "";
    private GameObject focus;
    private GameObject orientationTarget;
    private bool isLance;

    public LookAwayFromTarget(EncounterRules encounterRules, string focusKey, string orientationTargetKey, bool isLance) : base(encounterRules) {
      this.focusKey = focusKey;
      this.orientationTargetKey = orientationTargetKey;
      this.isLance = isLance;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[LookAwayFromTarget] For {focus.name} to look away from {orientationTarget.name}");

      if (isLance) SaveSpawnPositions(focus);
      RotateAwayFromTarget(focus, orientationTarget);
      if (isLance) RestoreLanceMemberSpawnPositions(focus);      
    }

    protected override bool GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(focusKey, out focus);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (focus == null) {
        Main.Logger.LogWarning($"[LookAwayFromTarget] Object reference for focus '{focusKey}' is null. This will be handled gracefully.");
        return false;
      }

      if (orientationTarget == null) {
        Main.Logger.LogWarning($"[LookAwayFromTarget] Object reference for orientation target '{orientationTarget}' is null. This will be handled gracefully.");
        return false;
      }

      return true;
    }
  }
}