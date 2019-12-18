using Harmony;

using System;
using System.Collections.Generic;

using MissionControl.Data;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ItemRegistry), "AddItem")]
  public class ItemRegistryAddItemPatch {
    static void Prefix(ItemRegistry __instance, ITaggedItem item) {
      Main.LogDebug($"[ItemRegistryAddItemPatch] Attempting to add item {item.DisplayName}");
    }
  }
}