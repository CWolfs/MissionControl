using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Logic {
  public abstract class LanceLogic : LogicBlock {
    public LanceLogic() {
      this.Type = LogicType.CONTRACT_OVERRIDE_MANIPULATION;
    }

    public LanceOverride SelectAppropriateLanceOverride(string teamType) {
      string biome = Enum.GetName(typeof(Biome.BIOMESKIN), MissionControl.Instance.CurrentContract.ContractBiome);
      biome = biome.Capitalise();
      string contractType = MissionControl.Instance.CurrentContractType;
      List<string> lancePoolKeys = Main.Settings.AdditionalLances.GetLancePoolKeys(teamType, biome, contractType);
      Main.Logger.Log($"[SelectAppropriateLanceOverride] Lance pool keys valid for '{teamType}', '{biome}', '{contractType}' are '{string.Join(", ", lancePoolKeys.ToArray())}'");
      return null;
    }
  }
}