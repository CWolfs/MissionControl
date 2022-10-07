using UnityEngine;

using BattleTech;

using HBS.Data;

using MissionControl.Rules;

namespace MissionControl.RuntimeCast {
  public class RuntimeCastFactory {
    public static CastDef CreateCast() {
      Contract contract = MissionControl.Instance.CurrentContract;
      FactionValue employerFaction = contract.GetTeamFaction(EncounterRules.EMPLOYER_TEAM_ID);
      string factionId = employerFaction.FactionDefID;
      string employerFactionName = "Military Support";

      if (employerFaction.Name != "INVALID_UNSET" && employerFaction.Name != "NoFaction") {
        FactionDef employerFactionDef = UnityGameInstance.Instance.Game.DataManager.Factions.Get(factionId);
        if (employerFactionDef == null) Main.Logger.LogError($"[RuntimeCastFactory] Error finding FactionDef for faction with id '{factionId}'");
        employerFactionName = employerFactionDef.Name.ToUpper();
      }

      string employerFactionKey = (employerFaction.Name != "INVALID_UNSET" && employerFaction.Name != "NoFaction") ? "All" : employerFaction.ToString();

      string gender = DataManager.Instance.GetRandomGender();
      string firstName = DataManager.Instance.GetRandomFirstName(gender, employerFactionKey);
      string lastName = DataManager.Instance.GetRandomLastName(employerFactionKey);
      string rank = DataManager.Instance.GetRandomRank(employerFactionKey);
      string portraitPath = DataManager.Instance.GetRandomPortraitPath(gender);
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
      runtimeCastDef.FactionValue = employerFaction;
      runtimeCastDef.showRank = true;
      runtimeCastDef.showFirstName = true;
      runtimeCastDef.showCallsign = false;
      runtimeCastDef.showLastName = true;
      runtimeCastDef.defaultEmotePortrait.portraitAssetPath = portraitPath;

      ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(runtimeCastDef.id, runtimeCastDef);

      return runtimeCastDef;
    }

    public static CastDef CreateCast(PilotDef pilotDef, string rankOverride = "Pilot") {
      CastDef runtimeCastDef = new CastDef();
      runtimeCastDef.id = $"castDef_{pilotDef.Description.Id.ToUpperFirst()}";
      runtimeCastDef.internalName = pilotDef.Description.Id.ToUpperFirst();
      runtimeCastDef.firstName = pilotDef.Description.Callsign;
      runtimeCastDef.lastName = pilotDef.Description.Callsign;
      runtimeCastDef.callsign = pilotDef.Description.Callsign;
      runtimeCastDef.rank = $"{UnityGameInstance.Instance.Game.Simulation.CompanyName} - {rankOverride}";
      runtimeCastDef.gender = pilotDef.Description.Gender;
      runtimeCastDef.FactionValue = FactionEnumeration.GetPlayer1sMercUnitFactionValue();
      runtimeCastDef.showRank = true;
      runtimeCastDef.showFirstName = true;
      runtimeCastDef.showCallsign = false;
      runtimeCastDef.showLastName = false;

      string pilotIconPath = "";
      if ((pilotDef.Description.Icon != "") && (pilotDef.Description.Icon != null)) {
        pilotIconPath = $"sprites/Portraits/{pilotDef.Description.Icon}";
        runtimeCastDef.defaultEmotePortrait.portraitAssetPath = $"{pilotIconPath}.png";
      } else {
        runtimeCastDef.defaultEmotePortrait.portraitAssetPath = $"{pilotDef.Description.Id.ToUpperFirst()}.generated";
        Sprite sprite = pilotDef.GetPortraitSprite(UnityGameInstance.Instance.Game.DataManager);
        DataManager.Instance.GeneratedPortraits[pilotDef.Description.Id.ToUpperFirst()] = sprite;
      }

      return runtimeCastDef;
    }

    public static string GetPilotDefIDFromCastDefID(string castDefID) {
      return castDefID.Substring(castDefID.IndexOf("_") + 1);
    }
  }
}