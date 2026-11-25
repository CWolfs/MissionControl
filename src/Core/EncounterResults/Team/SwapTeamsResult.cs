using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Result {
  public class SwapTeamsResult : EncounterResult {
    public string Team1GUID { get; set; }
    public string Team2GUID { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      TeamUtils.SwapTeamFactions(MissionControl.Instance.CurrentContract, Team1GUID, Team2GUID);
    }
  }
}
