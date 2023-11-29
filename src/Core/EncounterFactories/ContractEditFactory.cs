using UnityEngine;

using MissionControl.EncounterNodes.ContractEdits;

namespace MissionControl.EncounterFactories {
  public class ContractEditFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject((name == null) ? "ContractEdit" : name);
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;

      return go;
    }

    public static SwapTeamFactionGameLogic CreateSwapFaction(GameObject parent, string name, string guid, string teamGuid1, string teamGuid2) {
      GameObject spawnSwapGameObject = CreateGameObject(parent, name);

      SwapTeamFactionGameLogic swapTeamFactionGameLogic = spawnSwapGameObject.AddComponent<SwapTeamFactionGameLogic>();
      swapTeamFactionGameLogic.encounterObjectGuid = guid;
      swapTeamFactionGameLogic.team1Guid = teamGuid1;
      swapTeamFactionGameLogic.team2Guid = teamGuid2;

      return swapTeamFactionGameLogic;
    }
  }
}