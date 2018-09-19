using UnityEngine;
using System.Collections;

using SpawnVariation;

public static class RectExtensions {
	public static Vector3 CalculateRandomXZEdge(this Rect rect) {
    float x = 0;
    float y = 0;
    float z = 0;
    float width = rect.width;
    float height = rect.height;
    float buffer = 100f;
    float halfBoundaryXWidth = width / 2f;
    float halfBoundaryZWidth = height / 2f;
  
    // Randomly select an edge
    int edge = UnityEngine.Random.Range(0, 4);
    switch (edge) {
      case 0: { // Forward (Max-Z)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Forward (Max-Z edge)");
        x = UnityEngine.Random.Range(-halfBoundaryXWidth, halfBoundaryXWidth);
        z = halfBoundaryZWidth - buffer;
        break;
      }
      case 1: { // Right (Max-X)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Right (Max-X edge)");
        x = halfBoundaryXWidth - buffer;
        z = UnityEngine.Random.Range(-halfBoundaryZWidth, halfBoundaryZWidth);
        break;
      }
      case 2: {  // Back (Min-Z)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Back (Min-Z edge)");
        x = UnityEngine.Random.Range(-halfBoundaryXWidth, halfBoundaryXWidth);
        z = -halfBoundaryZWidth + buffer;
        break;
      }
      case 3: { // Left (Min-X)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Left (Min-X edge)");
        x = -halfBoundaryXWidth + buffer;
        z = UnityEngine.Random.Range(-halfBoundaryZWidth, halfBoundaryZWidth);
        break;
      }
    }

    return new Vector3(x, y, z);
  }

  public static Vector3 CalculateRandomXZEdge(this Rect rect, Vector3 offset) {
    return rect.CalculateRandomXZEdge() + offset;
  }
}