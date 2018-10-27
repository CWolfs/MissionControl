using UnityEngine;
using System.Collections.Generic;

namespace MissionControl.Utils {
  public class Vector3Comparer : IEqualityComparer<Vector3> {
    public bool Equals(Vector3 v1, Vector3 v2) {
      return v1 == v2;
    }

    public int GetHashCode(Vector3 v) {
      return v.GetHashCode();
    }
  }
}