using UnityEngine;
using System.Collections;

using MissionControl;
using MissionControl.Utils;

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

  public static bool Intersects(this Rect r1, Rect r2, out Rect area) {
    area = new Rect();

    if (r2.Overlaps(r1)) {
      float x1 = Mathf.Min(r1.xMax, r2.xMax);
      float x2 = Mathf.Max(r1.xMin, r2.xMin);
      float y1 = Mathf.Min(r1.yMax, r2.yMax);
      float y2 = Mathf.Max(r1.yMin, r2.yMin);
      area.x = Mathf.Min(x1, x2);
      area.y = Mathf.Min(y1, y2);
      area.width = Mathf.Max(0.0f, x1 - x2);
      area.height = Mathf.Max(0.0f, y1 - y2);
      
      return true;
    }

    return false;
  }

  public static Rect GenerateUsableBoundary(this Rect boundaryRec) {
    float mapBorderSize = 50f;
    float mapSize = 2048f;
    Rect edgeOfMapRec = new Rect(-((mapSize) / 2f), -((mapSize) / 2f), mapSize - mapBorderSize, mapSize - mapBorderSize);
    Rect boundaryIntersect;
    boundaryRec.Intersects(edgeOfMapRec, out boundaryIntersect);
    return boundaryIntersect;
  }
}