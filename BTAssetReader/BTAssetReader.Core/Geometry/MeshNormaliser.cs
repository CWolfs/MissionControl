using System;
using System.Collections.Generic;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.Geometry {
  /// <summary>
  /// Pure-C# mesh normalisation shared identically between Unity (MissionControl/Designer)
  /// and Godot (Designer-Prop-Studio). Applies ExcludedParts -> RotationOffset -> TargetHeight/Scale -> PivotMode -> PositionOffset
  /// uniformly across LODs, colliders, shells, splits, and lights.
  /// </summary>
  public static class MeshNormaliser {
    public static void Normalize(CustomModelBundle bundle, MeshTransformSettings settings) {
      if (bundle == null || bundle.Lod0Mesh == null || settings == null) return;

      // 0. Strip excluded model parts BEFORE measuring bounds, height, or pivot so remaining parts re-centre cleanly
      if (settings.ExcludedParts != null && settings.ExcludedParts.Count > 0) {
        var excluded = new HashSet<string>(settings.ExcludedParts, StringComparer.OrdinalIgnoreCase);
        FilterExcludedParts(bundle.Lod0Mesh, excluded);
        FilterExcludedParts(bundle.Lod1Mesh, excluded);
        FilterExcludedParts(bundle.Lod2Mesh, excluded);
        FilterExcludedParts(bundle.ColMesh, excluded);
        FilterExcludedParts(bundle.GlassMesh, excluded);
        FilterExcludedParts(bundle.ShellMesh, excluded);
        FilterExcludedParts(bundle.ShellColMesh, excluded);
      }

      Float3 rotDeg = settings.RotationOffset;
      bool hasRot = Math.Abs(rotDeg.X) > 1e-4f || Math.Abs(rotDeg.Y) > 1e-4f || Math.Abs(rotDeg.Z) > 1e-4f;
      Quat rot = hasRot ? Quat.FromUnityEulerDegrees(rotDeg) : Quat.Identity;

      // 1. Measure rotated LOD0 bounds so TargetHeight & PivotMode match final orientation
      ComputeBoundsAfterRotation(bundle.Lod0Mesh, rot, hasRot, out Float3 rotMin, out Float3 rotMax);
      float rawHeight = rotMax.Y - rotMin.Y;

      float heightScale = (settings.TargetHeight > 0.001f && rawHeight > 1e-5f)
        ? settings.TargetHeight / rawHeight
        : 1.0f;

      Float3 scale = new Float3(
        (settings.Scale.X != 0f ? settings.Scale.X : 1f) * heightScale,
        (settings.Scale.Y != 0f ? settings.Scale.Y : 1f) * heightScale,
        (settings.Scale.Z != 0f ? settings.Scale.Z : 1f) * heightScale);

      // 2. Compute pivot shift on rotated+scaled LOD0 bounds
      Float3 sMin = new Float3(
        Math.Min(rotMin.X * scale.X, rotMax.X * scale.X),
        Math.Min(rotMin.Y * scale.Y, rotMax.Y * scale.Y),
        Math.Min(rotMin.Z * scale.Z, rotMax.Z * scale.Z));
      Float3 sMax = new Float3(
        Math.Max(rotMin.X * scale.X, rotMax.X * scale.X),
        Math.Max(rotMin.Y * scale.Y, rotMax.Y * scale.Y),
        Math.Max(rotMin.Z * scale.Z, rotMax.Z * scale.Z));

      Float3 pivotShift = new Float3(0f, 0f, 0f);
      if (settings.PivotMode == CorePivotMode.Center) {
        pivotShift = new Float3((sMin.X + sMax.X) * 0.5f, (sMin.Y + sMax.Y) * 0.5f, (sMin.Z + sMax.Z) * 0.5f);
      } else if (settings.PivotMode == CorePivotMode.Bottom) {
        pivotShift = new Float3((sMin.X + sMax.X) * 0.5f, sMin.Y, (sMin.Z + sMax.Z) * 0.5f);
      }

      Float3 posOffset = settings.PositionOffset;
      bool flipV = settings.FlipTextureV;

      // 3. Apply identical transform to every mesh slot
      ApplyTransform(bundle.Lod0Mesh, rot, hasRot, scale, pivotShift, posOffset, flipV);
      ApplyTransform(bundle.Lod1Mesh, rot, hasRot, scale, pivotShift, posOffset, flipV);
      ApplyTransform(bundle.Lod2Mesh, rot, hasRot, scale, pivotShift, posOffset, flipV);
      ApplyTransform(bundle.ColMesh, rot, hasRot, scale, pivotShift, posOffset, flipV);
      ApplyTransform(bundle.GlassMesh, rot, hasRot, scale, pivotShift, posOffset, flipV);
      ApplyTransform(bundle.ShellMesh, rot, hasRot, scale, pivotShift, posOffset, flipV);
      ApplyTransform(bundle.ShellColMesh, rot, hasRot, scale, pivotShift, posOffset, flipV);

      if (bundle.CustomSplits != null) {
        foreach (var piece in bundle.CustomSplits) {
          if (piece?.Mesh == null) continue;
          // Transform piece vertices in world space (centroid + vertex), then re-centre around new centroid
          Float3 c = piece.Centroid;
          for (int i = 0; i < piece.Mesh.Vertices.Length; i++) {
            Float3 v = piece.Mesh.Vertices[i];
            piece.Mesh.Vertices[i] = new Float3(v.X + c.X, v.Y + c.Y, v.Z + c.Z);
          }
          ApplyTransform(piece.Mesh, rot, hasRot, scale, pivotShift, posOffset, flipV);
          Float3 newC = piece.Mesh.LocalAabb.Center;
          for (int i = 0; i < piece.Mesh.Vertices.Length; i++) {
            Float3 v = piece.Mesh.Vertices[i];
            piece.Mesh.Vertices[i] = new Float3(v.X - newC.X, v.Y - newC.Y, v.Z - newC.Z);
          }
          RecalculateBounds(piece.Mesh);
          piece.Centroid = newC;
          piece.LocalBounds = piece.Mesh.LocalAabb;
        }
      }

      if (bundle.Lights != null) {
        float avgScale = (Math.Abs(scale.X) + Math.Abs(scale.Y) + Math.Abs(scale.Z)) / 3f;
        foreach (var light in bundle.Lights) {
          Float3 p = hasRot ? rot.Rotate(light.Position) : light.Position;
          light.Position = new Float3(
            p.X * scale.X - pivotShift.X + posOffset.X,
            p.Y * scale.Y - pivotShift.Y + posOffset.Y,
            p.Z * scale.Z - pivotShift.Z + posOffset.Z);
          if (hasRot) {
            light.Rotation = new Float3(
              light.Rotation.X + rotDeg.X,
              light.Rotation.Y + rotDeg.Y,
              light.Rotation.Z + rotDeg.Z);
          }
          if (avgScale > 0.01f && Math.Abs(avgScale - 1f) > 1e-3f) {
            light.Radius *= avgScale;
          }
        }
      }
    }

    private static void ComputeBoundsAfterRotation(RawMeshData mesh, Quat rot, bool hasRot, out Float3 min, out Float3 max) {
      min = new Float3(float.MaxValue, float.MaxValue, float.MaxValue);
      max = new Float3(float.MinValue, float.MinValue, float.MinValue);
      for (int i = 0; i < mesh.Vertices.Length; i++) {
        Float3 p = hasRot ? rot.Rotate(mesh.Vertices[i]) : mesh.Vertices[i];
        if (p.X < min.X) min.X = p.X;
        if (p.Y < min.Y) min.Y = p.Y;
        if (p.Z < min.Z) min.Z = p.Z;
        if (p.X > max.X) max.X = p.X;
        if (p.Y > max.Y) max.Y = p.Y;
        if (p.Z > max.Z) max.Z = p.Z;
      }
    }

    private static void ApplyTransform(RawMeshData mesh, Quat rot, bool hasRot, Float3 scale, Float3 pivotShift, Float3 posOffset, bool flipTextureV) {
      if (mesh == null || mesh.Vertices == null || mesh.Vertices.Length == 0) return;

      for (int i = 0; i < mesh.Vertices.Length; i++) {
        Float3 p = hasRot ? rot.Rotate(mesh.Vertices[i]) : mesh.Vertices[i];
        mesh.Vertices[i] = new Float3(
          p.X * scale.X - pivotShift.X + posOffset.X,
          p.Y * scale.Y - pivotShift.Y + posOffset.Y,
          p.Z * scale.Z - pivotShift.Z + posOffset.Z);
      }

      if (mesh.Normals != null && mesh.Normals.Length == mesh.Vertices.Length) {
        Float3 invScale = new Float3(
          Math.Abs(scale.X) > 1e-6f ? 1f / scale.X : 1f,
          Math.Abs(scale.Y) > 1e-6f ? 1f / scale.Y : 1f,
          Math.Abs(scale.Z) > 1e-6f ? 1f / scale.Z : 1f);
        for (int i = 0; i < mesh.Normals.Length; i++) {
          Float3 n = hasRot ? rot.Rotate(mesh.Normals[i]) : mesh.Normals[i];
          n = new Float3(n.X * invScale.X, n.Y * invScale.Y, n.Z * invScale.Z);
          mesh.Normals[i] = Normalize(n);
        }
      }

      if (mesh.Tangents != null && mesh.Tangents.Length == mesh.Vertices.Length) {
        float wSign = flipTextureV ? -1f : 1f;
        for (int i = 0; i < mesh.Tangents.Length; i++) {
          Float4 t = mesh.Tangents[i];
          Float3 td = new Float3(t.X, t.Y, t.Z);
          td = hasRot ? rot.Rotate(td) : td;
          td = Normalize(new Float3(td.X * scale.X, td.Y * scale.Y, td.Z * scale.Z));
          mesh.Tangents[i] = new Float4(td.X, td.Y, td.Z, t.W * wSign);
        }
      }

      if (flipTextureV && mesh.Uv0 != null) {
        for (int i = 0; i < mesh.Uv0.Length; i++) {
          Float2 u = mesh.Uv0[i];
          mesh.Uv0[i] = new Float2(u.X, 1f - u.Y);
        }
      }

      // If odd number of negative scales, flip triangle winding
      if (scale.X * scale.Y * scale.Z < 0f && mesh.SubMeshIndices != null) {
        for (int s = 0; s < mesh.SubMeshIndices.Length; s++) {
          int[] idx = mesh.SubMeshIndices[s];
          for (int i = 0; i + 2 < idx.Length; i += 3) {
            int tmp = idx[i + 1];
            idx[i + 1] = idx[i + 2];
            idx[i + 2] = tmp;
          }
        }
      }

      RecalculateBounds(mesh);
      RecalculatePartBounds(mesh);
    }

    public static void FilterExcludedParts(RawMeshData mesh, HashSet<string> excludedNames) {
      if (mesh == null || excludedNames == null || excludedNames.Count == 0) return;
      if (mesh.Parts == null || mesh.Parts.Count <= 1 || mesh.VertexPartIds == null || mesh.Vertices == null) return;
      if (mesh.VertexPartIds.Length != mesh.Vertices.Length || mesh.SubMeshIndices == null) return;

      int partCount = mesh.Parts.Count;
      var keepPart = new bool[partCount];
      int keptPartCount = 0;
      for (int p = 0; p < partCount; p++) {
        bool keep = !excludedNames.Contains(mesh.Parts[p].Name ?? string.Empty);
        keepPart[p] = keep;
        if (keep) keptPartCount++;
      }
      // Safety: if everything or nothing would be filtered, leave mesh unchanged
      if (keptPartCount == 0 || keptPartCount == partCount) return;

      int vCount = mesh.Vertices.Length;
      var remap = new int[vCount];
      for (int i = 0; i < vCount; i++) remap[i] = -1;

      var newSubIndices = new int[mesh.SubMeshIndices.Length][];
      var partRemap = new int[partCount];
      var newParts = new List<ModelPartInfo>(keptPartCount);
      for (int p = 0; p < partCount; p++) {
        if (keepPart[p]) {
          partRemap[p] = newParts.Count;
          newParts.Add(new ModelPartInfo { Name = mesh.Parts[p].Name, TriangleCount = 0 });
        } else {
          partRemap[p] = -1;
        }
      }

      int newVertCount = 0;
      int totalKeptTris = 0;
      for (int s = 0; s < mesh.SubMeshIndices.Length; s++) {
        int[] srcIdx = mesh.SubMeshIndices[s];
        if (srcIdx == null || srcIdx.Length < 3) {
          newSubIndices[s] = Array.Empty<int>();
          continue;
        }
        var kept = new List<int>(srcIdx.Length);
        for (int i = 0; i + 2 < srcIdx.Length; i += 3) {
          int a = srcIdx[i], b = srcIdx[i + 1], c = srcIdx[i + 2];
          if ((uint)a >= (uint)vCount || (uint)b >= (uint)vCount || (uint)c >= (uint)vCount) continue;
          int pid = mesh.VertexPartIds[a];
          if ((uint)pid < (uint)partCount && !keepPart[pid]) continue;

          if (remap[a] < 0) remap[a] = newVertCount++;
          if (remap[b] < 0) remap[b] = newVertCount++;
          if (remap[c] < 0) remap[c] = newVertCount++;
          kept.Add(remap[a]);
          kept.Add(remap[b]);
          kept.Add(remap[c]);
          totalKeptTris++;
          if ((uint)pid < (uint)partCount && partRemap[pid] >= 0) {
            newParts[partRemap[pid]].TriangleCount++;
          }
        }
        newSubIndices[s] = kept.ToArray();
      }

      if (totalKeptTris == 0 || newVertCount == 0) return;

      var newVerts = new Float3[newVertCount];
      var newNorms = mesh.Normals != null && mesh.Normals.Length == vCount ? new Float3[newVertCount] : null;
      var newTangs = mesh.Tangents != null && mesh.Tangents.Length == vCount ? new Float4[newVertCount] : null;
      var newCols = mesh.Colors != null && mesh.Colors.Length == vCount ? new ColorRgba32[newVertCount] : null;
      var newUv0 = mesh.Uv0 != null && mesh.Uv0.Length == vCount ? new Float2[newVertCount] : null;
      var newUv1 = mesh.Uv1 != null && mesh.Uv1.Length == vCount ? new Float2[newVertCount] : null;
      var newUv2 = mesh.Uv2 != null && mesh.Uv2.Length == vCount ? new Float2[newVertCount] : null;
      var newPartIds = new int[newVertCount];

      for (int i = 0; i < vCount; i++) {
        int ni = remap[i];
        if (ni < 0) continue;
        newVerts[ni] = mesh.Vertices[i];
        if (newNorms != null) newNorms[ni] = mesh.Normals[i];
        if (newTangs != null) newTangs[ni] = mesh.Tangents[i];
        if (newCols != null) newCols[ni] = mesh.Colors[i];
        if (newUv0 != null) newUv0[ni] = mesh.Uv0[i];
        if (newUv1 != null) newUv1[ni] = mesh.Uv1[i];
        if (newUv2 != null) newUv2[ni] = mesh.Uv2[i];
        int oldPid = mesh.VertexPartIds[i];
        newPartIds[ni] = (uint)oldPid < (uint)partCount ? partRemap[oldPid] : -1;
      }

      mesh.Vertices = newVerts;
      mesh.Normals = newNorms;
      mesh.Tangents = newTangs;
      mesh.Colors = newCols;
      mesh.Uv0 = newUv0;
      mesh.Uv1 = newUv1;
      mesh.Uv2 = newUv2;
      mesh.VertexPartIds = newPartIds;
      mesh.Parts = newParts;
      mesh.VertexCount = newVertCount;
      mesh.IndexFormat = newVertCount > 65535 ? 1 : 0;
      mesh.SubMeshIndices = newSubIndices;
      if (mesh.SubMeshes != null && mesh.SubMeshes.Length == newSubIndices.Length) {
        for (int s = 0; s < newSubIndices.Length; s++) {
          mesh.SubMeshes[s].IndexCount = (uint)newSubIndices[s].Length;
        }
      }

      RecalculateBounds(mesh);
      RecalculatePartBounds(mesh);
    }

    public static void RecalculatePartBounds(RawMeshData mesh) {
      if (mesh == null || mesh.Parts == null || mesh.Parts.Count == 0 || mesh.VertexPartIds == null || mesh.Vertices == null) return;
      if (mesh.VertexPartIds.Length != mesh.Vertices.Length) return;

      int pc = mesh.Parts.Count;
      var mins = new Float3[pc];
      var maxs = new Float3[pc];
      var hasAny = new bool[pc];
      for (int p = 0; p < pc; p++) {
        mins[p] = new Float3(float.MaxValue, float.MaxValue, float.MaxValue);
        maxs[p] = new Float3(float.MinValue, float.MinValue, float.MinValue);
      }

      for (int i = 0; i < mesh.Vertices.Length; i++) {
        int pid = mesh.VertexPartIds[i];
        if ((uint)pid >= (uint)pc) continue;
        Float3 v = mesh.Vertices[i];
        if (v.X < mins[pid].X) mins[pid].X = v.X;
        if (v.Y < mins[pid].Y) mins[pid].Y = v.Y;
        if (v.Z < mins[pid].Z) mins[pid].Z = v.Z;
        if (v.X > maxs[pid].X) maxs[pid].X = v.X;
        if (v.Y > maxs[pid].Y) maxs[pid].Y = v.Y;
        if (v.Z > maxs[pid].Z) maxs[pid].Z = v.Z;
        hasAny[pid] = true;
      }

      for (int p = 0; p < pc; p++) {
        if (!hasAny[p]) continue;
        mesh.Parts[p].LocalBounds = new RawBounds {
          Center = new Float3((mins[p].X + maxs[p].X) * 0.5f, (mins[p].Y + maxs[p].Y) * 0.5f, (mins[p].Z + maxs[p].Z) * 0.5f),
          Extents = new Float3((maxs[p].X - mins[p].X) * 0.5f, (maxs[p].Y - mins[p].Y) * 0.5f, (maxs[p].Z - mins[p].Z) * 0.5f)
        };
      }
    }

    public static void RecalculateBounds(RawMeshData mesh) {
      if (mesh == null || mesh.Vertices == null || mesh.Vertices.Length == 0) return;
      Float3 min = new Float3(float.MaxValue, float.MaxValue, float.MaxValue);
      Float3 max = new Float3(float.MinValue, float.MinValue, float.MinValue);
      for (int i = 0; i < mesh.Vertices.Length; i++) {
        Float3 v = mesh.Vertices[i];
        if (v.X < min.X) min.X = v.X;
        if (v.Y < min.Y) min.Y = v.Y;
        if (v.Z < min.Z) min.Z = v.Z;
        if (v.X > max.X) max.X = v.X;
        if (v.Y > max.Y) max.Y = v.Y;
        if (v.Z > max.Z) max.Z = v.Z;
      }
      mesh.LocalAabb = new RawBounds {
        Center = new Float3((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f, (min.Z + max.Z) * 0.5f),
        Extents = new Float3((max.X - min.X) * 0.5f, (max.Y - min.Y) * 0.5f, (max.Z - min.Z) * 0.5f)
      };
    }

    public static void RecalculateNormalsIfMissing(RawMeshData mesh) {
      if (mesh == null || mesh.Vertices == null || mesh.Vertices.Length == 0) return;
      if (mesh.Normals != null && mesh.Normals.Length == mesh.Vertices.Length) return;

      var normals = new Float3[mesh.Vertices.Length];
      if (mesh.SubMeshIndices != null) {
        for (int s = 0; s < mesh.SubMeshIndices.Length; s++) {
          int[] idx = mesh.SubMeshIndices[s];
          for (int i = 0; i + 2 < idx.Length; i += 3) {
            int i0 = idx[i], i1 = idx[i + 1], i2 = idx[i + 2];
            if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= mesh.Vertices.Length || i1 >= mesh.Vertices.Length || i2 >= mesh.Vertices.Length) continue;
            Float3 a = mesh.Vertices[i0], b = mesh.Vertices[i1], c = mesh.Vertices[i2];
            Float3 e1 = new Float3(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            Float3 e2 = new Float3(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            Float3 fn = Cross(e1, e2);
            normals[i0] = Add(normals[i0], fn);
            normals[i1] = Add(normals[i1], fn);
            normals[i2] = Add(normals[i2], fn);
          }
        }
      }

      for (int i = 0; i < normals.Length; i++) {
        normals[i] = Normalize(normals[i]);
      }
      mesh.Normals = normals;
    }

    public static void RecalculateTangentsIfMissing(RawMeshData mesh) {
      if (mesh == null || mesh.Vertices == null || mesh.Vertices.Length == 0 || mesh.SubMeshIndices == null) return;
      if (mesh.Tangents != null && mesh.Tangents.Length == mesh.Vertices.Length) return;

      RecalculateNormalsIfMissing(mesh);
      int vCount = mesh.Vertices.Length;
      var tan1 = new Float3[vCount];
      var tan2 = new Float3[vCount];
      bool hasUv = mesh.Uv0 != null && mesh.Uv0.Length == vCount;

      if (hasUv) {
        for (int s = 0; s < mesh.SubMeshIndices.Length; s++) {
          int[] idx = mesh.SubMeshIndices[s];
          if (idx == null) continue;
          for (int i = 0; i + 2 < idx.Length; i += 3) {
            int i0 = idx[i], i1 = idx[i + 1], i2 = idx[i + 2];
            if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= vCount || i1 >= vCount || i2 >= vCount) continue;

            Float3 v0 = mesh.Vertices[i0], v1 = mesh.Vertices[i1], v2 = mesh.Vertices[i2];
            Float2 w0 = mesh.Uv0[i0], w1 = mesh.Uv0[i1], w2 = mesh.Uv0[i2];

            float x1 = v1.X - v0.X, x2 = v2.X - v0.X;
            float y1 = v1.Y - v0.Y, y2 = v2.Y - v0.Y;
            float z1 = v1.Z - v0.Z, z2 = v2.Z - v0.Z;

            float s1 = w1.X - w0.X, s2 = w2.X - w0.X;
            float t1 = w1.Y - w0.Y, t2 = w2.Y - w0.Y;

            float denom = s1 * t2 - s2 * t1;
            if (Math.Abs(denom) < 1e-8f) continue;
            float r = 1.0f / denom;

            Float3 sdir = new Float3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Float3 tdir = new Float3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i0] = Add(tan1[i0], sdir); tan1[i1] = Add(tan1[i1], sdir); tan1[i2] = Add(tan1[i2], sdir);
            tan2[i0] = Add(tan2[i0], tdir); tan2[i1] = Add(tan2[i1], tdir); tan2[i2] = Add(tan2[i2], tdir);
          }
        }
      }

      var tangents = new Float4[vCount];
      for (int i = 0; i < vCount; i++) {
        Float3 n = mesh.Normals[i];
        Float3 t = tan1[i];
        // Gram-Schmidt orthogonalize
        float ndott = Dot(n, t);
        Float3 ortho = new Float3(t.X - n.X * ndott, t.Y - n.Y * ndott, t.Z - n.Z * ndott);
        float lenSq = ortho.X * ortho.X + ortho.Y * ortho.Y + ortho.Z * ortho.Z;
        if (lenSq < 1e-8f) {
          Float3 refAxis = Math.Abs(n.Y) < 0.9f ? new Float3(0f, 1f, 0f) : new Float3(1f, 0f, 0f);
          ortho = Normalize(Cross(refAxis, n));
          tangents[i] = new Float4(ortho.X, ortho.Y, ortho.Z, 1f);
        } else {
          float invLen = 1f / (float)Math.Sqrt(lenSq);
          ortho = new Float3(ortho.X * invLen, ortho.Y * invLen, ortho.Z * invLen);
          float w = Dot(Cross(n, t), tan2[i]) < 0f ? -1f : 1f;
          tangents[i] = new Float4(ortho.X, ortho.Y, ortho.Z, w);
        }
      }
      mesh.Tangents = tangents;
    }

    public static RawMeshData CreateBoxMesh(RawBounds bounds, string name) {
      float cx = bounds.Center.X, cy = bounds.Center.Y, cz = bounds.Center.Z;
      float ex = Math.Max(0.05f, bounds.Extents.X);
      float ey = Math.Max(0.05f, bounds.Extents.Y);
      float ez = Math.Max(0.05f, bounds.Extents.Z);

      Float3[] v = new[] {
        new Float3(cx - ex, cy - ey, cz - ez),
        new Float3(cx + ex, cy - ey, cz - ez),
        new Float3(cx + ex, cy + ey, cz - ez),
        new Float3(cx - ex, cy + ey, cz - ez),
        new Float3(cx - ex, cy - ey, cz + ez),
        new Float3(cx + ex, cy - ey, cz + ez),
        new Float3(cx + ex, cy + ey, cz + ez),
        new Float3(cx - ex, cy + ey, cz + ez)
      };

      // Unity left-handed outward CW triangles
      int[] tris = new[] {
        0, 2, 1, 0, 3, 2, // -Z
        5, 7, 4, 5, 6, 7, // +Z
        4, 3, 0, 4, 7, 3, // -X
        1, 6, 5, 1, 2, 6, // +X
        3, 6, 2, 3, 7, 6, // +Y
        4, 1, 5, 4, 0, 1  // -Y
      };

      var mesh = new RawMeshData {
        Name = name,
        VertexCount = v.Length,
        IndexFormat = 0,
        Vertices = v,
        SubMeshes = new[] { new RawSubMesh { Topology = 0, IndexCount = (uint)tris.Length } },
        SubMeshIndices = new[] { tris },
        LocalAabb = bounds,
        OriginalIsReadable = true
      };
      RecalculateNormalsIfMissing(mesh);
      return mesh;
    }

    internal static Float3 Add(Float3 a, Float3 b) => new Float3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    internal static Float3 Sub(Float3 a, Float3 b) => new Float3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    internal static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    internal static Float3 Cross(Float3 a, Float3 b) => new Float3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
    internal static Float3 Normalize(Float3 v) {
      float len = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
      return len > 1e-6f ? new Float3(v.X / len, v.Y / len, v.Z / len) : new Float3(0f, 1f, 0f);
    }

    internal readonly struct Quat {
      public readonly float X, Y, Z, W;
      public static readonly Quat Identity = new Quat(0f, 0f, 0f, 1f);
      public Quat(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

      /// <summary>
      /// Unity Euler order: Z then X then Y (q = qY * qX * qZ).
      /// </summary>
      public static Quat FromUnityEulerDegrees(Float3 deg) {
        const float d2r = (float)(Math.PI / 180.0);
        float hx = deg.X * d2r * 0.5f;
        float hy = deg.Y * d2r * 0.5f;
        float hz = deg.Z * d2r * 0.5f;

        float sx = (float)Math.Sin(hx), cx = (float)Math.Cos(hx);
        float sy = (float)Math.Sin(hy), cy = (float)Math.Cos(hy);
        float sz = (float)Math.Sin(hz), cz = (float)Math.Cos(hz);

        // qY * qX * qZ
        return new Quat(
          cy * sx * cz + sy * cx * sz,
          sy * cx * cz - cy * sx * sz,
          cy * cx * sz - sy * sx * cz,
          cy * cx * cz + sy * sx * sz);
      }

      public Float3 Rotate(Float3 v) {
        float tx = 2f * (Y * v.Z - Z * v.Y);
        float ty = 2f * (Z * v.X - X * v.Z);
        float tz = 2f * (X * v.Y - Y * v.X);
        return new Float3(
          v.X + W * tx + (Y * tz - Z * ty),
          v.Y + W * ty + (Z * tx - X * tz),
          v.Z + W * tz + (X * ty - Y * tx));
      }
    }
  }
}
