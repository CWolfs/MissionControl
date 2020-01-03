using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MissionControl.Data;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Logic {
  public abstract class LanceLogic : LogicBlock {
    public LanceLogic() {
      this.Type = LogicType.CONTRACT_OVERRIDE_MANIPULATION;
    }

    public MLanceOverride SelectAppropriateLanceOverride(string teamType) {
      string biome = Enum.GetName(typeof(Biome.BIOMESKIN), MissionControl.Instance.CurrentContract.ContractBiome);
      biome = biome.Capitalise();
      string contractType = MissionControl.Instance.CurrentContractType;
      FactionDef faction = MissionControl.Instance.GetFactionFromTeamType(teamType);
      string factionName = (faction == null) ? "UNKNOWN" : faction.Name;
      int factionRep = (MissionControl.Instance.IsSkirmish()) ? 0 : UnityGameInstance.Instance.Game.Simulation.GetRawReputation(faction.FactionValue);
      bool useElites = MissionControl.Instance.ShouldUseElites(faction, teamType);
      Config.Lance activeAdditionalLance = Main.Settings.ActiveAdditionalLances.GetActiveAdditionalLanceByTeamType(teamType);
      List<string> lancePoolKeys = Main.Settings.ActiveAdditionalLances.GetLancePoolKeys(teamType, biome, contractType, factionName, factionRep);

      int index = UnityEngine.Random.Range(0, lancePoolKeys.Count);
      string selectedLanceKey = lancePoolKeys[index];
      if (useElites) selectedLanceKey = $"{selectedLanceKey}{activeAdditionalLance.EliteLances.Suffix}";

      Main.LogDebug($"[SelectAppropriateLanceOverride] Lance pool keys valid for '{teamType.Capitalise()}', '{biome}', '{contractType}', '{faction}' are '{string.Join(", ", lancePoolKeys.ToArray())}'");

      if (DataManager.Instance.DoesLanceOverrideExist(selectedLanceKey)) {
        Main.Logger.Log($"[SelectAppropriateLanceOverride] Selected lance key '{selectedLanceKey}'");
        return DataManager.Instance.GetLanceOverride(selectedLanceKey);
      } else {
        if (useElites) {
          selectedLanceKey = selectedLanceKey.Replace(activeAdditionalLance.EliteLances.Suffix, "");
          if (DataManager.Instance.DoesLanceOverrideExist(selectedLanceKey)) {
            Main.Logger.LogError($"[SelectAppropriateLanceOverride] Cannot find 'ELITE' variant of '{selectedLanceKey}' so using original version with a +4 difficulty adjustment.");
            MLanceOverride lanceOverride = DataManager.Instance.GetLanceOverride(selectedLanceKey);
            lanceOverride.lanceDifficultyAdjustment = Mathf.Clamp(lanceOverride.lanceDifficultyAdjustment + 4, 1, 10);
            return lanceOverride;
          }
        }

        Main.Logger.LogError($"[SelectAppropriateLanceOverride] MLanceOverride of '{selectedLanceKey}' not found. Defaulting to 'Generic_Light_Battle_Lance'");
        return DataManager.Instance.GetLanceOverride("Generic_Light_Battle_Lance");
      }
    }
  }
}