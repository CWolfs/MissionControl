using BattleTech;

using Harmony;

using System.Linq;

namespace MissionControl.Result {
  public class SetTeamShareVisionWithAlliesResult : EncounterResult {
    public string Team { get; set; }
    public bool ShareVision { get; set; } = true;

    public void BeforeSceneManipulation(MessageCenterMessage message) {
      Trigger(null, null);
    }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetTeamShareVisionWithAlliesResult] Setting Team '{Team}' to share vision with allies '{ShareVision}'");
      Team team = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(TeamUtils.GetTeamGuid(Team));

      // Visibility stuff might be a bit too early here

      /*
      team.VisibilityCache = null;
      AccessTools.Property(typeof(Team), "ShareVisionWithAlliance").SetValue(team, ShareVision);
      AccessTools.Method(typeof(Team), "InitVisibilityCaches").Invoke(team, null);

      foreach (Lance lance in team.lances) {
        AccessTools.Method(typeof(Lance), "InitVisibilityCache").Invoke(lance, null);
      }
      */

      /*
      Main.LogDebug($"[SetTeamShareVisionWithAlliesResult] Units in team are: '{team.units.Count}'");
      team.units.ForEach(unit => {
        Main.LogDebug($"[SetTeamShareVisionWithAlliesResult] Hiding unit '{unit.DisplayName}'");
        unit.OnPlayerVisibilityChanged(VisibilityLevel.None);
      });
      */
    }
  }
}
