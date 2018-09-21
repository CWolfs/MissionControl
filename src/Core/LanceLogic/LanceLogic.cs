using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Logic {
  public abstract class LanceLogic : LogicBlock {

    public LanceLogic() {
      this.Type = LogicType.CONTRACT_OVERRIDE_MANIPULATION;
    }
  }
}