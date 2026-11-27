using System;
using UnityEngine;
using UnityEngine.Rendering;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Unity {
  public enum MeshPivotMode {
    Original = 0,
    Center = 1,
    Bottom = 2
  }

  public static class UnityMeshBuilder {
    public static Mesh Build(RawMeshData raw, bool makeReadable = true, MeshPivotMode pivotMode = MeshPivotMode.Original) {
      if (raw == null || raw.Vertices == null || raw.Vertices.Length == 0) return null;

      Mesh mesh = new Mesh {
        name = raw.Name ?? "VanillaMesh",
        indexFormat = raw.IndexFormat == 1 ? IndexFormat.UInt32 : IndexFormat.UInt16
      };

      int vCount = raw.Vertices.Length;
      Vector3[] verts = new Vector3[vCount];
      Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
      Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

      for (int i = 0; i < vCount; i++) {
        var v = raw.Vertices[i];
        var p = new Vector3(v.X, v.Y, v.Z);
        verts[i] = p;
        if (p.x < min.x) min.x = p.x;
        if (p.y < min.y) min.y = p.y;
        if (p.z < min.z) min.z = p.z;
        if (p.x > max.x) max.x = p.x;
        if (p.y > max.y) max.y = p.y;
        if (p.z > max.z) max.z = p.z;
      }

      Vector3 pivotShift = Vector3.zero;
      if (pivotMode == MeshPivotMode.Center) {
        pivotShift = (min + max) * 0.5f;
      } else if (pivotMode == MeshPivotMode.Bottom) {
        pivotShift = new Vector3((min.x + max.x) * 0.5f, min.y, (min.z + max.z) * 0.5f);
      }

      if (pivotShift != Vector3.zero) {
        for (int i = 0; i < vCount; i++) {
          verts[i] -= pivotShift;
        }
      }

      mesh.vertices = verts;

      if (raw.Normals != null && raw.Normals.Length == vCount) {
        Vector3[] normals = new Vector3[vCount];
        for (int i = 0; i < vCount; i++) {
          var n = raw.Normals[i];
          normals[i] = new Vector3(n.X, n.Y, n.Z);
        }
        mesh.normals = normals;
      }

      if (raw.Tangents != null && raw.Tangents.Length == vCount) {
        Vector4[] tangents = new Vector4[vCount];
        for (int i = 0; i < vCount; i++) {
          var t = raw.Tangents[i];
          tangents[i] = new Vector4(t.X, t.Y, t.Z, t.W);
        }
        mesh.tangents = tangents;
      }

      if (raw.Colors != null && raw.Colors.Length == vCount) {
        Color32[] colors = new Color32[vCount];
        for (int i = 0; i < vCount; i++) {
          var c = raw.Colors[i];
          colors[i] = new Color32(c.R, c.G, c.B, c.A);
        }
        mesh.colors32 = colors;
      }

      if (raw.Uv0 != null && raw.Uv0.Length == vCount) {
        mesh.uv = ToVector2Array(raw.Uv0);
      }
      if (raw.Uv1 != null && raw.Uv1.Length == vCount) {
        mesh.uv2 = ToVector2Array(raw.Uv1);
      }
      if (raw.Uv2 != null && raw.Uv2.Length == vCount) {
        mesh.uv3 = ToVector2Array(raw.Uv2);
      }

      int subCount = raw.SubMeshIndices?.Length ?? 0;
      mesh.subMeshCount = subCount;
      for (int s = 0; s < subCount; s++) {
        int[] indices = raw.SubMeshIndices[s];
        MeshTopology topo = raw.SubMeshes != null && s < raw.SubMeshes.Length
          ? (MeshTopology)raw.SubMeshes[s].Topology
          : MeshTopology.Triangles;
        if (topo == MeshTopology.Triangles) {
          mesh.SetTriangles(indices, s, calculateBounds: false);
        } else {
          mesh.SetIndices(indices, topo, s, calculateBounds: false);
        }
      }

      if (pivotShift != Vector3.zero || (raw.LocalAabb.Extents.X == 0f && raw.LocalAabb.Extents.Y == 0f && raw.LocalAabb.Extents.Z == 0f)) {
        mesh.RecalculateBounds();
      } else {
        mesh.bounds = new Bounds(
          new Vector3(raw.LocalAabb.Center.X, raw.LocalAabb.Center.Y, raw.LocalAabb.Center.Z),
          new Vector3(raw.LocalAabb.Extents.X * 2f, raw.LocalAabb.Extents.Y * 2f, raw.LocalAabb.Extents.Z * 2f));
      }

      mesh.UploadMeshData(markNoLongerReadable: !makeReadable);
      return mesh;
    }

    private static Vector2[] ToVector2Array(Float2[] src) {
      Vector2[] dst = new Vector2[src.Length];
      for (int i = 0; i < src.Length; i++) {
        dst[i] = new Vector2(src[i].X, src[i].Y);
      }
      return dst;
    }
  }
}
