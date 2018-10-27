using System;
using UnityEngine;

namespace MissionControl.Utils {
  public struct RectEdgePosition {
    public Vector3 Position { get; set; }
    public RectExtensions.RectEdge Edge { get; private set; }

    public RectEdgePosition(Vector3 position, RectExtensions.RectEdge edge) {
      this.Position = position;
      this.Edge = edge;
    }
  }
}