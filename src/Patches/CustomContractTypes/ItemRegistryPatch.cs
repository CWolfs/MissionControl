using Harmony;

using System;
using System.Collections.Generic;

using MissionControl.Data;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ItemRegistry))]
  [HarmonyPatch(MethodType.Constructor)]
  public class ItemRegistryPatch {
    static void Postfix(ItemRegistry __instance, Dictionary<TaggedObjectType, List<ITaggedItem>> ___itemsByType) {
      Array values = Enum.GetValues(typeof(MCTaggedObjectType));
      for (int i = 0; i < values.Length; i++) {
        TaggedObjectType key = (TaggedObjectType)values.GetValue(i);
        ___itemsByType[key] = new List<ITaggedItem>();
      }
    }
  }
}