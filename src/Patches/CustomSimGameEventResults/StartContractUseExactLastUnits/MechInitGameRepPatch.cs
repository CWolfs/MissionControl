using Harmony;

using BattleTech;

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Mech), "InitGameRep")]
  public class MechInitGameRepPatch {
    static void Postfix(Mech __instance) {
      Main.Logger.Log($"[MechInitGameRepPatch Postfix] Patching");
      if (!MissionControl.Instance.IsNextContractUseExactLastUnits) {
        return;
      }

      List<MechDef> cachedLastMechs = SimGameStateRestoreMechPostCombatPatch.MechsToStore;

      string mechDefGUID = __instance.MechDef.GUID;
      Main.Logger.Log($"[MechInitGameRepPatch Postfix] Looking for cached mechdef with GUID: {mechDefGUID}");

      MechDef cachedMechDef = cachedLastMechs
        .Where(mechDef => mechDef.GUID == mechDefGUID)
        .FirstOrDefault();

      if (cachedMechDef != null) {
        SetDamageOnMechDef(__instance, cachedMechDef);
      }
    }

    private static void SetDamageOnMechDef(Mech mech, MechDef cachedMechDef) {
      Main.Logger.Log($"[MechInitGameRepPatch Postfix] About to apply MechRep damage");

      if (mech.GameRep == null) {
        Main.Logger.Log("[MechInitGameRepPatch Postfix] Mech.GameRep is null, returning");
        return;
      }

      LocationDamageLevel centerTorsoLocationDamageLevel = mech.GetLocationDamageLevel(ChassisLocations.CenterTorso);
      Main.Logger.Log("[MechInitGameRepPatch Postfix] centerTorsoLocationDamageLevel: " + centerTorsoLocationDamageLevel.ToString());
      switch (centerTorsoLocationDamageLevel) {
        case LocationDamageLevel.Penalized:
          Main.Logger.Log("[MechInitGameRepPatch Postfix] Playing mild engine damage VFX on center torso");
          WwiseManager.SetSwitch(AudioSwitch_mech_engine_damaged_mildly_yesno.damaged_mildly_yes, mech.GameRep.audioObject);
          WwiseManager.PostEvent(AudioEventList_mech.mech_engine_damaged_mildly, mech.GameRep.audioObject);
          break;
        case LocationDamageLevel.NonFunctional:
          Main.Logger.Log("[MechInitGameRepPatch Postfix] Playing badly engine damage VFX on center torso");
          WwiseManager.SetSwitch(AudioSwitch_mech_engine_damaged_badly_yesno.damaged_badly_yes, mech.GameRep.audioObject);
          WwiseManager.PostEvent(AudioEventList_mech.mech_engine_damaged_badly, mech.GameRep.audioObject);
          break;
        default:
          break;
      }

      Main.Logger.Log("[MechInitGameRepPatch Postfix] Setting up gamerep damaged states");
      mech.GameRep.SetupDamageStates(mech, mech.MechDef);

      LocationDamageLevel leftLegLocationDamageLevel = mech.GetLocationDamageLevel(ChassisLocations.LeftLeg);
      LocationDamageLevel rightLegLocationDamageLevel = mech.GetLocationDamageLevel(ChassisLocations.RightLeg);

      Main.Logger.Log("[MechInitGameRepPatch Postfix] Updating leg damage animiation flags. LL: " + leftLegLocationDamageLevel.ToString() + " RL: " + rightLegLocationDamageLevel.ToString());
      mech.GameRep.UpdateLegDamageAnimFlags(leftLegLocationDamageLevel, rightLegLocationDamageLevel);

      // mech.GameRep.needsToRefreshCombinedMesh = true; // TODO: This causes a problem. Change this to trigger on maybe encounter start?

      Main.Logger.Log("[MechInitGameRepPatch Postfix] Refreshing all info on CombatHUDActorInfo");
      GameObject.FindObjectsOfType<CombatHUDActorInfo>().ToList().ForEach(actorInfo => {
        actorInfo.RefreshAllInfo();
      });

      GameObject.FindObjectsOfType<HUDMechArmorReadout>().ToList().ForEach(readout => {
        readout.ResetArmorStructureBars();
      });
    }
  }
}