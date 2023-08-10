using BattleTech;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Data;

namespace MissionControl.Conditional {
  public class EvaluateTagConditional : DesignConditional {
    public string Scope { get; set; }
    public ExistsOperation Operation { get; set; }
    public string Tag { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      Main.LogDebug($"[EvaluateTagConditional] Evaluating Scope '{Scope}' Operation '{Operation}' Tag '{Tag}'");
      TagSet tags = TagUtils.GetTagSet(Scope);

      switch (Operation) {
        case ExistsOperation.IsSet: return tags.Contains(Tag);
        case ExistsOperation.NotSet: return !tags.Contains(Tag);
      }

      return false;
    }
  }
}