using UnityEngine;

using BattleTech;

using HBS.Util;

using MissionControl.Data;

namespace MissionControl.LogicComponents.CombatStates {
  public class DisablePilotDeathGameLogic : EncounterObjectGameLogic {
    public static string DISABLE_PILOT_DEATH = "DISABLE_PILOT_DEATH";
    public static string DISABLE_PILOT_INJURY = "DISABLE_PILOT_INJURY";

    [SerializeField]
    public bool DisableInjuries { get; set; } = false;

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.DisablePilotDeath;
      }
    }

    public override void OnEnterActive() {
      base.OnEnterActive();
      Main.LogDebug($"[DisablePilotDeathGameLogic.OnEnterActive] OnEnterActive");
      MissionControl.Instance.SetGameLogicData(DISABLE_PILOT_DEATH, "true");
      if (DisableInjuries) MissionControl.Instance.SetGameLogicData(DISABLE_PILOT_INJURY, "true");
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<DisablePilotDeathGameLogic>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<DisablePilotDeathGameLogic>(new DisablePilotDeathGameLogic());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<DisablePilotDeathGameLogic>(this);
    }
  }
}
