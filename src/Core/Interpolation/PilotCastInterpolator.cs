using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Data;

using HBS.Data;

using MissionControl.RuntimeCast;

namespace MissionControl.Interpolation {
  public class PilotCastInterpolator {
    private static PilotCastInterpolator instance;
    public static PilotCastInterpolator Instance {
      get {
        if (instance == null) instance = new PilotCastInterpolator();
        return instance;
      }
    }

    private List<DialogueOverride> BackedUpDialogueOverrides { get; set; } = new List<DialogueOverride>();
    public Dictionary<string, string> DynamicCastDefs { get; set; } = new Dictionary<string, string>(); // <BindingKey, castDefID>
    public Dictionary<string, Dictionary<int, SpawnableUnit>> DynamicTakenLanceUnitPositions { get; set; } = new Dictionary<string, Dictionary<int, SpawnableUnit>>(); // <Team Name, <LancePosition, bool>>

    public void InterpolateContractDialogueCast() {
      List<DialogueOverride> dialogueOverrides = MissionControl.Instance.CurrentContract.Override.dialogueList;
      foreach (DialogueOverride dialogueOverride in dialogueOverrides) {
        // Backup all DialogueOverrides
        BackedUpDialogueOverrides.Add(dialogueOverride.Copy());

        // Process overrides
        List<DialogueContentOverride> dialogueContentOverrides = dialogueOverride.dialogueContent;

        foreach (DialogueContentOverride dialogueContentOverride in dialogueContentOverrides) {
          string interpolatedCastDefID = Interpolate(MissionControl.Instance.CurrentContract, dialogueContentOverride.selectedCastDefId);
          if (interpolatedCastDefID != null) dialogueContentOverride.selectedCastDefId = interpolatedCastDefID;
        }
      }
    }

    public string Interpolate(Contract contract, string selectedCastDefID) {
      if (!IsDynamicCastDef(selectedCastDefID)) return null;

      // GUARD: If no existing random binding, check for specific pilot slot or random request
      SpawnableUnit[] units = contract.Lances.GetLanceUnits(GetTeamIDFromCastDefID(selectedCastDefID));
      if (units.Length <= 0) return null;

      // Check the castDef - Commander, Team position or Team random
      if (selectedCastDefID == CustomCastDef.castDef_Commander) {
        return InterpolateCommander(contract, selectedCastDefID, units);
      } else if (IsPlayerTeamDynamicCastDefID(selectedCastDefID)) {
        return InterpolatePlayerPilot(contract, selectedCastDefID, units);
      } else if (IsNonPlayerTeamDynamicCastDefID(selectedCastDefID)) {
        return InterpolateNonPlayerPilot(contract, selectedCastDefID);
      }

      return selectedCastDefID;
    }

    private string InterpolateCommander(Contract contract, string selectedCastDefID, SpawnableUnit[] units) {
      Pilot commanderPilot = UnityGameInstance.Instance.Game.Simulation.Commander;
      PilotDef commanderPilotDef = commanderPilot.pilotDef;
      string pilotCastDefID = $"castDef_{commanderPilot.Description.Id}";

      HandlePilotCastDefAndRebinding(pilotCastDefID, commanderPilotDef, true);

      return pilotCastDefID;
    }

