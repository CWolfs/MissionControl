using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceAtEdgeOfBoundary : SpawnLogic {
    public SpawnLanceAtEdgeOfBoundary(GameObject lanceGameObject) : base() {
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] For {lanceGameObject.name}");
      SpawnManager spawnManager = SpawnManager.GetInstance();
      GameObject chunkBoundaryRect = spawnManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = boundaryLogic.GetRect();
      float width = boundaryRec.width;
      float x = UnityEngine.Random.Range(-width / 2f, width / 2f) + boundary.transform.position.x;
      float z = UnityEngine.Random.Range(-width / 2f, width / 2f) + boundary.transform.position.z;

      Vector3 lancePosition = lanceGameObject.transform.position;
      lanceGameObject.transform.position = new Vector3(x, lancePosition.y, z);
    }
  }
}