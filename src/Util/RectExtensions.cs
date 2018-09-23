using UnityEngine;
using System.Collections;

using ContractCommand;
using ContractCommand.Utils;

public static class RectExtensions {
  public enum RectEdge { ANY = -1, MIN_X = 0, MAX_X = 1, MIN_Z = 2, MAX_Z = 3 }

	public static RectEdgePosition CalculateRandomXZEdge(this Rect rect, RectEdge edge) {
    float x = 0f;
    float y = 0f;
    float z = 0f;
    float width = rect.width;
    float height = rect.height;
    float buffer = 100f;
    float halfBoundaryXWidth = width / 2f;
    float halfBoundaryZWidth = height / 2f;
    float minHalfBoundaryXWidth = (halfBoundaryXWidth * -1f) + buffer;
    float maxHalfBoundaryXWidth = halfBoundaryXWidth - buffer;
    float minHalfBoundaryZWidth = (halfBoundaryZWidth * -1f) + buffer;
    float maxHalfBoundaryZWidth = halfBoundaryZWidth - buffer;
  
    // Randomly select an edge
    RectEdge recEdge = (edge == RectEdge.ANY) ? (RectEdge)UnityEngine.Random.Range(0, 4) : edge;
    switch (recEdge) {
      case RectEdge.MAX_Z: { // Forward (Max-Z)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Forward (Max-Z edge)");
        x = UnityEngine.Random.Range(minHalfBoundaryXWidth, maxHalfBoundaryXWidth);
        z = maxHalfBoundaryZWidth;
        break;
      }
      case RectEdge.MAX_X: { // Right (Max-X)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Right (Max-X edge)");
        x = maxHalfBoundaryXWidth;
        z = UnityEngine.Random.Range(minHalfBoundaryZWidth, maxHalfBoundaryZWidth);
        break;
      }
      case RectEdge.MIN_Z: {  // Back (Min-Z)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Back (Min-Z edge)");
        x = UnityEngine.Random.Range(minHalfBoundaryXWidth, maxHalfBoundaryXWidth);
        z = minHalfBoundaryZWidth;
        break;
      }
      case RectEdge.MIN_X: { // Left (Min-X)
        Main.Logger.LogDebug("[CalculateRandomXZEdge] Selecting Left (Min-X edge)");
        x = minHalfBoundaryXWidth;
        z = UnityEngine.Random.Range(minHalfBoundaryZWidth, maxHalfBoundaryZWidth);
        break;
      }
    }

    // return new Vector3(x, y, z);
    return new RectEdgePosition(new Vector3(x, y, z), recEdge);
  }

  public static RectEdgePosition CalculateRandomXZEdge(this Rect rect, Vector3 offset, RectEdge edge) {
    RectEdgePosition edgePosition = rect.CalculateRandomXZEdge(edge);
    edgePosition.Position = edgePosition.Position + offset;
    return edgePosition;
  }
}