using Harmony;

using BattleTech.Serialization;
using BattleTech.Serialization.Models;

using MissionControl.Result;
using MissionControl.Conditional;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ProtobufSerialization), "SerializeClass")]
  public class ProtobufSerializationSerializeClassPatch {
    static bool Prefix(ProtobufSerialization __instance, object serializableInstance, ref SerializedClassResult __result) {

      if (MissionControl.Instance.IsCustomContractType) {
        if (serializableInstance != null && (serializableInstance.GetType() == typeof(EncounterResultBox) || serializableInstance.GetType() == typeof(EncounterConditionalBox))) {
          __result = default(SerializedClassResult);
          __result.ClassID = 0;
          __result.ClassData = new byte[0];
          return false;
        }
      }

      return true;
    }
  }
}