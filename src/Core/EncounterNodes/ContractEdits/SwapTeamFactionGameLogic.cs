using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using HBS.Util;

using MissionControl.Data;
using MissionControl.LogicComponents;

namespace MissionControl.EncounterNodes.ContractEdits {
  /**
  * Ideally this would be a trigger, condition and result combination
  * - Enter region
  * - Send Message
  * - Condition is met
  * - Result runs which runs this class
  */
  public class SwapTeamFactionGameLogic : EncounterObjectGameLogic, ExecutableGameLogic {

    [SerializeField]
    public string team1Guid { get; set; } = "UNSET";

    [SerializeField]
    public string team2Guid { get; set; } = "UNSET";

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.SwapTeamFaction;
      }
    }

    private void SwapTeamFactions() {
      TeamUtils.SwapTeamFactions(MissionControl.Instance.CurrentContract, team1Guid, team2Guid);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<SwapTeamFactionGameLogic>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<SwapTeamFactionGameLogic>(new SwapTeamFactionGameLogic());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<SwapTeamFactionGameLogic>(this);
    }

    public void Execute() {
      SwapTeamFactions();
    }
  }
}
