using UnityEngine;

using BattleTech;

using HBS.Data;

using MissionControl.Data;
using MissionControl.RuntimeCast;

using System;

namespace MissionControl.Patches {
  public class DialogueApplyCastDefCommon {
    protected static bool IsTrueRandom(string selectedCastDefId) {
      return selectedCastDefId == CustomCastDef.castDef_TeamPilot_Random;
    }

    protected static bool IsBindableRandom(string selectedCastDefId) {
      return selectedCastDefId.StartsWith(CustomCastDef.castDef_TeamPilot_Random) && (selectedCastDefId != CustomCastDef.castDef_TeamPilot_Random);
    }

    public static void HandlePilotCast(Contract contract, ref string selectedCastDefId) {
      // If no existing random binding, check for specific pilot slot or random request
      SpawnableUnit[] units = contract.Lances.GetLanceUnits(TeamUtils.PLAYER_TEAM_ID);
      if (units.Length <= 0) return;

      if (selectedCastDefId == CustomCastDef.castDef_Commander) {
        Pilot commanderPilot = UnityGameInstance.Instance.Game.Simulation.Commander;
        PilotDef commanderPilotDef = commanderPilot.pilotDef;
        string pilotCastDefId = $"castDef_{commanderPilot.Description.Id}";

        // If there is no pilotdef cast, make it
        if (!UnityGameInstance.BattleTechGame.DataManager.CastDefs.Exists(pilotCastDefId)) {
          ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(pilotCastDefId, RuntimeCastFactory.CreateCast(commanderPilotDef, "Commander"));
        } else {
          RebindPortrait(commanderPilotDef);
        }

        selectedCastDefId = pilotCastDefId;
      } else if (selectedCastDefId.StartsWith(CustomCastDef.castDef_TeamPilot)) {
        // Check for already bound random pilots - castDef_TeamPilot_Random_*
        if (IsBindableRandom(selectedCastDefId)) {
          int bindingID = int.Parse(selectedCastDefId.Substring(selectedCastDefId.LastIndexOf("_") + 1));
          if (MissionControl.Instance.DynamicCastDefs.ContainsKey(bindingID)) {
            selectedCastDefId = MissionControl.Instance.DynamicCastDefs[bindingID];
            return;
          }
        }

        int pilotIndex = 0;
        bool isRandomDynamicForBinding = false;

        if (IsTrueRandom(selectedCastDefId)) {
          System.Random random = new System.Random();
          int possiblePilotIndex = random.Next(1, units.Length + 1);
        } else if (IsBindableRandom(selectedCastDefId)) {
          // Weed out bindings
          while (MissionControl.Instance.DynamicTakenLanceUnitIndex.Count < units.Length) {
            System.Random random = new System.Random();
            int possiblePilotIndex = random.Next(1, units.Length + 1);

            if (!MissionControl.Instance.DynamicTakenLanceUnitIndex.ContainsKey(possiblePilotIndex)) {
              pilotIndex = possiblePilotIndex;
              MissionControl.Instance.DynamicTakenLanceUnitIndex.Add(pilotIndex, true);
              isRandomDynamicForBinding = true;
              break;
            }
          }

          // Couldn't find a valid unused unit - fallback to Darius default
          if (!isRandomDynamicForBinding) {
            Main.LogDebug($"[HandlePilotCast] All pilots are used up. Defaulting to 'castDef_DariusDefault'");
            int bindingID = int.Parse(selectedCastDefId.Substring(selectedCastDefId.LastIndexOf("_") + 1));
            selectedCastDefId = "castDef_DariusDefault";
            MissionControl.Instance.DynamicCastDefs[bindingID] = selectedCastDefId;
            return;
          }
        } else {
          string pilotIndexString = selectedCastDefId.Substring(selectedCastDefId.LastIndexOf("_") + 1);
          pilotIndex = int.Parse(pilotIndexString);
        }

        PilotDef pilotDef = null;
        string pilotCastDefId = "";

        if (units.Length >= pilotIndex) {
          SpawnableUnit unit = units[pilotIndex - 1];
          pilotDef = unit.Pilot;
          pilotCastDefId = $"castDef_{pilotDef.Description.Id}";

          // If there is no pilotdef cast, make it
          if (!UnityGameInstance.BattleTechGame.DataManager.CastDefs.Exists(pilotCastDefId)) {
            if (pilotDef.Description.Id == "commander") {
              ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(pilotCastDefId, RuntimeCastFactory.CreateCast(pilotDef, "Commander"));
            } else {
              ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(pilotCastDefId, RuntimeCastFactory.CreateCast(pilotDef));
            }
          } else {
            RebindPortrait(pilotDef);
          }

          if (isRandomDynamicForBinding) {
            int bindingID = int.Parse(selectedCastDefId.Substring(selectedCastDefId.LastIndexOf("_") + 1));
            MissionControl.Instance.DynamicCastDefs[bindingID] = pilotCastDefId;
          }

          selectedCastDefId = pilotCastDefId;
        }
      }
    }

    private static void RebindPortrait(PilotDef pilotDef) {
      Sprite sprite = pilotDef.GetPortraitSprite(UnityGameInstance.Instance.Game.DataManager);
      DataManager.Instance.GeneratedPortraits[pilotDef.Description.Id] = sprite;
    }
  }
}