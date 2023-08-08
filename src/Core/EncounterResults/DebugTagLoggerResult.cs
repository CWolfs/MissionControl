using HBS.Collections;

using MissionControl.Data;

/**
This debug result will log out all tags for a scope if the key 'All' is used, or log out if a specific tag exists or not
*/
namespace MissionControl.Result {
  public class DebugTagLogger : EncounterResult {
    public Scope Scope { get; set; }
    public string Key { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[DebugTagLogger] Triggering");
      TagSet tags = TagUtils.GetTagSet(Scope.ToString());

      if (tags == null) {
        Main.Logger.LogWarning($"[DebugTagLogger] TagSet with scope '{Scope}' could not be found. Maybe there is no active flashpoint or no current system if it's trying to get that.");
      }

      if (Key == "All") {
        foreach (string tag in tags) {
          Main.LogDebug($"[DebugTagLogger][Scope: '{Scope}'] Tag: '{tag}' exists");
        }
      } else {
        if (tags.Contains(Key)) {
          Main.LogDebug($"[DebugTagLogger][Scope: '{Scope}'] Tag: '{Key} exists'");
        } else {
          Main.LogDebug($"[DebugTagLogger][Scope: '{Scope}'] Tag: '{Key} DOES NOT exist");
        }
      }
    }
  }
}
