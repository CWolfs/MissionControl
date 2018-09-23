using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace EncounterCommand.Logic {
  public abstract class ObjectiveLogic : LogicBlock {
    public struct ProgressFormat {
      public static string NUMBER_OF_UNITS_TO_DEFEND_REMAINING = "[numberOfUnitsToDefendRemaining]";
      public static string NUMBER_OF_UNITS_TO_DEFEND = "[numberOfUnitsToDefend]";
      public static string DURATION_REMAINING = "[durationRemaining]";
      public static string DURATION_TO_OCCUPY = "[durationToOccupy]";
      public static string PERCENTAGE_COMPLETE = "[percentageComplete]";
      public static string PLURAL_DURATION_TYPE = "[pluralDurationType]";  // Round(s) or Phase(s)
    }

    public ObjectiveLogic() {
      this.Type = LogicType.ENCOUNTER_MANIPULATION;
    }
  }
}