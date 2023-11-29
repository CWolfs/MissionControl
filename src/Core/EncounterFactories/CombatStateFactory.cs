using UnityEngine;

using MissionControl.EncounterNodes.CombatStates;

namespace MissionControl.EncounterFactories {
  public class CombatStateFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject((name == null) ? "CombatState" : name);
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;

      return go;
    }

    public static DisablePilotDeathGameLogic CreateDisablePilotDeath(GameObject parent, string name, string guid, bool disableInjuries) {
      GameObject disablePilotDeathGameObject = CreateGameObject(parent, name);

      DisablePilotDeathGameLogic disablePilotDeathGameLogic = disablePilotDeathGameObject.AddComponent<DisablePilotDeathGameLogic>();
      disablePilotDeathGameLogic.encounterObjectGuid = guid;
      disablePilotDeathGameLogic.DisableInjuries = disableInjuries;

      return disablePilotDeathGameLogic;
    }
  }
}