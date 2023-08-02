using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Conditional {
  /**
  * TODO: Consider if this, simplier version of the vanilla check, is still required or not. It's not hooked up to anything yet.
  */
  public class RegionIsOccupiedConditional : DesignConditional {
    public string RegionGuid { get; set; }
    public List<string> UnitTags { get; set; } = new List<string>();

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);
      RegionExited regionExitedMessage = message as RegionExited;

      Main.LogDebug("[RegionIsOccupiedConditional] Evaluating");
      if (regionExitedMessage == null) return false;
      if (RegionGuid == null) {
        Main.Logger.LogError($"[RegionIsOccupiedConditional] Region Guid is null. This conditional should have a valid GUID.");
        return false;
      }

      RegionGameLogic regionGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);
      if (regionGameLogic == null) {
        Main.Logger.LogError($"[RegionIsOccupiedConditional] Region Not Found for Guid '{RegionGuid}'");
        return false;
      }

      List<AbstractActor> actors = new List<AbstractActor>();

      if (UnitTags.Count > 0) {
        List<ITaggedItem> objectsOfTypeWithTagSet = this.combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.Unit, new HBS.Collections.TagSet(UnitTags.ToArray()));
        for (int i = 0; i < objectsOfTypeWithTagSet.Count; i++) {
          AbstractActor abstractActor = objectsOfTypeWithTagSet[i] as AbstractActor;
          actors.Add(abstractActor);
        }
      } else {
        actors = UnityGameInstance.Instance.Game.Combat.AllActors;
      }

      foreach (AbstractActor actor in actors) {
        if (actor.IsInRegion(RegionGuid)) {
          Main.Logger.LogError($"[RegionIsOccupiedConditional] AbstractActor '{actor.DisplayName}' is occupying region");
          return true;
        }
      }

      return false;
    }
  }
}