    private string InterpolatePlayerPilot(Contract contract, string selectedCastDefID, SpawnableUnit[] units) {
      // Check for already bound random pilots - castDef_TeamPilot_Random_*
      if (IsBindableRandom(selectedCastDefID)) {
        string bindingKey = GetBindingKey(selectedCastDefID);
        string boundCastDefID = GetExistingBoundCastDefID(bindingKey);
        if (boundCastDefID != null) return boundCastDefID;
      }

      int pilotPosition = SelectPilotPositionInLance(selectedCastDefID, "Player1", units); // Positon starting at slot 1

      // Handle fallback
      if (!IsPilotPositionValid(pilotPosition)) {
        // Fallback to Darius default
        Main.LogDebug($"[PilotCastInterpolator.InterpolatePlayerPilot] All pilots are used up. Defaulting to 'castDef_DariusDefault'");
        string fallbackCastDefID = "castDef_DariusDefault";

        if (IsBindableRandom(selectedCastDefID)) {
          string bindingKey = GetBindingKey(selectedCastDefID);
          PilotCastInterpolator.Instance.DynamicCastDefs[bindingKey] = fallbackCastDefID;
        }

        return fallbackCastDefID;
      }

      PilotDef pilotDef = null;
      string pilotCastDefId = "";

      if (units.Length >= pilotPosition) {
        SpawnableUnit unit = units[pilotPosition - 1];
        pilotDef = unit.Pilot;
        pilotCastDefId = $"castDef_{pilotDef.Description.Id}";

        HandlePilotCastDefAndRebinding(pilotCastDefId, pilotDef, false);

        if (IsBindableRandom(selectedCastDefID)) {
          string bindingKey = GetBindingKey(selectedCastDefID);
          PilotCastInterpolator.Instance.DynamicCastDefs[bindingKey] = pilotCastDefId;
        }

        return pilotCastDefId;
      }

      return null;
    }

    private string InterpolateNonPlayerPilot(Contract contract, string selectedCastDefID) {
      // Problem: This code runs way before non-player units are resolved. Solve this later.
      Main.LogDebugWarning("[PilotCastInterpolator] Using a dynamic pilot castdef for a non-player unit is not supported yet");
      // Maybe collect all dialogues that refer to non-player lances and run a 'late bind' on them to replace them when the units have been resolved

      return null;
    }

    private bool IsPilotPositionValid(int pilotPosition) {
      return pilotPosition > 0;
    }

    private int SelectPilotPositionInLance(string selectedCastDefID, string teamName, SpawnableUnit[] units) {
      if (IsTrueRandom(selectedCastDefID)) { // castDef_TeamPilot_Random format
        return Random.Range(1, units.Length + 1);
      } else if (IsBindableRandom(selectedCastDefID)) { // castDef_TeamPilot_Random_X format
        return FindNonUsedPilotPosition(teamName, units);
      } else { // castDef_TeamPilot_X format
        return int.Parse(GetPilotPositionFromCastDef(selectedCastDefID));
      }
    }

    public void AddTakenPilotPosition(string teamName, int unitPosition, SpawnableUnit unit = null) {
      if (!DynamicTakenLanceUnitPositions.ContainsKey(teamName)) DynamicTakenLanceUnitPositions.Add(teamName, new Dictionary<int, SpawnableUnit>());
      Dictionary<int, SpawnableUnit> takenUnitPositions = DynamicTakenLanceUnitPositions[teamName];

      takenUnitPositions[unitPosition] = unit;
    }

    private int FindNonUsedPilotPosition(string teamName, SpawnableUnit[] units) {
      if (!DynamicTakenLanceUnitPositions.ContainsKey(teamName)) DynamicTakenLanceUnitPositions.Add(teamName, new Dictionary<int, SpawnableUnit>());
      Dictionary<int, SpawnableUnit> takenUnitPositions = DynamicTakenLanceUnitPositions[teamName];

      while (takenUnitPositions.Count < units.Length) {
        int possiblePilotPosition = Random.Range(1, units.Length + 1);

        if (!takenUnitPositions.ContainsKey(possiblePilotPosition)) {
          takenUnitPositions.Add(possiblePilotPosition, units[possiblePilotPosition - 1]);
          return possiblePilotPosition;
        }
      }

      return 0;
    }

    private string GetPilotPositionFromCastDef(string selectedCastDefID) {
      return selectedCastDefID.Substring(selectedCastDefID.LastIndexOf("_") + 1);
    }

    public string GetBindingKey(string selectedCastDefID) {
      return selectedCastDefID.Substring(selectedCastDefID.IndexOf("_") + 1);
    }

    private string GetExistingBoundCastDefID(string bindingKey) {
      if (PilotCastInterpolator.Instance.DynamicCastDefs.ContainsKey(bindingKey)) {
        return PilotCastInterpolator.Instance.DynamicCastDefs[bindingKey];
      }
      return null;
    }

