using HBS.Collections;

using MissionControl.Data;

/**
This result will set or remove a tag in a scope
*/
namespace MissionControl.Result {
  public class SetTagResult : EncounterResult {
    public Scope Scope { get; set; }
    public TagOperation Operation { get; set; }
    public string Tag { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetStatResult] Triggering for Scope '{Scope}' Operation '{Operation}' Tag '{Tag}'");
      TagSet tags = TagUtils.GetTagSet(Scope);

      switch (Operation) {
        case TagOperation.Add: tags.Add(Tag); break;
        case TagOperation.Remove: tags.Remove(Tag); break;
      }
    }
  }
}
