using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using MissionControl.EncounterNodes.Dropship;
using MissionControl.Data;

using System.Collections.Generic;

namespace MissionControl.EncounterFactories {
  public class DropshipNodeFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject((name == null) ? "Dropship" : name);
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;

      return go;
    }

    public static CustomDropshipLandingSpotGameLogic CreateDropshipLandingSpot(GameObject parent, string name, string guid, StartingDropshipAnimationState dropshipStates, CustomDropshipType useDropshipType, bool automarkLandingZone, string teamGUID, List<DropshipRef> dropshipRefs) {
      GameObject dropshipLandingSpoGameObject = CreateGameObject(parent, name);

      CustomDropshipLandingSpotGameLogic dropshipLandingSpotGameLogic = dropshipLandingSpoGameObject.AddComponent<CustomDropshipLandingSpotGameLogic>();
      dropshipLandingSpotGameLogic.encounterObjectGuid = guid;

      return dropshipLandingSpotGameLogic;
    }
  }
}