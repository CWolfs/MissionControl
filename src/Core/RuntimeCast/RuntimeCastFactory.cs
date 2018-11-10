using UnityEngine;
using System;

using BattleTech;

using HBS.Data;

using MissionControl.Rules;

namespace MissionControl.RuntimeCast {
  public class RuntimeCastFactory {
    public static CastDef CreateCast() {
      Contract contract = MissionControl.Instance.CurrentContract;
      Faction employerFaction = contract.GetTeamFaction(EncounterRules.EMPLOYER_TEAM_ID);

      CastDef runtimeCastDef = new CastDef();
      // Temp test data
      runtimeCastDef.id = "castDef_LanceLeaderVictorDavion";
      runtimeCastDef.internalName = "castDef_LanceLeaderVictorDavion";
      runtimeCastDef.firstName = "Victor";
      runtimeCastDef.lastName = "Davion";
      runtimeCastDef.callsign = "Prince";
      runtimeCastDef.rank = employerFaction.ToString();
      runtimeCastDef.gender = Gender.Male;
      runtimeCastDef.faction = employerFaction;
      runtimeCastDef.showRank = true;
      runtimeCastDef.showFirstName = true;
      runtimeCastDef.showCallsign = true;
      runtimeCastDef.showLastName = true;
      runtimeCastDef.defaultEmotePortrait.portraitAssetPath = "sprites/Portraits/guiTxrPort_SE01_AranoGuard_def_utr.png";

      ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(runtimeCastDef.id, runtimeCastDef);

      return runtimeCastDef;
    }
  }
}