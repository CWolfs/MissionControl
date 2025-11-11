using UnityEngine;

using BattleTech;

namespace MissionControl.EncounterFactories {
  public static class BoundaryFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject escapeRegionGo = new GameObject(name ?? "EncounterBoundaryRect");
      escapeRegionGo.transform.parent = parent.transform;
      escapeRegionGo.transform.localPosition = Vector3.zero;

      return escapeRegionGo;
    }

    public static EncounterBoundaryRectGameLogic CreateEncounterBoundary(GameObject parent, string name, string guid, int width, int length) {
      GameObject boundaryGameObject = CreateGameObject(parent, name);

      EncounterBoundaryRectGameLogic boundaryRectGameLogic = boundaryGameObject.AddComponent<EncounterBoundaryRectGameLogic>();
      boundaryRectGameLogic.encounterObjectGuid = guid;
      boundaryRectGameLogic.width = width;
      boundaryRectGameLogic.height = length;

      return boundaryRectGameLogic;
    }
  }
}