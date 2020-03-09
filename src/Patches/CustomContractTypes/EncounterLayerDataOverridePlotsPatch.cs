using Harmony;

using UnityEngine;

using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerData), "OverridePlots")]
  public class EncounterLayerDataOverridePlotsPatch {
    static void Postfix(EncounterLayerData __instance) {
      Main.LogDebug($"[EncounterLayerData.Postfix] Running...");
      List<object[]> queuedBuildingMounts = MissionControl.Instance.QueuedBuildingMounts;
      foreach (object[] mountInfo in queuedBuildingMounts) {
        SetMountOnPosition((GameObject)mountInfo[0], (string)mountInfo[1]);
      }
      MissionControl.Instance.QueuedBuildingMounts.Clear();
    }

    public static void SetMountOnPosition(GameObject target, string mountTargetPath) {
      GameObject go = GameObject.Find(mountTargetPath);
      if (go == null) {
        Main.Logger.LogError($"[EncounterLayerDataLoadPatch.SetMountOnPositions] Target '{mountTargetPath}' could not be found");
        return;
      } else {
        Main.LogDebug($"[EncounterLayerDataLoadPatch.SetMountOnPositions] Target '{mountTargetPath}' found with '{go.name}'");
      }

      Vector3 pos = go.transform.position;
      Collider col = go.GetComponentInChildren<Collider>();

      RaycastHit[] hits = Physics.RaycastAll(new Vector3(pos.x, pos.y + 500f, pos.z), go.transform.TransformDirection(Vector3.down), 1000f);
      foreach (RaycastHit hit1 in hits) {
        if (hit1.collider.gameObject.name == col.gameObject.name) {
          pos.y = hit1.point.y;
        }
      }

      target.transform.position = pos;
    }
  }
}