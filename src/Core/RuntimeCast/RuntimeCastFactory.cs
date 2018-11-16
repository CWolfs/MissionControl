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
      string employerFactionKey = (employerFaction == Faction.INVALID_UNSET || employerFaction == Faction.NoFaction) ? "All" : employerFaction.ToString();
      string employerFactionName = (employerFaction == Faction.INVALID_UNSET || employerFaction == Faction.NoFaction) ? "Military Support" : employerFaction.ToString();
      string gender = DataManager.Instance.GetRandomGender();
      string firstName = DataManager.Instance.GetRandomFirstName(gender, employerFactionKey);
      string lastName = DataManager.Instance.GetRandomLastName(employerFactionKey);
      string rank = DataManager.Instance.GetRandomRank(employerFactionKey);
      Gender btGender = Gender.Male;
      if (gender == "Female") btGender = Gender.Female;
      if (gender == "Unspecified") btGender = Gender.NonBinary;

      CastDef runtimeCastDef = new CastDef();
      // Temp test data
      runtimeCastDef.id = $"castDef_{rank}{firstName}{lastName}";
      runtimeCastDef.internalName = $"{rank}{firstName}{lastName}";
      runtimeCastDef.firstName = $"{rank} {firstName}";
      runtimeCastDef.lastName = lastName;
      runtimeCastDef.callsign = rank;
      runtimeCastDef.rank = employerFactionName;
      runtimeCastDef.gender = btGender;
      runtimeCastDef.faction = employerFaction;
      runtimeCastDef.showRank = true;
      runtimeCastDef.showFirstName = true;
      runtimeCastDef.showCallsign = false;
      runtimeCastDef.showLastName = true;
      runtimeCastDef.defaultEmotePortrait.portraitAssetPath = "sprites/Portraits/guiTxrPort_SE01_AranoGuard_def_utr.png";

      ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(runtimeCastDef.id, runtimeCastDef);

      return runtimeCastDef;
    }
  }
}