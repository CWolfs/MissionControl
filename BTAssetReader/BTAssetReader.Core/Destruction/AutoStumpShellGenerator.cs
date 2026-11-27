using System;
using System.Collections.Generic;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.Destruction {
  /// <summary>
  /// Generates a footprint-accurate ruined building shell ("Auto-Stump") by slicing the
  /// intact mesh horizontally at stumpHeightFraction with a deterministic jagged profile
  /// and capping the top with an interior rubble mound.
  /// </summary>
  public static class AutoStumpShellGenerator {
    private const float Eps = 1e-5f;

    public static RawMeshData Generate(RawMeshData sourceMesh, float stumpHeightFraction = 0.22f, int seed = 0) {
      if (sourceMesh == null || sourceMesh.Vertices == null || sourceMesh.Vertices.Length == 0 || sourceMesh.SubMeshIndices == null) {
        return null;
      }

      MeshNormaliser.RecalculateBounds(sourceMesh);
      RawBounds b = sourceMesh.LocalAabb;
      float minY = b.Center.Y - b.Extents.Y;
      float height = Math.Max(0.25f, b.Extents.Y * 2f);
      float clampedFrac = Math.Max(0.08f, Math.Min(0.65f, stumpHeightFraction));
      float baseCutY = minY + height * clampedFrac;

      float spanX = Math.Max(1f, b.Extents.X * 2f);
      float spanZ = Math.Max(1f, b.Extents.Z * 2f);
      float amplitude = Math.Max(0.2f, Math.Min(2.0f, height * 0.09f));
      if (baseCutY - amplitude < minY + 0.1f) {
        amplitude = Math.Max(0.05f, (baseCutY - minY) * 0.45f);
      }

      var rng = new Random(seed != 0 ? seed + 7919 : 7919);
      float p1 = (float)(rng.NextDouble() * Math.PI * 2.0);
      float p2 = (float)(rng.NextDouble() * Math.PI * 2.0);
      float p3 = (float)(rng.NextDouble() * Math.PI * 2.0);
      float fx = (float)(Math.PI * 2.5 / spanX);
      float fz = (float)(Math.PI * 2.5 / spanZ);
      float fDiag = (float)(Math.PI * 3.2 / Math.Max(spanX, spanZ));

      float CutHeightAt(float x, float z) {
        float w1 = (float)(Math.Sin(x * fx + p1) * Math.Cos(z * fz + p2));
        float w2 = (float)Math.Sin((x + z) * fDiag + p3);
        return baseCutY + amplitude * (0.55f * w1 + 0.45f * w2);
      }

      int origSubCount = sourceMesh.SubMeshIndices.Length;
      int interiorSubMesh = origSubCount;
      var tris = PlaneSliceFracturer.ExtractTriangles(sourceMesh);

      var kept = new List<PlaneSliceFracturer.Tri>();
      var rimSegments = new List<(Float3 A, Float3 B)>();

      for (int i = 0; i < tris.Count; i++) {
        var t = tris[i];
        float da = t.A.Pos.Y - CutHeightAt(t.A.Pos.X, t.A.Pos.Z);
        float db = t.B.Pos.Y - CutHeightAt(t.B.Pos.X, t.B.Pos.Z);
        float dc = t.C.Pos.Y - CutHeightAt(t.C.Pos.X, t.C.Pos.Z);

        bool ka = da <= Eps;
        bool kb = db <= Eps;
        bool kc = dc <= Eps;

        if (ka && kb && kc) {
          kept.Add(t);
          continue;
        }
        if (!ka && !kb && !kc) {
          continue;
        }

        // Straddling triangle
        PlaneSliceFracturer.Vert v0, v1, v2;
        float d0, d1, d2;
        bool loneKept;

        if (ka != kb && ka != kc) {
          v0 = t.A; v1 = t.B; v2 = t.C; d0 = da; d1 = db; d2 = dc; loneKept = ka;
        } else if (kb != ka && kb != kc) {
          v0 = t.B; v1 = t.C; v2 = t.A; d0 = db; d1 = dc; d2 = da; loneKept = kb;
        } else {
          v0 = t.C; v1 = t.A; v2 = t.B; d0 = dc; d1 = da; d2 = db; loneKept = kc;
        }

        var i01 = PlaneSliceFracturer.LerpVert(v0, v1, PlaneSliceFracturer.SafeFraction(d0, d1));
        var i02 = PlaneSliceFracturer.LerpVert(v0, v2, PlaneSliceFracturer.SafeFraction(d0, d2));

        if (loneKept) {
          kept.Add(new PlaneSliceFracturer.Tri { A = v0, B = i01, C = i02, SubMesh = t.SubMesh });
          rimSegments.Add((i01.Pos, i02.Pos));
        } else {
          kept.Add(new PlaneSliceFracturer.Tri { A = i01, B = v1, C = v2, SubMesh = t.SubMesh });
          kept.Add(new PlaneSliceFracturer.Tri { A = i01, B = v2, C = i02, SubMesh = t.SubMesh });
          rimSegments.Add((i02.Pos, i01.Pos));
        }
      }

      if (kept.Count == 0) {
        // Fallback: tiny low rubble box if source geometry had no vertices below baseCutY
        return MeshNormaliser.CreateBoxMesh(new RawBounds {
          Center = new Float3(b.Center.X, minY + height * 0.1f, b.Center.Z),
          Extents = new Float3(b.Extents.X * 0.9f, Math.Max(0.2f, height * 0.1f), b.Extents.Z * 0.9f)
        }, $"{sourceMesh.Name ?? "Prop"}_shell");
      }

      // Cap the ruined stump opening with a gentle interior rubble mound
      if (rimSegments.Count >= 3) {
        double cx = 0, cy = 0, cz = 0;
        for (int i = 0; i < rimSegments.Count; i++) {
          cx += rimSegments[i].A.X + rimSegments[i].B.X;
          cy += rimSegments[i].A.Y + rimSegments[i].B.Y;
          cz += rimSegments[i].A.Z + rimSegments[i].B.Z;
        }
        double inv = 1.0 / (rimSegments.Count * 2.0);
        // Slightly dipped/uneven rubble interior centre
        Float3 center = new Float3(
          (float)(cx * inv),
          Math.Max(minY + 0.15f, (float)(cy * inv) - amplitude * 0.2f),
          (float)(cz * inv));

        Float3 up = new Float3(0f, 1f, 0f);
        Float3 uAxis = new Float3(1f, 0f, 0f);
        Float3 vAxis = new Float3(0f, 0f, 1f);

        for (int i = 0; i < rimSegments.Count; i++) {
          Float3 a = rimSegments[i].A;
          Float3 bPos = rimSegments[i].B;
          Float3 fn = MeshNormaliser.Cross(MeshNormaliser.Sub(bPos, a), MeshNormaliser.Sub(center, a));
          float areaSq = fn.X * fn.X + fn.Y * fn.Y + fn.Z * fn.Z;
          if (areaSq < 1e-8f) continue;

          bool facesUp = fn.Y >= 0f;
          Float3 n = MeshNormaliser.Normalize(facesUp ? fn : new Float3(-fn.X, -fn.Y, -fn.Z));
          if (n.Y < 0.25f) n = up;

          var va = new PlaneSliceFracturer.Vert { Pos = a, Norm = n, Uv = PlaneSliceFracturer.ProjectUv(a, uAxis, vAxis) };
          var vb = new PlaneSliceFracturer.Vert { Pos = bPos, Norm = n, Uv = PlaneSliceFracturer.ProjectUv(bPos, uAxis, vAxis) };
          var vc = new PlaneSliceFracturer.Vert { Pos = center, Norm = up, Uv = PlaneSliceFracturer.ProjectUv(center, uAxis, vAxis) };

          if (facesUp) {
            kept.Add(new PlaneSliceFracturer.Tri { A = va, B = vb, C = vc, SubMesh = interiorSubMesh });
          } else {
            kept.Add(new PlaneSliceFracturer.Tri { A = va, B = vc, C = vb, SubMesh = interiorSubMesh });
          }
        }
      }

      int totalSubSlots = origSubCount + 1;
      var trisBySub = new List<int>[totalSubSlots];
      for (int s = 0; s < totalSubSlots; s++) trisBySub[s] = new List<int>();

      var verts = new List<Float3>(kept.Count * 3);
      var norms = new List<Float3>(kept.Count * 3);
      var uvs = new List<Float2>(kept.Count * 3);

      for (int i = 0; i < kept.Count; i++) {
        var t = kept[i];
        int slot = Math.Max(0, Math.Min(totalSubSlots - 1, t.SubMesh));
        int baseIdx = verts.Count;

        verts.Add(t.A.Pos);
        verts.Add(t.B.Pos);
        verts.Add(t.C.Pos);
        norms.Add(t.A.Norm);
        norms.Add(t.B.Norm);
        norms.Add(t.C.Norm);
        uvs.Add(t.A.Uv);
        uvs.Add(t.B.Uv);
        uvs.Add(t.C.Uv);

        trisBySub[slot].Add(baseIdx);
        trisBySub[slot].Add(baseIdx + 1);
        trisBySub[slot].Add(baseIdx + 2);
      }

      bool hasInterior = trisBySub[interiorSubMesh].Count > 0;
      int finalSubCount = hasInterior ? totalSubSlots : origSubCount;
      var subMeshes = new RawSubMesh[finalSubCount];
      var subIndices = new int[finalSubCount][];
      for (int s = 0; s < finalSubCount; s++) {
        int[] arr = trisBySub[s].ToArray();
        subIndices[s] = arr;
        subMeshes[s] = new RawSubMesh { Topology = 0, IndexCount = (uint)arr.Length };
      }

      var result = new RawMeshData {
        Name = $"{sourceMesh.Name ?? "Prop"}_shell",
        VertexCount = verts.Count,
        IndexFormat = verts.Count > 65535 ? 1 : 0,
        Vertices = verts.ToArray(),
        Normals = norms.ToArray(),
        Uv0 = uvs.ToArray(),
        SubMeshes = subMeshes,
        SubMeshIndices = subIndices,
        OriginalIsReadable = true
      };
      MeshNormaliser.RecalculateTangentsIfMissing(result);
      MeshNormaliser.RecalculateBounds(result);
      return result;
    }
  }
}
