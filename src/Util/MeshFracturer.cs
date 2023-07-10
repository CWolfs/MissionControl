using UnityEngine;

using System.Collections.Generic;

public class MeshFracturer {
  public static List<GameObject> Fracture(GameObject parentDestructibleGO, int pieces) {
    Mesh lod0Mesh = parentDestructibleGO.FindRecursive($"{parentDestructibleGO.name}_LOD0").GetComponent<MeshFilter>().sharedMesh;
    MissionControl.Main.Logger.Log("[Fracture] lod0Mesh " + lod0Mesh.name);

    List<GameObject> splitPieceGOs = new List<GameObject>();

    List<Mesh> meshPieces = Fracture(lod0Mesh, pieces);

    Material placeholderMaterial = new Material(Shader.Find("BattleTech Standard"));

    for (int i = 0; i < meshPieces.Count; i++) {
      Mesh mesh = meshPieces[i];
      GameObject splitPiece = new GameObject("split" + i);

      MeshFilter mf = splitPiece.AddComponent<MeshFilter>();
      mf.mesh = mesh;

      MeshRenderer mr = splitPiece.AddComponent<MeshRenderer>();
      Material[] materials = new Material[mesh.subMeshCount];
      for (int j = 0; j < mesh.subMeshCount; j++) {
        materials[j] = placeholderMaterial;
      }
      mr.sharedMaterials = materials;

      splitPiece.AddComponent<BoxCollider>();

      Rigidbody rb = splitPiece.AddComponent<Rigidbody>();
      rb.mass = 5000; // seems the main chunks are around this mass in vanilla

      DestructibleObject destructibleObject = splitPiece.AddComponent<DestructibleObject>();
      destructibleObject.dependentPersistentFX = new List<GameObject>();
      destructibleObject.embeddedFlimsyChildren = new List<DestructibleObject>();

      splitPiece.AddComponent<PhysicsExplodeChild>();

      splitPieceGOs.Add(splitPiece);
    }

    return splitPieceGOs;
  }

  public static List<Mesh> Fracture(Mesh mesh, int pieces) {
    List<Mesh> fracturedPieces = new List<Mesh>();

    if (pieces == 1) {
      Mesh copiedMesh = Mesh.Instantiate(mesh);
      fracturedPieces.Add(copiedMesh);
      return fracturedPieces;
    }

    Vector3[] vertices = mesh.vertices;
    int[] triangles = mesh.triangles;
    Vector2[] uvs = mesh.uv;

    // Each face of a mesh consists of 3 vertices forming a triangle
    int numberOfFaces = triangles.Length / 3;

    for (int i = 0; i < pieces; i++) {
      List<Vector3> newVertices = new List<Vector3>();
      List<int> newTriangles = new List<int>();
      List<Vector2> newUVs = new List<Vector2>();

      for (int j = i; j < numberOfFaces; j += pieces) {
        // Accessing each vertex of the face
        for (int k = 0; k < 3; k++) {
          int triangleIndex = j * 3 + k;

          newVertices.Add(vertices[triangles[triangleIndex]]);
          newTriangles.Add(newVertices.Count - 1);
          newUVs.Add(uvs[triangles[triangleIndex]]);
        }
      }

      Mesh newMesh = new Mesh();
      newMesh.vertices = newVertices.ToArray();
      newMesh.triangles = newTriangles.ToArray();
      newMesh.uv = newUVs.ToArray();
      newMesh.RecalculateNormals();
      fracturedPieces.Add(newMesh);
    }

    return fracturedPieces;
  }
}
