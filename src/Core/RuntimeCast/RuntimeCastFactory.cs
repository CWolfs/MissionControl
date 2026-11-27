using System;
using System.Collections.Generic;

using UnityEngine;

using BattleTech;

using HBS.Data;

using MissionControl.Rules;
using MissionControl.Utils;

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
      string pilotKey = pilotDef.Description.Id.ToUpperFirst();
      string companyName = UnityGameInstance.Instance?.Game?.Simulation?.CompanyName;

      CastDef runtimeCastDef = new CastDef();
      runtimeCastDef.id = $"castDef_{pilotKey}";
      runtimeCastDef.internalName = pilotKey;
      runtimeCastDef.firstName = pilotDef.Description.Callsign;
      runtimeCastDef.lastName = pilotDef.Description.Callsign;
      runtimeCastDef.callsign = pilotDef.Description.Callsign;
      runtimeCastDef.rank = string.IsNullOrEmpty(companyName) ? rankOverride : $"{companyName} - {rankOverride}";
      runtimeCastDef.gender = pilotDef.Description.Gender;
      runtimeCastDef.FactionValue = FactionEnumeration.GetPlayer1sMercUnitFactionValue();
      runtimeCastDef.showRank = true;
      runtimeCastDef.showFirstName = true;
      runtimeCastDef.showCallsign = false;
      runtimeCastDef.showLastName = false;

      // Always route dynamic pilot cast portraits through the generated-portrait lookup so
      // 3D portraits, vanilla sprites, and ModTek-loaded PNG/DDS sprites all resolve consistently.
      runtimeCastDef.defaultEmotePortrait.portraitAssetPath = $"{pilotKey}.generated";
      CachePilotPortrait(pilotDef);

      return runtimeCastDef;
    }

    public static Sprite CachePilotPortrait(PilotDef pilotDef) {
      if (pilotDef?.Description == null || string.IsNullOrEmpty(pilotDef.Description.Id)) return null;

      string pilotKey = pilotDef.Description.Id.ToUpperFirst();
      string generatedAssetPath = $"{pilotKey}.generated";
      Sprite sprite = ResolvePilotPortraitSprite(pilotDef);
      if (sprite != null) {
        DataManager.Instance.GeneratedPortraits[pilotKey] = sprite;
        if (DataManager.Instance.UnresolvedGeneratedPortraits.Remove(generatedAssetPath)) {
          DialogueContent.dialogueSpriteCache?.Remove(generatedAssetPath);
        }
      } else {
        DataManager.Instance.GeneratedPortraits.Remove(pilotKey);
      }
      return sprite;
    }

    public static Sprite ResolvePilotPortraitSprite(PilotDef pilotDef) {
      if (pilotDef == null) return null;

      string iconId = pilotDef.Description?.Icon;
      if (!string.IsNullOrEmpty(iconId)) {
        // Preserve explicit MissionControl / mcData mod portrait overrides before falling back to game/ModTek sprites.
        Sprite modOverrideSprite = LoadModOverridePortraitByIcon(iconId);
        if (modOverrideSprite != null) return modOverrideSprite;
      }

      BattleTech.Data.DataManager btDataManager = UnityGameInstance.Instance?.Game?.DataManager;
      Sprite sprite = pilotDef.GetPortraitSprite(btDataManager);
      if (sprite != null) return sprite;

      if (string.IsNullOrEmpty(iconId)) return null;

      string generatedCacheKey = !string.IsNullOrEmpty(pilotDef.Description?.Id)
        ? $"{pilotDef.Description.Id.ToUpperFirst()}.generated"
        : null;
      sprite = ImageUtils.LoadSpriteById(btDataManager, iconId, generatedCacheKey);
      if (sprite != null) return sprite;

      return LoadVanillaPortraitByIcon(iconId);
    }

    public static PilotDef FindPilotDefByKey(string pilotKey) {
      if (string.IsNullOrEmpty(pilotKey)) return null;

      CombatGameState combat = UnityGameInstance.Instance?.Game?.Combat;
      if (combat != null) {
        if (combat.AllActors != null) {
          foreach (AbstractActor actor in combat.AllActors) {
            PilotDef pilotDef = actor?.GetPilot()?.pilotDef;
            if (IsMatchingPilotDef(pilotDef, pilotKey)) return pilotDef;
          }
        }

        if (combat.ActiveContract?.Lances != null) {
          SpawnableUnit[] playerUnits = combat.ActiveContract.Lances.GetLanceUnits(EncounterRules.PLAYER_TEAM_ID);
          if (playerUnits != null) {
            foreach (SpawnableUnit unit in playerUnits) {
              if (IsMatchingPilotDef(unit?.Pilot, pilotKey)) return unit.Pilot;
            }
          }
        }
      }

      SimGameState sim = UnityGameInstance.Instance?.Game?.Simulation;
      if (sim != null) {
        if (IsMatchingPilotDef(sim.Commander?.pilotDef, pilotKey)) return sim.Commander.pilotDef;

        if (sim.PilotRoster != null) {
          foreach (Pilot pilot in sim.PilotRoster) {
            if (IsMatchingPilotDef(pilot?.pilotDef, pilotKey)) return pilot.pilotDef;
          }
        }
      }

      BattleTech.Data.DataManager btDataManager = UnityGameInstance.Instance?.Game?.DataManager;
      if (btDataManager?.PilotDefs != null) {
        if (btDataManager.PilotDefs.Exists(pilotKey)) {
          return btDataManager.PilotDefs.Get(pilotKey);
        }

        string lowerFirstKey = char.ToLowerInvariant(pilotKey[0]) + pilotKey.Substring(1);
        if (btDataManager.PilotDefs.Exists(lowerFirstKey)) {
          return btDataManager.PilotDefs.Get(lowerFirstKey);
        }

        foreach (KeyValuePair<string, PilotDef> entry in btDataManager.PilotDefs) {
          if (IsMatchingPilotDef(entry.Value, pilotKey)) return entry.Value;
        }
      }

      return null;
    }

    private static bool IsMatchingPilotDef(PilotDef pilotDef, string pilotKey) {
      return pilotDef?.Description != null
        && string.Equals(pilotDef.Description.Id, pilotKey, StringComparison.OrdinalIgnoreCase);
    }

    private static Sprite LoadModOverridePortraitByIcon(string iconId) {
      string[] relativeCandidates = new string[] {
        $"sprites/Portraits/{iconId}.png",
        $"sprites/Portraits/{iconId}.dds"
      };

      string modPath = DataManager.Instance.ResolveModFilePath(relativeCandidates);
      if (modPath != null) {
        Sprite modSprite = ImageUtils.LoadSpriteFromDisk(modPath);
        if (modSprite != null) return modSprite;
      }

      return null;
    }

    private static Sprite LoadVanillaPortraitByIcon(string iconId) {
      string[] relativeCandidates = new string[] {
        $"sprites/Portraits/{iconId}.png",
        $"sprites/Portraits/{iconId}.dds"
      };

      foreach (string relativePath in relativeCandidates) {
        string vanillaPath = Utilities.PathUtils.AppendPath(EmotePortrait.SpriteBasePath, relativePath, false);
        Sprite vanillaSprite = ImageUtils.LoadSpriteFromDisk(vanillaPath);
        if (vanillaSprite != null) return vanillaSprite;
      }

      return null;
    }

    public static string GetPilotDefIDFromCastDefID(string castDefID) {
      return castDefID.Substring(castDefID.IndexOf("_") + 1);
    }

    public static string GetCastDefIDFromPilotDefID(string pilotDefID) {
      return $"castDef_{pilotDefID.ToUpperFirst()}";
    }

    public static CastDef GetCastDef(string castDefID) {
      if (UnityGameInstance.Instance.Game.DataManager.CastDefs.Exists(castDefID)) {
        return UnityGameInstance.Instance.Game.DataManager.CastDefs.Get(castDefID);
      }

      return null;
    }
  }
}