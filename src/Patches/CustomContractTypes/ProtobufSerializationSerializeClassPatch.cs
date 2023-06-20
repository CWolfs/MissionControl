using Harmony;

using BattleTech.Serialization;

using MissionControl.Result;
using MissionControl.Conditional;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ProtobufSerialization), "SerializeClass")]
  public class ProtobufSerializationSerializeClassPatch {
    static bool Prefix(ProtobufSerialization __instance, object serializableInstance) {

      if (MissionControl.Instance.IsCustomContractType) {
        if (serializableInstance != null && (serializableInstance.GetType() == typeof(EncounterResultBox) || serializableInstance.GetType() == typeof(EncounterConditionalBox))) {
          return false;
        }
      }

      return true;
    }
  }
}