using UnityEngine;

using MissionControl.LogicComponents.CombatStates;
using BattleTech;

namespace MissionControl.EncounterFactories {
  public class NavigationFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject((name == null) ? "Navigation" : name);
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;

      return go;
    }

    public static RouteGameLogic CreateRoute(GameObject parent, string name, string guid) {
      GameObject routeGameObject = CreateGameObject(parent, name);

      RouteGameLogic routeGameLogic = routeGameObject.AddComponent<RouteGameLogic>();
      routeGameLogic.encounterObjectGuid = guid;

      return routeGameLogic;
    }

    public static RoutePointGameLogic CreateRoutePoint(GameObject parent, string name, string guid) {
      GameObject routePointGameObject = CreateGameObject(parent, name);

      RoutePointGameLogic routePointGameLogic = routePointGameObject.AddComponent<RoutePointGameLogic>();
      routePointGameLogic.encounterObjectGuid = guid;

      return routePointGameLogic;
    }
  }
}