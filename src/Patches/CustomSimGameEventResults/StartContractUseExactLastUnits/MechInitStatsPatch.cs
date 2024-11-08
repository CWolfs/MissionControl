using Harmony;

using BattleTech;

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Mech), "InitStats")]
  public class MechInitStatsPatch {
    static void Postfix(Mech __instance) {
      Main.Logger.Log($"[MechInitStatsPatch Postfix] Patching");
      if (!MissionControl.Instance.IsNextContractUseExactLastUnits) {
        return;
      }

      List<MechDef> cachedLastMechs = SimGameStateRestoreMechPostCombatPatch.MechsToStore;

      // foreach (MechDef mechDef in cachedLastMechs) {
      //   Main.Logger.Log($"[MechInitStatsPatch Postfix] Printing cached Mechdefs - Cached mechdef: {mechDef.GUID} - {mechDef.Description.Id}");
      // }

      string mechDefGUID = __instance.MechDef.GUID;
      Main.Logger.Log($"[MechInitStatsPatch Postfix] Looking for cached mechdef with GUID: {mechDefGUID}");

      MechDef cachedMechDef = cachedLastMechs
        .Where(mechDef => mechDef.GUID == mechDefGUID)
        .FirstOrDefault();

      if (cachedMechDef != null) {
        SetDamageOnMechDef(__instance, cachedMechDef);
      }
    }

    private static void SetDamageOnMechDef(Mech mech, MechDef cachedMechDef) {
      Main.Logger.Log($"[MechInitStatsPatch Postfix] Setting damage on mechdef: {cachedMechDef.Description.Id}");

      mech.MechDef.Head.CurrentArmor = cachedMechDef.Head.CurrentArmor;
      mech.MechDef.Head.CurrentInternalStructure = cachedMechDef.Head.CurrentInternalStructure;
      mech.MechDef.Head.DamageLevel = cachedMechDef.Head.DamageLevel;

      mech.MechDef.CenterTorso.CurrentArmor = cachedMechDef.CenterTorso.CurrentArmor;
      mech.MechDef.CenterTorso.CurrentRearArmor = cachedMechDef.CenterTorso.CurrentRearArmor;
      mech.MechDef.CenterTorso.CurrentInternalStructure = cachedMechDef.CenterTorso.CurrentInternalStructure;
      mech.MechDef.CenterTorso.DamageLevel = cachedMechDef.CenterTorso.DamageLevel;

      mech.MechDef.LeftTorso.CurrentArmor = cachedMechDef.LeftTorso.CurrentArmor;
      mech.MechDef.LeftTorso.CurrentRearArmor = cachedMechDef.LeftTorso.CurrentRearArmor;
      mech.MechDef.LeftTorso.CurrentInternalStructure = cachedMechDef.LeftTorso.CurrentInternalStructure;
      mech.MechDef.LeftTorso.DamageLevel = cachedMechDef.LeftTorso.DamageLevel;

      mech.MechDef.RightTorso.CurrentArmor = cachedMechDef.RightTorso.CurrentArmor;
      mech.MechDef.RightTorso.CurrentRearArmor = cachedMechDef.RightTorso.CurrentRearArmor;
      mech.MechDef.RightTorso.CurrentInternalStructure = cachedMechDef.RightTorso.CurrentInternalStructure;
      mech.MechDef.RightTorso.DamageLevel = cachedMechDef.RightTorso.DamageLevel;

      mech.MechDef.LeftArm.CurrentArmor = cachedMechDef.LeftArm.CurrentArmor;
      mech.MechDef.LeftArm.CurrentInternalStructure = cachedMechDef.LeftArm.CurrentInternalStructure;
      mech.MechDef.LeftArm.DamageLevel = cachedMechDef.LeftArm.DamageLevel;

      mech.MechDef.RightArm.CurrentArmor = cachedMechDef.RightArm.CurrentArmor;
      mech.MechDef.RightArm.CurrentInternalStructure = cachedMechDef.RightArm.CurrentInternalStructure;
      mech.MechDef.RightArm.DamageLevel = cachedMechDef.RightArm.DamageLevel;

      mech.MechDef.LeftLeg.CurrentArmor = cachedMechDef.LeftLeg.CurrentArmor;
      mech.MechDef.LeftLeg.CurrentInternalStructure = cachedMechDef.LeftLeg.CurrentInternalStructure;
      mech.MechDef.LeftLeg.DamageLevel = cachedMechDef.LeftLeg.DamageLevel;

      mech.MechDef.RightLeg.CurrentArmor = cachedMechDef.RightLeg.CurrentArmor;
      mech.MechDef.RightLeg.CurrentInternalStructure = cachedMechDef.RightLeg.CurrentInternalStructure;
      mech.MechDef.RightLeg.DamageLevel = cachedMechDef.RightLeg.DamageLevel;

      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.Head), StatCollection.StatOperation.Set, cachedMechDef.Head.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.Head), StatCollection.StatOperation.Set, cachedMechDef.Head.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.Head), StatCollection.StatOperation.Set, cachedMechDef.Head.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Head] Current Armour: " + cachedMechDef.Head.CurrentArmor); ;
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Head] Current Internal Structure: " + cachedMechDef.Head.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Head] Damage Level: " + cachedMechDef.Head.DamageLevel);

      // center torso
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.CenterTorso), StatCollection.StatOperation.Set, cachedMechDef.CenterTorso.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.CenterTorsoRear), StatCollection.StatOperation.Set, cachedMechDef.CenterTorso.CurrentRearArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.CenterTorso), StatCollection.StatOperation.Set, cachedMechDef.CenterTorso.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.CenterTorso), StatCollection.StatOperation.Set, cachedMechDef.CenterTorso.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Center Torso] Current Armour: " + cachedMechDef.CenterTorso.CurrentArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Center Torso] Current Rear Armour: " + cachedMechDef.CenterTorso.CurrentRearArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Center Torso] Current Internal Structure: " + cachedMechDef.CenterTorso.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Center Torso] Damage Level: " + cachedMechDef.CenterTorso.DamageLevel);

      // left torso
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.LeftTorso), StatCollection.StatOperation.Set, cachedMechDef.LeftTorso.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.LeftTorsoRear), StatCollection.StatOperation.Set, cachedMechDef.LeftTorso.CurrentRearArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.LeftTorso), StatCollection.StatOperation.Set, cachedMechDef.LeftTorso.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.LeftTorso), StatCollection.StatOperation.Set, cachedMechDef.LeftTorso.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Torso] Current Armour: " + cachedMechDef.LeftTorso.CurrentArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Torso] Current Rear Armour: " + cachedMechDef.LeftTorso.CurrentRearArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Torso] Current Internal Structure: " + cachedMechDef.LeftTorso.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Torso] Damage Level: " + cachedMechDef.LeftTorso.DamageLevel);

      // right torso
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.RightTorso), StatCollection.StatOperation.Set, cachedMechDef.RightTorso.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.RightTorsoRear), StatCollection.StatOperation.Set, cachedMechDef.RightTorso.CurrentRearArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.RightTorso), StatCollection.StatOperation.Set, cachedMechDef.RightTorso.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.RightTorso), StatCollection.StatOperation.Set, cachedMechDef.RightTorso.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Torso] Current Armour: " + cachedMechDef.RightTorso.CurrentArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Torso] Current Rear Armour: " + cachedMechDef.RightTorso.CurrentRearArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Torso] Current Internal Structure: " + cachedMechDef.RightTorso.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Torso] Damage Level: " + cachedMechDef.RightTorso.DamageLevel);

      // left arm
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.LeftArm), StatCollection.StatOperation.Set, cachedMechDef.LeftArm.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.LeftArm), StatCollection.StatOperation.Set, cachedMechDef.LeftArm.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.LeftArm), StatCollection.StatOperation.Set, cachedMechDef.LeftArm.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Arm] Current Armour: " + cachedMechDef.LeftArm.CurrentArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Arm] Current Internal Structure: " + cachedMechDef.LeftArm.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Arm] Damage Level: " + cachedMechDef.LeftArm.DamageLevel);

      // right arm
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.RightArm), StatCollection.StatOperation.Set, cachedMechDef.RightArm.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.RightArm), StatCollection.StatOperation.Set, cachedMechDef.RightArm.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.RightArm), StatCollection.StatOperation.Set, cachedMechDef.RightArm.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Arm] Current Armour: " + cachedMechDef.RightArm.CurrentArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Arm] Current Internal Structure: " + cachedMechDef.RightArm.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Arm] Damage Level: " + cachedMechDef.RightArm.DamageLevel);

      // left leg
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.LeftLeg), StatCollection.StatOperation.Set, cachedMechDef.LeftLeg.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.LeftLeg), StatCollection.StatOperation.Set, cachedMechDef.LeftLeg.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.LeftLeg), StatCollection.StatOperation.Set, cachedMechDef.LeftLeg.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Leg] Current Armour: " + cachedMechDef.LeftLeg.CurrentArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Leg] Current Internal Structure: " + cachedMechDef.LeftLeg.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Left Leg] Damage Level: " + cachedMechDef.LeftLeg.DamageLevel);

      // right leg
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForArmorLocation(ArmorLocation.RightLeg), StatCollection.StatOperation.Set, cachedMechDef.RightLeg.CurrentArmor);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureLocation(ChassisLocations.RightLeg), StatCollection.StatOperation.Set, cachedMechDef.RightLeg.CurrentInternalStructure);
      mech.statCollection.ModifyStat("MC", 0, mech.GetStringForStructureDamageLevel(ChassisLocations.RightLeg), StatCollection.StatOperation.Set, cachedMechDef.RightLeg.DamageLevel);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Leg] Current Armour: " + cachedMechDef.RightLeg.CurrentArmor);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Leg] Current Internal Structure: " + cachedMechDef.RightLeg.CurrentInternalStructure);
      Main.Logger.Log("[MechInitStatsPatch Postfix] [Right Leg] Damage Level: " + cachedMechDef.RightLeg.DamageLevel);

      mech.StartingStructure = mech.SummaryStructureCurrent;
      mech.StartingArmor = mech.SummaryArmorCurrent;

      // This works but it seems the components are already in the right state so disabling this code for
      // for (int i = 0; i < mech.MechDef.Inventory.Length; i++) {
      //   MechComponent newMechComponent = mech.allComponents[i];
      //   MechComponentRef newMechComponentRef = mech.MechDef.Inventory[i];
      //   MechComponentRef cachedMechComponentRef = cachedMechDef.Inventory[i];

      //   Main.Logger.Log($"[MechInitStatsPatch Postfix] Setting damage on mech component: {newMechComponentRef.ComponentDefID} from {newMechComponent.DamageLevel} to {cachedMechComponentRef.DamageLevel}");
      //   newMechComponentRef.DamageLevel = cachedMechComponentRef.DamageLevel;
      //   newMechComponent.StatCollection.ModifyStat("MC", 0, "DamageLevel", StatCollection.StatOperation.Set, cachedMechComponentRef.DamageLevel);

      //   // TODO: Restore ammo for weapons
      // }

      // Main.Logger.Log("[MechInitStatsPatch Postfix] Refreshing all info on CombatHUDActorInfo");
      // GameObject.FindObjectsOfType<CombatHUDActorInfo>().ToList().ForEach(actorInfo => {
      //   actorInfo.RefreshAllInfo();
      // });
    }
  }
}