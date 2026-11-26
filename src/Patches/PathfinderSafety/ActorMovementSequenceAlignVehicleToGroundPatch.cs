using UnityEngine;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ActorMovementSequence), "AlignVehicleToGround")]
  public static class ActorMovementSequenceAlignVehicleToGroundPatch {
    private const float MIN_GROUND_NORMAL_Y = 0.25f;

    public static bool Prefix(Transform vehicleTransform, float deltaTime) {
      if (!MissionControl.Instance.AllowMissionControl(true)) return true;
      if (vehicleTransform == null) return false;

      RaycastHit[] hits = Physics.RaycastAll(
        new Ray(vehicleTransform.position + Vector3.up * 20f, Vector3.down),
        40f,
        PathFinderManager.GroundRaycastMask,
        QueryTriggerInteraction.Ignore
      );

      RaycastHit? highestHit = null;
      for (int i = 0; i < hits.Length; i++) {
        RaycastHit hit = hits[i];
        if (hit.transform == null || hit.transform == vehicleTransform || hit.transform.IsChildOf(vehicleTransform)) {
          continue;
        }
        if (hit.normal.y < MIN_GROUND_NORMAL_Y) {
          continue;
        }
        if (!PathFinderManager.IsValidGroundCollider(hit.collider, out _)) {
          continue;
        }

        if (!highestHit.HasValue || highestHit.Value.point.y < hit.point.y) {
          highestHit = hit;
        }
      }

      if (highestHit.HasValue) {
        Vector3 normal = highestHit.Value.normal.normalized;
        Vector3 flatForward = Vector3.ProjectOnPlane(vehicleTransform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f) {
          flatForward = Quaternion.Euler(0f, vehicleTransform.rotation.eulerAngles.y, 0f) * Vector3.forward;
        } else {
          flatForward.Normalize();
        }

        float slopeY = -(flatForward.x * normal.x + flatForward.z * normal.z) / normal.y;
        Vector3 slopeForward = new Vector3(flatForward.x, slopeY, flatForward.z).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(slopeForward, normal);
        vehicleTransform.rotation = Quaternion.RotateTowards(vehicleTransform.rotation, targetRotation, 180f * Mathf.Max(0f, deltaTime));
      }

      return false;
    }
  }
}
