using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Logic {
  public abstract class ObjectiveLogic : LogicBlock {

    public ObjectiveLogic() {
      this.Type = LogicType.ENCOUNTER_MANIPULATION;
    }
  }
}