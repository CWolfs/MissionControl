using Harmony;

using BattleTech;

using System.Collections.Generic;

using MissionControl.Utils;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "RestoreMechPostCombat")]
  public class SimGameStateRestoreMechPostCombatPatch {
    public static List<MechDef> MechsToStore { get; set; } = new List<MechDef>();

    static bool Prefix(SimGameState __instance, MechDef mech) {
      Main.Logger.Log($"[SimGameStateRestoreMechPostCombatPatch Prefix] Patching");

      if (MissionControl.Instance.IsNextContractUseExactLastUnits) {
        Main.Logger.Log($"[SimGameStateRestoreMechPostCombatPatch Prefix] UseExactLastUnits so not repairing armour on mechs with undamaged internals");

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Cloning mechdef for later use: " + mech.GUID + " - " + mech.Description.Id);

        // Effectively clone the mechdef
        MechDef mechDef = new MechDef(mech);

        mechDef.Head.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.Head.CurrentInternalStructure, mechDef.Chassis.Head.InternalStructure);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Calculating damage levels for mechdef: " + mechDef.GUID + " - " + mechDef.Description.Id);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Original LeftArm.DamageLevel: " + mechDef.LeftArm.DamageLevel);
        mechDef.LeftArm.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.LeftArm.CurrentInternalStructure, mechDef.Chassis.LeftArm.InternalStructure);
        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Updated LeftArm.DamageLevel: " + mechDef.LeftArm.DamageLevel);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Original LeftTorso.DamageLevel: " + mechDef.LeftTorso.DamageLevel);
        mechDef.LeftTorso.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.LeftTorso.CurrentInternalStructure, mechDef.Chassis.LeftTorso.InternalStructure);
        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Updated LeftTorso.DamageLevel: " + mechDef.LeftTorso.DamageLevel);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Original CenterTorso.DamageLevel: " + mechDef.CenterTorso.DamageLevel);
        mechDef.CenterTorso.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.CenterTorso.CurrentInternalStructure, mechDef.Chassis.CenterTorso.InternalStructure);
        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Updated CenterTorso.DamageLevel: " + mechDef.CenterTorso.DamageLevel);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Original RightTorso.DamageLevel: " + mechDef.RightTorso.DamageLevel);
        mechDef.RightArm.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.RightArm.CurrentInternalStructure, mechDef.Chassis.RightArm.InternalStructure);
        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Updated RightArm.DamageLevel: " + mechDef.RightArm.DamageLevel);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Original RightTorso.DamageLevel: " + mechDef.RightTorso.DamageLevel);
        mechDef.RightTorso.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.RightTorso.CurrentInternalStructure, mechDef.Chassis.RightTorso.InternalStructure);
        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Updated RightTorso.DamageLevel: " + mechDef.RightTorso.DamageLevel);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Original LeftLeg.DamageLevel: " + mechDef.LeftLeg.DamageLevel);
        mechDef.LeftLeg.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.LeftLeg.CurrentInternalStructure, mechDef.Chassis.LeftLeg.InternalStructure);
        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Updated LeftLeg.DamageLevel: " + mechDef.LeftLeg.DamageLevel);

        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Original RightLeg.DamageLevel: " + mechDef.RightLeg.DamageLevel);
        mechDef.RightLeg.DamageLevel = DamageTools.CalculateDamageLevel(mechDef.RightLeg.CurrentInternalStructure, mechDef.Chassis.RightLeg.InternalStructure);
        Main.Logger.Log("[SimGameStateRestoreMechPostCombatPatch Prefix] Updated RightLeg.DamageLevel: " + mechDef.RightLeg.DamageLevel);

        MechsToStore.Add(mechDef);
        return false;
      }

      return true;
    }
  }
}