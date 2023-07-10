using UnityEngine;

using System.Collections.Generic;

namespace MissionControl.Utils {
  public class MeshTools {
    public static Mesh CreateHexigon(float radius) {
      Vector2[] vertices2d = new Vector2[] {
        new Vector2(0, radius),
        new Vector2(radius, radius / 2f),
        new Vector2(radius, -(radius / 2f)),
        new Vector2(0, -radius),
        new Vector2(-radius, -(radius / 2f)),
        new Vector2(-radius, radius / 2f),
      };

      Triangulator tr = new Triangulator(vertices2d);
      int[] indices = tr.Triangulate();

      Vector3[] vertices = new Vector3[vertices2d.Length];
      for (int i = 0; i < vertices.Length; i++) {
        vertices[i] = new Vector3(vertices2d[i].x, 0, vertices2d[i].y);
      }

      Mesh mesh = new Mesh();
      mesh.vertices = vertices;
      mesh.triangles = indices;
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      return mesh;
    }

    public static Mesh CreateHexigon(float radius, List<Vector3> points) {
      Vector2[] vertices2d = new Vector2[] {
        new Vector2(0, radius),
        new Vector2(radius, radius / 2f),
        new Vector2(radius, -(radius / 2f)),
        new Vector2(0, -radius),
        new Vector2(-radius, -(radius / 2f)),
        new Vector2(-radius, radius / 2f),
      };

      Triangulator tr = new Triangulator(vertices2d);
      int[] indices = tr.Triangulate();

      Vector3[] vertices = new Vector3[vertices2d.Length];
      for (int i = 0; i < vertices.Length; i++) {
        vertices[i] = new Vector3(vertices2d[i].x, points[i].y, vertices2d[i].y);
      }

      Mesh mesh = new Mesh();
      mesh.vertices = vertices;
      mesh.triangles = indices;
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      return mesh;
    }
  }
}