    private void HandlePilotCastDefAndRebinding(string pilotCastDefID, PilotDef pilotDef, bool isCommander) {
      if (!UnityGameInstance.BattleTechGame.DataManager.CastDefs.Exists(pilotCastDefID)) {
        ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(pilotCastDefID, RuntimeCastFactory.CreateCast(pilotDef, isCommander ? "Commander" : "Pilot"));
      } else {
        RebindPortrait(pilotDef);
      }
    }

    private static void RebindPortrait(PilotDef pilotDef) {
      Sprite sprite = pilotDef.GetPortraitSprite(UnityGameInstance.Instance.Game.DataManager);
      DataManager.Instance.GeneratedPortraits[pilotDef.Description.Id] = sprite;
    }

    public bool IsPlayerTeamDynamicCastDefID(string selectedCastDefID) {
      return selectedCastDefID.StartsWith(CustomCastDef.castDef_TeamPilot);
    }

    public bool IsEmployerTeamDynamicCastID(string selectedCastDefID) {
      return selectedCastDefID.StartsWith(CustomCastDef.castDef_EmployerPilot);
    }

    public bool IsTargetTeamDynamicCastID(string selectedCastDefID) {
      return selectedCastDefID.StartsWith(CustomCastDef.castDef_EmployerPilot);
    }

    public bool IsNonPlayerTeamDynamicCastDefID(string selectedCastDefID) {
      return IsEmployerTeamDynamicCastID(selectedCastDefID) || IsTargetTeamDynamicCastID(selectedCastDefID);
    }

    public bool IsDynamicCastDef(string selectedCastDefId) {
      return (selectedCastDefId == CustomCastDef.castDef_Commander || selectedCastDefId.StartsWith(CustomCastDef.castDef_TeamPilot) ||
        selectedCastDefId.StartsWith(CustomCastDef.castDef_EmployerPilot) || selectedCastDefId.StartsWith(CustomCastDef.castDef_TargetPilot));
    }

    protected static bool IsTrueRandom(string selectedCastDefId) {
      return selectedCastDefId == CustomCastDef.castDef_TeamPilot_Random || selectedCastDefId == CustomCastDef.castDef_EmployerPilot_Random || selectedCastDefId == CustomCastDef.castDef_TargetPilot_Random;
    }

    protected static bool IsBindableRandom(string selectedCastDefId) {
      return (selectedCastDefId.StartsWith(CustomCastDef.castDef_TeamPilot_Random) && (selectedCastDefId != CustomCastDef.castDef_TeamPilot_Random)) ||
        (selectedCastDefId.StartsWith(CustomCastDef.castDef_EmployerPilot_Random) && (selectedCastDefId != CustomCastDef.castDef_EmployerPilot_Random)) ||
        (selectedCastDefId.StartsWith(CustomCastDef.castDef_TargetPilot_Random) && (selectedCastDefId != CustomCastDef.castDef_TargetPilot_Random));
    }

    public string GetTeamIDFromCastDefID(string selectedCastDefID) {
      if (selectedCastDefID == CustomCastDef.castDef_Commander) return TeamUtils.PLAYER_TEAM_ID;

      int firstIndex = selectedCastDefID.IndexOf("_") + 1;
      int secondIndex = selectedCastDefID.IndexOf("Pilot");
      int length = secondIndex - firstIndex;

      string teamName = selectedCastDefID.Substring(firstIndex, length);
      if (teamName == "Team") teamName = "Player1";
      return TeamUtils.GetTeamGuid(teamName);
    }

    public void Reset() {
      // Restore backed up dialogues
      MissionControl.Instance.CurrentContract.Override.dialogueList.Clear();
      MissionControl.Instance.CurrentContract.Override.dialogueList.AddRange(BackedUpDialogueOverrides);

      // Clear old data
      BackedUpDialogueOverrides.Clear();
      DynamicCastDefs.Clear();
      DynamicTakenLanceUnitPositions.Clear();
      DataManager.Instance.ResetBetweenContracts();
    }
  }
}