using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using MissionControl.EncounterNodes.Dropship;
using MissionControl.Data;
using MissionControl.Data.Refs;

using System.Collections.Generic;
using HBS.Collections;

namespace MissionControl.EncounterFactories {
  public class DropshipNodeFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject((name == null) ? "Dropship" : name);
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;

      return go;
    }

    public static CustomDropshipLandingSpotGameLogic CreateDropshipLandingSpot(GameObject parent, string name, string guid, StartingDropshipAnimationState dropshipStates, CustomDropshipType useDropshipType, bool automarkLandingZone, string teamGUID, List<string> dropshipTags) {
      GameObject dropshipLandingSpoGameObject = CreateGameObject(parent, name);

      CustomDropshipLandingSpotGameLogic dropshipLandingSpotGameLogic = dropshipLandingSpoGameObject.AddComponent<CustomDropshipLandingSpotGameLogic>();
      dropshipLandingSpotGameLogic.encounterObjectGuid = guid;

      dropshipLandingSpotGameLogic.DropshipStates = dropshipStates;
      dropshipLandingSpotGameLogic.UseDropshipType = useDropshipType;
      dropshipLandingSpotGameLogic.AutoMarkDropshipLandingZone = automarkLandingZone;
      dropshipLandingSpotGameLogic.TeamGUID = teamGUID;
      dropshipLandingSpotGameLogic.DropshipTags = dropshipTags;

      return dropshipLandingSpotGameLogic;
    }

    public static CustomDropshipExtractionGameLogic CreateDropshipExtraction(GameObject parent, string name, string guid, CustomDropshipLandingSpotRef dropshipLandingSpotRef, ObjectiveRef callDropshipObjectiveRef, ObjectiveRef loadDropshipObjectiveRef, List<string> requiredTagsOnLance, bool spawnLancesWhenLanded, bool takeOffImmediately, bool extractViaDropship, bool showEscapeMessage) {
      GameObject dropshipExtractionGameObject = CreateGameObject(parent, name);

      CustomDropshipExtractionGameLogic dropshipExtractionGameLogic = dropshipExtractionGameObject.AddComponent<CustomDropshipExtractionGameLogic>();
      dropshipExtractionGameLogic.encounterObjectGuid = guid;

      dropshipExtractionGameLogic.DropshipLandingSpotRef = dropshipLandingSpotRef;
      dropshipExtractionGameLogic.CallDropshipObjectiveRef = callDropshipObjectiveRef;
      dropshipExtractionGameLogic.LoadDropshipObjectiveRef = loadDropshipObjectiveRef;
      dropshipExtractionGameLogic.RequiredTagsOnLance = new TagSet(requiredTagsOnLance);
      dropshipExtractionGameLogic.SpawnLancesWhenLanded = spawnLancesWhenLanded;
      dropshipExtractionGameLogic.TakeOffImmediately = takeOffImmediately;
      dropshipExtractionGameLogic.ExtractViaDropship = extractViaDropship;
      dropshipExtractionGameLogic.ShowEscapeMessage = showEscapeMessage;

      return dropshipExtractionGameLogic;
    }
  }
}