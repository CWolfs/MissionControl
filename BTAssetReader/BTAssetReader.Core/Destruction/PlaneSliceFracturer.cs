using System;
using System.Collections.Generic;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.Destruction {
  /// <summary>
  /// Deterministic pure-C# 3D plane-slicing mesh fracturer with cross-section caps.
  /// Shared identically by MissionControl, Designer, and Designer-Prop-Studio.
  /// </summary>
  public static class PlaneSliceFracturer {
    private const float PlaneEps = 1e-5f;

    internal struct Vert {
      public Float3 Pos;
      public Float3 Norm;
      public Float2 Uv;
    }

    internal struct Tri {
      public Vert A, B, C;
      public int SubMesh;
    }

    private sealed class WorkPiece {
      public readonly List<Tri> Tris = new List<Tri>();
      public Float3 Min;
      public Float3 Max;

      public float Volume {
        get {
          float dx = Math.Max(0.01f, Max.X - Min.X);
          float dy = Math.Max(0.01f, Max.Y - Min.Y);
          float dz = Math.Max(0.01f, Max.Z - Min.Z);
          return dx * dy * dz;
        }
      }

      public void RecomputeBounds() {
        Min = new Float3(float.MaxValue, float.MaxValue, float.MaxValue);
        Max = new Float3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < Tris.Count; i++) {
          Expand(Tris[i].A.Pos);
          Expand(Tris[i].B.Pos);
          Expand(Tris[i].C.Pos);
        }
      }

      private void Expand(Float3 p) {
        if (p.X < Min.X) Min.X = p.X;
        if (p.Y < Min.Y) Min.Y = p.Y;
        if (p.Z < Min.Z) Min.Z = p.Z;
        if (p.X > Max.X) Max.X = p.X;
        if (p.Y > Max.Y) Max.Y = p.Y;
        if (p.Z > Max.Z) Max.Z = p.Z;
      }
    }

    public static List<FracturePieceData> Fracture(RawMeshData sourceMesh, int targetPieces = 8, int seed = 0) {
      var results = new List<FracturePieceData>();
      if (sourceMesh == null || sourceMesh.Vertices == null || sourceMesh.Vertices.Length == 0 || sourceMesh.SubMeshIndices == null) {
        return results;
      }

      targetPieces = Math.Max(1, Math.Min(32, targetPieces));
      int origSubCount = sourceMesh.SubMeshIndices.Length;
      int interiorSubMesh = origSubCount; // Extra submesh slot for interior rubble cross-sections

      var rootPiece = ExtractWorkPiece(sourceMesh);
      if (rootPiece.Tris.Count == 0) return results;
      rootPiece.RecomputeBounds();

      var pieces = new List<WorkPiece> { rootPiece };
      var rng = new Random(seed != 0 ? seed : 1337);

      int safety = targetPieces * 6;
      while (pieces.Count < targetPieces && safety-- > 0) {
        // Pick largest splittable piece by bounding volume
        int bestIdx = -1;
        float bestVol = -1f;
        for (int i = 0; i < pieces.Count; i++) {
          if (pieces[i].Tris.Count < 4) continue;
          float v = pieces[i].Volume;
          if (v > bestVol) {
            bestVol = v;
            bestIdx = i;
          }
        }
        if (bestIdx < 0) break;

        WorkPiece target = pieces[bestIdx];
        bool splitSucceeded = false;

        for (int attempt = 0; attempt < 3; attempt++) {
          ComputeSlicePlane(target, rng, attempt == 2, out Float3 origin, out Float3 normal);
          SlicePiece(target, origin, normal, interiorSubMesh, out WorkPiece posPiece, out WorkPiece negPiece);

          if (posPiece.Tris.Count >= 2 && negPiece.Tris.Count >= 2) {
            posPiece.RecomputeBounds();
            negPiece.RecomputeBounds();
            pieces.RemoveAt(bestIdx);
            pieces.Add(posPiece);
            pieces.Add(negPiece);
            splitSucceeded = true;
            break;
          }
        }

        if (!splitSucceeded) break;
      }

      string baseName = sourceMesh.Name ?? "Prop";
      for (int i = 0; i < pieces.Count; i++) {
        FracturePieceData data = BuildFracturePiece(pieces[i], $"{baseName}_split_{i}", origSubCount, interiorSubMesh);
        if (data != null) results.Add(data);
      }

      return results;
    }

    internal static List<Tri> ExtractTriangles(RawMeshData mesh) {
      var list = new List<Tri>();
      bool hasNorm = mesh.Normals != null && mesh.Normals.Length == mesh.Vertices.Length;
      bool hasUv = mesh.Uv0 != null && mesh.Uv0.Length == mesh.Vertices.Length;

      for (int s = 0; s < mesh.SubMeshIndices.Length; s++) {
        int[] idx = mesh.SubMeshIndices[s];
        if (idx == null) continue;
        for (int i = 0; i + 2 < idx.Length; i += 3) {
          int i0 = idx[i], i1 = idx[i + 1], i2 = idx[i + 2];
          if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= mesh.Vertices.Length || i1 >= mesh.Vertices.Length || i2 >= mesh.Vertices.Length) continue;
          Float3 p0 = mesh.Vertices[i0], p1 = mesh.Vertices[i1], p2 = mesh.Vertices[i2];
          Float3 fn = MeshNormaliser.Normalize(MeshNormaliser.Cross(MeshNormaliser.Sub(p1, p0), MeshNormaliser.Sub(p2, p0)));
          list.Add(new Tri {
            A = new Vert { Pos = p0, Norm = hasNorm ? mesh.Normals[i0] : fn, Uv = hasUv ? mesh.Uv0[i0] : new Float2(0f, 0f) },
            B = new Vert { Pos = p1, Norm = hasNorm ? mesh.Normals[i1] : fn, Uv = hasUv ? mesh.Uv0[i1] : new Float2(0f, 0f) },
            C = new Vert { Pos = p2, Norm = hasNorm ? mesh.Normals[i2] : fn, Uv = hasUv ? mesh.Uv0[i2] : new Float2(0f, 0f) },
            SubMesh = s
          });
        }
      }
      return list;
    }

    private static WorkPiece ExtractWorkPiece(RawMeshData mesh) {
      var wp = new WorkPiece();
      wp.Tris.AddRange(ExtractTriangles(mesh));
      return wp;
    }

    private static void ComputeSlicePlane(WorkPiece piece, Random rng, bool axisAlignedFallback, out Float3 origin, out Float3 normal) {
      float dx = piece.Max.X - piece.Min.X;
      float dy = piece.Max.Y - piece.Min.Y;
      float dz = piece.Max.Z - piece.Min.Z;

      Float3 center = new Float3(
        (piece.Min.X + piece.Max.X) * 0.5f,
        (piece.Min.Y + piece.Max.Y) * 0.5f,
        (piece.Min.Z + piece.Max.Z) * 0.5f);

      // Pick dominant axis (with slight preference to split horizontal footprint as well as height)
      Float3 baseAxis;
      float span;
      if (dx >= dy && dx >= dz) { baseAxis = new Float3(1f, 0f, 0f); span = dx; }
      else if (dz >= dx && dz >= dy) { baseAxis = new Float3(0f, 0f, 1f); span = dz; }
      else { baseAxis = new Float3(0f, 1f, 0f); span = dy; }

      if (axisAlignedFallback) {
        normal = baseAxis;
        origin = center;
        return;
      }

      // Organic jitter (~+-22 deg) on normal and +-14% on cut offset
      float jx = ((float)rng.NextDouble() * 2f - 1f) * 0.38f;
      float jy = ((float)rng.NextDouble() * 2f - 1f) * 0.38f;
      float jz = ((float)rng.NextDouble() * 2f - 1f) * 0.38f;
      normal = MeshNormaliser.Normalize(new Float3(baseAxis.X + jx, baseAxis.Y + jy, baseAxis.Z + jz));

      float offset = ((float)rng.NextDouble() * 2f - 1f) * span * 0.14f;
      origin = new Float3(
        center.X + normal.X * offset,
        center.Y + normal.Y * offset,
        center.Z + normal.Z * offset);
    }

    private static void SlicePiece(
      WorkPiece src,
      Float3 planeOrigin,
      Float3 planeNormal,
      int interiorSubMesh,
      out WorkPiece posPiece,
      out WorkPiece negPiece) {
      posPiece = new WorkPiece();
      negPiece = new WorkPiece();

      var cutSegments = new List<(Float3 A, Float3 B)>();

      for (int i = 0; i < src.Tris.Count; i++) {
        Tri t = src.Tris[i];
        float da = MeshNormaliser.Dot(MeshNormaliser.Sub(t.A.Pos, planeOrigin), planeNormal);
        float db = MeshNormaliser.Dot(MeshNormaliser.Sub(t.B.Pos, planeOrigin), planeNormal);
        float dc = MeshNormaliser.Dot(MeshNormaliser.Sub(t.C.Pos, planeOrigin), planeNormal);

        bool pa = da >= -PlaneEps;
        bool pb = db >= -PlaneEps;
        bool pc = dc >= -PlaneEps;

        if (pa && pb && pc) {
          posPiece.Tris.Add(t);
          continue;
        }
        if (da <= PlaneEps && db <= PlaneEps && dc <= PlaneEps) {
          negPiece.Tris.Add(t);
          continue;
        }

        // Straddling triangle: arrange so lone vertex is first
        Vert v0, v1, v2;
        float d0, d1, d2;
        bool loneIsPos;

        if (pa != pb && pa != pc) {
          v0 = t.A; v1 = t.B; v2 = t.C; d0 = da; d1 = db; d2 = dc; loneIsPos = pa;
        } else if (pb != pa && pb != pc) {
          v0 = t.B; v1 = t.C; v2 = t.A; d0 = db; d1 = dc; d2 = da; loneIsPos = pb;
        } else {
          v0 = t.C; v1 = t.A; v2 = t.B; d0 = dc; d1 = da; d2 = db; loneIsPos = pc;
        }

        Vert i01 = LerpVert(v0, v1, SafeFraction(d0, d1));
        Vert i02 = LerpVert(v0, v2, SafeFraction(d0, d2));

        var loneTri = new Tri { A = v0, B = i01, C = i02, SubMesh = t.SubMesh };
        var quadTri1 = new Tri { A = i01, B = v1, C = v2, SubMesh = t.SubMesh };
        var quadTri2 = new Tri { A = i01, B = v2, C = i02, SubMesh = t.SubMesh };

        if (loneIsPos) {
          posPiece.Tris.Add(loneTri);
          negPiece.Tris.Add(quadTri1);
          negPiece.Tris.Add(quadTri2);
          cutSegments.Add((i01.Pos, i02.Pos));
        } else {
          negPiece.Tris.Add(loneTri);
          posPiece.Tris.Add(quadTri1);
          posPiece.Tris.Add(quadTri2);
          cutSegments.Add((i02.Pos, i01.Pos));
        }
      }

      // Cap the cross-section on both pieces using planar fan from cut centroid
      if (cutSegments.Count >= 3) {
        AddPlanarCaps(cutSegments, planeNormal, interiorSubMesh, posPiece, negPiece);
      }
    }

    private static void AddPlanarCaps(
      List<(Float3 A, Float3 B)> segments,
      Float3 planeNormal,
      int interiorSubMesh,
      WorkPiece posPiece,
      WorkPiece negPiece) {
      // Build orthonormal tangent basis on the cut plane for UVs
      Float3 refAxis = Math.Abs(planeNormal.Y) < 0.9f ? new Float3(0f, 1f, 0f) : new Float3(1f, 0f, 0f);
      Float3 tanU = MeshNormaliser.Normalize(MeshNormaliser.Cross(refAxis, planeNormal));
      Float3 tanV = MeshNormaliser.Normalize(MeshNormaliser.Cross(planeNormal, tanU));

      double cx = 0, cy = 0, cz = 0;
      for (int i = 0; i < segments.Count; i++) {
        cx += segments[i].A.X + segments[i].B.X;
        cy += segments[i].A.Y + segments[i].B.Y;
        cz += segments[i].A.Z + segments[i].B.Z;
      }
      double inv = 1.0 / (segments.Count * 2.0);
      Float3 centroid = new Float3((float)(cx * inv), (float)(cy * inv), (float)(cz * inv));

      Float3 negNormal = new Float3(-planeNormal.X, -planeNormal.Y, -planeNormal.Z);
      Float2 cUv = ProjectUv(centroid, tanU, tanV);

      for (int i = 0; i < segments.Count; i++) {
        Float3 a = segments[i].A;
        Float3 b = segments[i].B;
        Float3 ab = MeshNormaliser.Sub(b, a);
        Float3 ac = MeshNormaliser.Sub(centroid, a);
        Float3 triCross = MeshNormaliser.Cross(ab, ac);
        float areaSq = triCross.X * triCross.X + triCross.Y * triCross.Y + triCross.Z * triCross.Z;
        if (areaSq < 1e-8f) continue;

        Float2 aUv = ProjectUv(a, tanU, tanV);
        Float2 bUv = ProjectUv(b, tanU, tanV);

        // Positive piece cap faces -planeNormal; negative piece cap faces +planeNormal
        // In Unity left-handed CW winding, Cross(B-A, C-A) points along outward normal
        bool crossMatchesPlane = MeshNormaliser.Dot(triCross, planeNormal) >= 0f;

        Vert vcPos = new Vert { Pos = centroid, Norm = negNormal, Uv = cUv };
        Vert vaPos = new Vert { Pos = a, Norm = negNormal, Uv = aUv };
        Vert vbPos = new Vert { Pos = b, Norm = negNormal, Uv = bUv };

        Vert vcNeg = new Vert { Pos = centroid, Norm = planeNormal, Uv = cUv };
        Vert vaNeg = new Vert { Pos = a, Norm = planeNormal, Uv = aUv };
        Vert vbNeg = new Vert { Pos = b, Norm = planeNormal, Uv = bUv };

        if (crossMatchesPlane) {
          negPiece.Tris.Add(new Tri { A = vaNeg, B = vbNeg, C = vcNeg, SubMesh = interiorSubMesh });
          posPiece.Tris.Add(new Tri { A = vaPos, B = vcPos, C = vbPos, SubMesh = interiorSubMesh });
        } else {
          posPiece.Tris.Add(new Tri { A = vaPos, B = vbPos, C = vcPos, SubMesh = interiorSubMesh });
          negPiece.Tris.Add(new Tri { A = vaNeg, B = vcNeg, C = vbNeg, SubMesh = interiorSubMesh });
        }
      }
    }

    internal static Float2 ProjectUv(Float3 p, Float3 uAxis, Float3 vAxis) {
      return new Float2(MeshNormaliser.Dot(p, uAxis) * 0.25f, MeshNormaliser.Dot(p, vAxis) * 0.25f);
    }

    internal static float SafeFraction(float d0, float d1) {
      float denom = d0 - d1;
      if (Math.Abs(denom) < 1e-7f) return 0.5f;
      float t = d0 / denom;
      return Math.Max(0f, Math.Min(1f, t));
    }

    internal static Vert LerpVert(Vert a, Vert b, float t) {
      return new Vert {
        Pos = new Float3(a.Pos.X + (b.Pos.X - a.Pos.X) * t, a.Pos.Y + (b.Pos.Y - a.Pos.Y) * t, a.Pos.Z + (b.Pos.Z - a.Pos.Z) * t),
        Norm = MeshNormaliser.Normalize(new Float3(a.Norm.X + (b.Norm.X - a.Norm.X) * t, a.Norm.Y + (b.Norm.Y - a.Norm.Y) * t, a.Norm.Z + (b.Norm.Z - a.Norm.Z) * t)),
        Uv = new Float2(a.Uv.X + (b.Uv.X - a.Uv.X) * t, a.Uv.Y + (b.Uv.Y - a.Uv.Y) * t)
      };
    }

    private static FracturePieceData BuildFracturePiece(WorkPiece piece, string name, int origSubCount, int interiorSubMesh) {
      if (piece.Tris.Count == 0) return null;
      piece.RecomputeBounds();

      Float3 centroid = new Float3(
        (piece.Min.X + piece.Max.X) * 0.5f,
        (piece.Min.Y + piece.Max.Y) * 0.5f,
        (piece.Min.Z + piece.Max.Z) * 0.5f);

      int totalSubSlots = origSubCount + 1;
      var trisBySub = new List<int>[totalSubSlots];
      for (int s = 0; s < totalSubSlots; s++) trisBySub[s] = new List<int>();

      var verts = new List<Float3>(piece.Tris.Count * 3);
      var norms = new List<Float3>(piece.Tris.Count * 3);
      var uvs = new List<Float2>(piece.Tris.Count * 3);

      for (int i = 0; i < piece.Tris.Count; i++) {
        Tri t = piece.Tris[i];
        int slot = Math.Max(0, Math.Min(totalSubSlots - 1, t.SubMesh));
        int baseIdx = verts.Count;

        verts.Add(MeshNormaliser.Sub(t.A.Pos, centroid));
        verts.Add(MeshNormaliser.Sub(t.B.Pos, centroid));
        verts.Add(MeshNormaliser.Sub(t.C.Pos, centroid));

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

      // Keep all original submeshes (0..origSubCount-1) aligned 1:1 with source material indices,
      // and include interior rubble submesh at index origSubCount if any cap triangles exist.
      bool hasInterior = trisBySub[interiorSubMesh].Count > 0;
      int finalSubCount = hasInterior ? totalSubSlots : origSubCount;
      var subMeshes = new RawSubMesh[finalSubCount];
      var subIndices = new int[finalSubCount][];
      for (int s = 0; s < finalSubCount; s++) {
        int[] arr = trisBySub[s].ToArray();
        subIndices[s] = arr;
        subMeshes[s] = new RawSubMesh {
          Topology = 0,
          IndexCount = (uint)arr.Length
        };
      }

      var rawMesh = new RawMeshData {
        Name = name,
        VertexCount = verts.Count,
        IndexFormat = verts.Count > 65535 ? 1 : 0,
        Vertices = verts.ToArray(),
        Normals = norms.ToArray(),
        Uv0 = uvs.ToArray(),
        SubMeshes = subMeshes,
        SubMeshIndices = subIndices,
        OriginalIsReadable = true
      };
      MeshNormaliser.RecalculateTangentsIfMissing(rawMesh);
      MeshNormaliser.RecalculateBounds(rawMesh);

      float vol = piece.Volume;
      float mass = Math.Max(500f, Math.Min(12000f, vol * 25f));

      return new FracturePieceData {
        Name = name,
        Centroid = centroid,
        Mesh = rawMesh,
        LocalBounds = rawMesh.LocalAabb,
        EstimatedMass = mass,
        InteriorSubMeshIndex = hasInterior ? interiorSubMesh : -1
      };
    }
  }
}
