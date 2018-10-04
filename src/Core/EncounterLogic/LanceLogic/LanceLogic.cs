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

      int index = UnityEngine.Random.Range(0, lancePoolKeys.Count);
      string selectedLanceKey = lancePoolKeys[index];

      if (Main.Settings.DebugMode) {
        Main.Logger.Log($"[SelectAppropriateLanceOverride] Lance pool keys valid for '{teamType}', '{biome}', '{contractType}' are '{string.Join(", ", lancePoolKeys.ToArray())}'");
      }

      if (DataManager.Instance.DoesLanceOverrideExist(selectedLanceKey)) {
        Main.Logger.Log($"[SelectAppropriateLanceOverride] Selected lance key '{selectedLanceKey}'");
        return DataManager.Instance.GetLanceOverride(selectedLanceKey);
      } else {
        Main.Logger.LogError($"[SelectAppropriateLanceOverride] MLanceOverride of '{selectedLanceKey}' not found. Defaulting to 'GENERIC_BATTLE_LANCE'");
        return DataManager.Instance.GetLanceOverride("GENERIC_BATTLE_LANCE");
      }
    }
  }
}