namespace MissionControl.Result {
  public class SetAllTeamsRelationshipResult : EncounterResult {
    public static string ENABLE_ALL_TEAMS_RELATIONSHIP = "ENABLE_ALL_TEAMS_RELATIONSHIP";
    public static string ALL_TEAMS_RELATIONSHIP = "ALL_TEAMS_RELATIONSHIP";

    public bool Enabled { get; set; }
    public string Relationship { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetAllTeamsRelationshipResult] Setting all teams to have relationship: {Relationship} Enabled: {Enabled}");
      MissionControl.Instance.SetGameLogicData(ENABLE_ALL_TEAMS_RELATIONSHIP, Enabled.ToString().ToLower());
      MissionControl.Instance.SetGameLogicData(ALL_TEAMS_RELATIONSHIP, Relationship);
    }
  }
}
