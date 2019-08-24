using UnityEngine;

using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl;

using Harmony;

public static class ItemRegistryExtensions {
  public static void RemoveItem(this ItemRegistry registry, string guid) {
    Dictionary<string, ITaggedItem> itemsByGuid = (Dictionary<string, ITaggedItem>)AccessTools.Property(typeof(ItemRegistry), "itemsByGuid").GetValue(registry, null);
    Dictionary<TaggedObjectType, List<ITaggedItem>> itemsByType = (Dictionary<TaggedObjectType, List<ITaggedItem>>)AccessTools.Property(typeof(ItemRegistry), "itemsByType").GetValue(registry, null);

    if (itemsByGuid.ContainsKey(guid)) {
      ITaggedItem item = itemsByGuid[guid];
      itemsByGuid.Remove(guid);

      if (itemsByType.ContainsKey(item.Type)) {
        itemsByType[item.Type].Remove(item);
      }
    }
  }
}