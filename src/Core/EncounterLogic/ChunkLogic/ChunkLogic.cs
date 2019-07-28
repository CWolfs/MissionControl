using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public abstract class ChunkLogic : LogicBlock {
    public struct ProgressFormat {
      public static string NUMBER_OF_UNITS_TO_DEFEND_REMAINING = "[numberOfUnitsToDefendRemaining]";
      public static string NUMBER_OF_UNITS_TO_DEFEND = "[numberOfUnitsToDefend]";
      public static string DURATION_REMAINING = "[durationRemaining]";
      public static string DURATION_TO_OCCUPY = "[durationToOccupy]";
      public static string PERCENTAGE_COMPLETE = "[percentageComplete]";
      public static string PLURAL_DURATION_TYPE = "[pluralDurationType]";  // Round(s) or Phase(s)
      public static string UNITS_OCCUPYING_SO_FAR = "[unitsOccupyingSoFar]";
      public static string NUMBER_OF_UNITS_TO_OCCUPY = "[numberOfUnitsToOccupy]";
    }

    public static string DIALOGUE_ADDITIONAL_LANCE_ALLY_START_GUID = "47647e3c-a82d-4946-a601-b7dddbb63452";

    public static string PLAYER_SPAWN_GUID = "";

    public ChunkLogic() {
      this.Type = LogicType.ENCOUNTER_MANIPULATION;
    }

    public string GetPlayerSpawnGuid() {
      GameObject encounterLayerGo = MissionControl.Instance.EncounterLayerGameObject;
      GameObject chunkPlayerLanceGo = encounterLayerGo.transform.Find(EncounterRules.GetPlayerLanceChunkName()).gameObject;
      GameObject spawnerPlayerLanceGo = chunkPlayerLanceGo.transform.Find(EncounterRules.GetPlayerLanceSpawnerName()).gameObject;
      return spawnerPlayerLanceGo.GetComponent<LanceSpawnerGameLogic>().encounterObjectGuid;
    }
  }
}