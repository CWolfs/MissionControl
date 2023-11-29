using HBS.Collections;

using MissionControl.Data;

/**
This debug result will log out all tags for a scope if the key 'All' is used, or log out if a specific tag exists or not
*/
namespace MissionControl.Result {
  public class DebugTagLoggerResult : EncounterResult {
    public Scope Scope { get; set; }
    public string Key { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[DebugTagLoggerResult] Triggering");
      TagSet tags = TagUtils.GetTagSet(Scope.ToString());

      if (tags == null) {
        Main.Logger.LogWarning($"[DebugTagLoggerResult] TagSet with scope '{Scope}' could not be found. Maybe there is no active flashpoint or no current system if it's trying to get that.");
      }

      if (Key == "All") {
        foreach (string tag in tags) {
          Main.LogDebug($"[DebugTagLoggerResult][Scope: '{Scope}'] Tag: '{tag}' exists");
        }
      } else {
        if (tags.Contains(Key)) {
          Main.LogDebug($"[DebugTagLoggerResult][Scope: '{Scope}'] Tag: '{Key} exists'");
        } else {
          Main.LogDebug($"[DebugTagLoggerResult][Scope: '{Scope}'] Tag: '{Key} DOES NOT exist");
        }
      }
    }
  }
}
