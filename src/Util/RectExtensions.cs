using UnityEngine;
using System.Collections;

using SpawnVariation;

public static class RectExtensions {
	public static Vector3 CalculateRandomXZEdge(this Rect rect) {
    float x = 0;
    float y = 0;
    float z = 0;
    float width = rect.width;
    float buffer = 100f;
  
    // Randomly select an edge
    UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
    int edge = UnityEngine.Random.Range(0, 3);
    switch (edge) {
      case 0: { // Forward (Max-Z)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Forward");
        x = UnityEngine.Random.Range(-width / 2f, width / 2f);
        z = (width / 2f) - buffer;
        break;
      }
      case 1: { // Right (Max-X)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Right");
        x = (width / 2f) - buffer;
        z = UnityEngine.Random.Range(-width / 2f, width / 2f);
        break;
      }
      case 2: {  // Back (Min-Z)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Back");
        x = UnityEngine.Random.Range(-width / 2f, width / 2f);
        z = -(width / 2f) + buffer;
        break;
      }
      case 3: { // Left (Min-X)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Left");
        x = -(width / 2f) + buffer;
        z = UnityEngine.Random.Range(-width / 2f, width / 2f);
        break;
      }
    }

    return new Vector3(x, y, z);
  }

  public static Vector3 CalculateRandomXZEdge(this Rect rect, Vector3 offset) {
    return rect.CalculateRandomXZEdge() + offset;
  }
}