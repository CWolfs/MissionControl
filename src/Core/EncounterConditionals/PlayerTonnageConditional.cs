using BattleTech;
using BattleTech.Framework;

using System.Collections.Generic;
using System.Linq;

using MissionControl.Data;

namespace MissionControl.Conditional {
  public class PlayerTonnageConditional : DesignConditional {
    public TonnageOperation Operation { get; set; }
    public float TonnageValue { get; set; }
    public bool OnlyAliveUnits { get; set; }
    public bool IncludeAllies { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      Main.LogDebug($"[PlayerTonnageConditional] Evaluating Operation '{Operation}' TonnageValue '{TonnageValue}' OnlyAliveUnits '{OnlyAliveUnits}' IncludeAllies '{IncludeAllies}'");

      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Team playerTeam = combatState.LocalPlayerTeam;

      List<AbstractActor> units = new List<AbstractActor>();

      if (IncludeAllies) {
        units = combatState.GetAllAlliesOf(playerTeam);
      } else {

        units = playerTeam.units;
      }

      if (OnlyAliveUnits) {
        units = units.Where(unit => !unit.IsDead).ToList();
      }

      // Calculate total tonnage
      float totalTonnage = 0f;
      foreach (AbstractActor unit in units) {
        if (unit is Mech mech) {
          totalTonnage += mech.MechDef.Chassis.Tonnage;
        } else if (unit is Vehicle vehicle) {
          totalTonnage += vehicle.VehicleDef.Chassis.Tonnage;
        }
        // Turrets don't have tonnage, so we skip them
      }

      Main.LogDebug($"[PlayerTonnageConditional] Total tonnage calculated: {totalTonnage}");

      // Evaluate the operation
      switch (Operation) {
        case TonnageOperation.GreaterThanOrEqualTo:
          return totalTonnage >= TonnageValue;
        case TonnageOperation.LessThanOrEqualTo:
          return totalTonnage <= TonnageValue;
        default:
          Main.Logger.LogError($"[PlayerTonnageConditional] Unsupported operation: {Operation}");
          return false;
      }
    }
  }
}
