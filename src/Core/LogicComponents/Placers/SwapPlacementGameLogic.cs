using UnityEngine;

using BattleTech;
using HBS.Util;

using MissionControl.Data;

namespace MissionControl.LogicComponents.Placers {
  public class SwapPlacementGameLogic : EncounterObjectGameLogic {

    [SerializeField]
    public string swapTarget1Guid { get; set; } = "UNSET";

    [SerializeField]
    public string swapTarget2Guid { get; set; } = "UNSET";

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.SwapPlacement;
      }
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<SwapPlacementGameLogic>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<SwapPlacementGameLogic>(new SwapPlacementGameLogic());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<SwapPlacementGameLogic>(this);
    }
  }
}
