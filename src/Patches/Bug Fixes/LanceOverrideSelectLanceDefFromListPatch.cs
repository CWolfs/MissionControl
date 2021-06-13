using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Patches {
  // Under weird circumstances it can ask for a lance at too high difficulty and for unknown reasons it only wants to grab lances within 10 difficulty.
  // Allowing over 10 difference in difficulty in it's search in this method should fix the issue.  This isn't a Mission Control specific fix as other
  // mod set ups could cause this but Mission Control with certain settings could induce this issue in of itself so worth fixing here.
  [HarmonyPatch(typeof(LanceOverride), "SelectLanceDefFromList")]
  public static class LanceOverrideSelectLanceDefFromListPatch {
    public static void Prefix(ref int ___MAX_DIFF_DIVERGENCE) {
      if (Main.Settings.Misc.LanceSelectionDivergenceOverride.Enable) {
        ___MAX_DIFF_DIVERGENCE = Main.Settings.Misc.LanceSelectionDivergenceOverride.Divergence;
      }
    }
  }
}
