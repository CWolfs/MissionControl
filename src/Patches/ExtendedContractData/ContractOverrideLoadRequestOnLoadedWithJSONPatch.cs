using Harmony;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using System;
using System.Linq;
using System.Reflection;

using MissionControl.Data;

namespace MissionControl.Patches {
  [HarmonyPatch()]
  public class ContractOverrideLoadRequestOnLoadedWithJSONPatch {
    private static Type ContractOverrideLoadRequestType = typeof(BattleTech.Data.DataManager).GetNestedType("ContractOverrideLoadRequest", BindingFlags.NonPublic);
    private static MethodInfo notifyLoadFailedMethodInfo = AccessTools.Method(ContractOverrideLoadRequestType, "NotifyLoadFailed");
    private static FieldInfo resourceIdFieldInfo = AccessTools.Field(ContractOverrideLoadRequestType, "resourceId");
    private static MethodInfo tryLoadDependenciesMethodInfo = AccessTools.Method(ContractOverrideLoadRequestType, "TryLoadDependencies");
    private static FieldInfo resourceFieldInfo = AccessTools.Field(ContractOverrideLoadRequestType, "resource");

    public static MethodBase TargetMethod() {
      Main.LogDebug($"[ContractOverrideLoadRequestOnLoadedWithJSONPatch TargetMethod] nestedType is: '{ContractOverrideLoadRequestType}'");
      return AccessTools.Method(ContractOverrideLoadRequestType, "OnLoadedWithJSON");
    }

    public static bool Prefix(object __instance, string json) {
      Main.LogDebug($"[ContractOverrideLoadRequestOnLoadedWithJSONPatch Prefix] Overriding '{__instance}'");
      if (string.IsNullOrEmpty(json)) {
        notifyLoadFailedMethodInfo.Invoke(__instance, null);
        return false;
      }

      MContractOverride contractOverride = new MContractOverride();
      contractOverride.FromJSON(json);
      contractOverride.AssignID((string)resourceIdFieldInfo.GetValue(__instance));
      resourceFieldInfo.SetValue(__instance, contractOverride);

      tryLoadDependenciesMethodInfo.Invoke(__instance, new object[] { contractOverride as BattleTech.Data.DataManager.ILoadDependencies });

      Main.LogDebug($"[ContractOverrideLoadRequestOnLoadedWithJSONPatch Prefix] Finished '{__instance}'");

      return false;
    }
  }
}


