using UnityEngine;

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

    public static string PLAYER1_LANCE_SPAWNER_GUID = "76b654a6-4f2c-4a6f-86e6-d4cf868335fe";

    public static string DIALOGUE_ADDITIONAL_LANCE_ALLY_START_GUID = "47647e3c-a82d-4946-a601-b7dddbb63452";
    public static string DIALOGUE_DYNAMIC_WITHDRAW_ESCAPE_GUID = "a6eae1af-7fd1-41a4-a0cc-e2deca2cf2a1";

    public static string DYNAMIC_WITHDRAW_CHUNK_GUID = "3e8be988-22c1-4d65-b0a7-3228d0f495a8";
    public static string DYNAMIC_WITHDRAW_OBJECTIVE_GUID = "65f04a39-fc57-476c-bae4-6958f8062c3f";
    public static string DYNAMIC_WITHDRAW_REGION_GUID = "bb9bef5d-aef1-4f83-9b9c-7ce5ce55bf63";

    public ChunkLogic() {
      this.Type = LogicType.ENCOUNTER_MANIPULATION;
    }

    public GameObject GetPlayerSpawn() {
      GameObject encounterLayerGo = MissionControl.Instance.EncounterLayerGameObject;
      GameObject chunkPlayerLanceGo = EncounterRules.GetPlayerLanceChunkGameObject(encounterLayerGo);
      GameObject spawnerPlayerLanceGo = EncounterRules.GetPlayerSpawnerGameObject(chunkPlayerLanceGo);
      return spawnerPlayerLanceGo;
    }

    public string GetPlayerSpawnGuid() {
      GameObject spawnerPlayerLanceGo = GetPlayerSpawn();
      return spawnerPlayerLanceGo.GetComponent<LanceSpawnerGameLogic>().encounterObjectGuid;
    }
  }
}