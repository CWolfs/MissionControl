using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace ContractCommand.Logic {
  public abstract class LanceLogic : LogicBlock {
    public LanceLogic() {
      this.Type = LogicType.CONTRACT_OVERRIDE_MANIPULATION;
    }
  }
}