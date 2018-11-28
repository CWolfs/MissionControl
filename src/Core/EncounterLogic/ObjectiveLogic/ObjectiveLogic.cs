using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Logic {
  public abstract class ObjectiveLogic : LogicBlock {
    public ObjectiveLogic() {
      this.Type = LogicType.CONTRACT_OVERRIDE_MANIPULATION;
    }
  }
}