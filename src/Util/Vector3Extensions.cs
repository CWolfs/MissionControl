using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using SpawnVariation;

public static class Vector3Extensions {
  public static Vector3 Flattened(this Vector3 vector) {
		return new Vector3(vector.x, 0f, vector.z);
	}

  public static float DistanceFlat(this Vector3 origin, Vector3 destination) {
		return Vector3.Distance(origin.Flattened(), destination.Flattened());
	}
}