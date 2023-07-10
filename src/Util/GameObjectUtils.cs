using System.Collections.Generic;

using UnityEngine;

namespace MissionControl.Utils {
  public static class GameObjectUtils {
    public static List<GameObject> FindContains(string namePart) {
      List<GameObject> matches = new List<GameObject>();

      foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject))) {
        if (go.name.Contains(namePart)) {
          matches.Add(go);
        }
      }

      return matches;
    }
  }
}