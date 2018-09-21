using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Logic {
  public class LogicBlock {
    public enum LogicType { NONE, RESOURCE_REQUEST, ENCOUNTER_MANIPULATION, SCENE_MANIPULATION }

    public LogicType Type { get; protected set; } = LogicType.NONE;
  }
}