using System;
using BattleTech;

namespace MissionControl.AI {
  public class DestinationUtil {
    public static RoutePointGameLogic FindDestinationByGUID(BehaviorTree tree, string waypointGUID) {
      ITaggedItem itemByGUID = tree.battleTechGame.Combat.ItemRegistry.GetItemByGUID(waypointGUID);
      return itemByGUID as RoutePointGameLogic;
    }

    public static Lance FindLanceByGUID(BehaviorTree tree, string lanceGUID) {
      ITaggedItem itemByGUID = tree.battleTechGame.Combat.ItemRegistry.GetItemByGUID(lanceGUID);
      return itemByGUID as Lance;  
    }
  }
}
