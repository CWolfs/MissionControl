using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Data;

using HBS.Data;

using MissionControl.RuntimeCast;

namespace MissionControl.Interpolation {
  /*
    1 - On contract initialisation, run a first pass over all dialogue to select and bind dynamic custom castdefs to prevent errors in vanilla code
    2 - On each interpolation, check if the speaking pilot's AbstractActor is dead or not
      2.1 - If dead, rebind and change all references in dialogue
  */
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
    public Dictionary<string, AbstractActor> BoundAbstractActors { get; set; } = new Dictionary<string, AbstractActor>(); // <BindingKey, AbstractActors>
    public Dictionary<string, int> BoundAbstractActorsFullIndex { get; set; } = new Dictionary<string, int>(); // <BindingKey, IndexInFullLanceIncludingSpaces>

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

      // GUARD: Prevent triggering when not required
      SpawnableUnit[] lanceConfigUnits = contract.Lances.GetLanceUnits(GetTeamIDFromCastDefID(selectedCastDefID));
      if (lanceConfigUnits.Length <= 0) return null;

      SpawnableUnit[] fullLanceConfigUnits = contract.Lances.GetLanceUnitsIncludeEmptySlots(GetTeamIDFromCastDefID(selectedCastDefID));
      if (fullLanceConfigUnits.Length <= 0) return null;

      // Check the castDef - Commander, Team position or Team random
      if (selectedCastDefID == CustomCastDef.castDef_Commander) {
        return InterpolateCommander(contract, selectedCastDefID);
      } else if (IsPlayerTeamDynamicCastDefID(selectedCastDefID)) {
        return InterpolatePlayerPilot(contract, selectedCastDefID, lanceConfigUnits, fullLanceConfigUnits);
      } else if (IsNonPlayerTeamDynamicCastDefID(selectedCastDefID)) {
        return InterpolateNonPlayerPilot(contract, selectedCastDefID);
      }

      return selectedCastDefID;
    }

    private string InterpolateCommander(Contract contract, string selectedCastDefID) {
      Pilot commanderPilot = UnityGameInstance.Instance.Game.Simulation.Commander;
      PilotDef commanderPilotDef = commanderPilot.pilotDef;
      string pilotCastDefID = RuntimeCastFactory.GetCastDefIDFromPilotDefID(commanderPilot.Description.Id);

      HandlePilotCastDefAndRebinding(pilotCastDefID, commanderPilotDef);

      return pilotCastDefID;
    }

    // Fallback to Darius default
    private string HandleFallback(int pilotPosition, string selectedCastDefID) {
      Main.LogDebug($"[PilotCastInterpolator.InterpolatePlayerPilot] All pilots are used up. Defaulting to 'castDef_DariusDefault'");
      string fallbackCastDefID = CustomCastDef.castDef_Darius;

      if (IsBindableRandom(selectedCastDefID)) {
        string bindingKey = GetBindingKey(selectedCastDefID);
        PilotCastInterpolator.Instance.DynamicCastDefs[bindingKey] = fallbackCastDefID;
      }

      return fallbackCastDefID;
    }

    private string BindCastDefAndActorIndex(int pilotPosition, string selectedCastDefID, SpawnableUnit[] lanceConfigUnits, SpawnableUnit[] fullLanceConfigUnits) {
      if (lanceConfigUnits.Length >= pilotPosition) {
        SpawnableUnit lanceConfigUnit = lanceConfigUnits[pilotPosition - 1];
        PilotDef lanceConfigUnitPilotDef = lanceConfigUnit.Pilot;

        // Find the full exact position so we can map it to the AbstractActors in the Team Lance
        int fullUnitPosition = fullLanceConfigUnits.Select((value, index) => new { value, index = index + 1 }).FirstOrDefault(fullUnit => fullUnit.value == lanceConfigUnit).index;

        // Can't access AbstractActors yet
        PilotDef pilotDef = lanceConfigUnitPilotDef;
        string pilotCastDefId = RuntimeCastFactory.GetCastDefIDFromPilotDefID(pilotDef.Description.Id);

        HandlePilotCastDefAndRebinding(pilotCastDefId, pilotDef);

        if (IsBindableRandom(selectedCastDefID)) {
          string bindingKey = GetBindingKey(selectedCastDefID);
          PilotCastInterpolator.Instance.DynamicCastDefs[bindingKey] = pilotCastDefId;
          PilotCastInterpolator.Instance.BoundAbstractActorsFullIndex[bindingKey] = fullUnitPosition;
        }

        return pilotCastDefId;
      }

      return null;
    }

    private string InterpolatePlayerPilot(Contract contract, string selectedCastDefID, SpawnableUnit[] lanceConfigUnits, SpawnableUnit[] fullLanceConfigUnits) {
      // Check for already bound random pilots - castDef_TeamPilot_Random_*
      if (IsBindableRandom(selectedCastDefID)) {
        string bindingKey = GetBindingKey(selectedCastDefID);
        string boundCastDefID = GetExistingBoundCastDefID(bindingKey);
        if (boundCastDefID != null) return boundCastDefID;
      }

      int pilotPosition = SelectPilotPositionInLance(selectedCastDefID, "Player1", lanceConfigUnits); // Positon starting at slot 1

      if (!IsPilotPositionValid(pilotPosition)) {
        return HandleFallback(pilotPosition, selectedCastDefID);
      }

      return BindCastDefAndActorIndex(pilotPosition, selectedCastDefID, lanceConfigUnits, fullLanceConfigUnits);
    }

    private string InterpolateNonPlayerPilot(Contract contract, string selectedCastDefID) {
      // Problem: This code runs way before non-player units are resolved. Solve this later.
      Main.LogDebugWarning("[PilotCastInterpolator] Using a dynamic pilot castdef for a non-player unit is not supported yet");
      // Maybe collect all dialogues that refer to non-player lances and run a 'late bind' on them to replace them when the units have been resolved

      return null;
    }

    public void LateBinding() {
      BindAbstractActorToBindingKey();
    }

    private void BindAbstractActorToBindingKey() {
      BoundAbstractActors.Clear();

      // GUARD: Prevent triggering when not required
      Team player1Team = TeamUtils.GetTeam(TeamUtils.PLAYER_TEAM_ID);
      List<Lance> lances = player1Team.lances;
      if (lances.Count <= 0) return;

      Lance lance = lances[0];
      List<AbstractActor> units = lance.GetLanceUnits();

      foreach (KeyValuePair<string, int> entry in BoundAbstractActorsFullIndex) {
        AbstractActor actor = units[entry.Value - 1];
        // Main.LogDebug($"[PilotCastInterpolator.BindAbstractActorToBindingKey] Binding AbstractActor '{actor.UnitName}' with pilot '{actor.GetPilot().Name}' using '{entry.Key}:{entry.Value - 1}'");
        BoundAbstractActors[entry.Key] = actor;
      }

      // Bind the commander if they are in combat
      if (!MissionControl.Instance.IsSkirmish()) {
        foreach (AbstractActor unit in units) {
          Pilot pilot = unit.GetPilot();
          Pilot commanderPilot = UnityGameInstance.Instance.Game.Simulation.Commander;

          if (pilot.Description.Id.ToUpperFirst() == commanderPilot.Description.Id.ToUpperFirst()) {
            BoundAbstractActors[DialogueInterpolationConstants.Commander] = unit;
          }
        }
      }
    }

    public string RebindDeadUnit(string bindingKey) {
      if (bindingKey == DialogueInterpolationConstants.Commander) {
        Main.LogDebug($"[PilotCastInterpolator.RebindDeadUnit] Rebinding and referencing dead commander to Darius");
        RebindDeadUnitReferences(CustomCastDef.castDef_Commander, CustomCastDef.castDef_Darius);
        return CustomCastDef.castDef_Darius;
      } else {
        string oldCastDefID = PilotCastInterpolator.Instance.DynamicCastDefs[bindingKey];
        string reboundCastDefID = RebindDeadUnitCastDef(bindingKey);
        RebindDeadUnitReferences(oldCastDefID, reboundCastDefID);
        LateBinding();
        return reboundCastDefID;
      }
    }

    private string RebindDeadUnitCastDef(string bindKey) {
      Contract contract = MissionControl.Instance.CurrentContract;
      SpawnableUnit[] lanceConfigUnits = contract.Lances.GetLanceUnits(TeamUtils.GetTeamGuid("Player1"));
      SpawnableUnit[] fullLanceConfigUnits = contract.Lances.GetLanceUnitsIncludeEmptySlots(TeamUtils.GetTeamGuid("Player1"));
      int pilotPosition = FindNonUsedPilotPosition("Player1", lanceConfigUnits);

      if (!IsPilotPositionValid(pilotPosition)) {
        return HandleFallback(pilotPosition, bindKey);
      }

      return BindCastDefAndActorIndex(pilotPosition, GetDynamicCastDefIDFromBindKey(bindKey), lanceConfigUnits, fullLanceConfigUnits);
    }

    private void RebindDeadUnitReferences(string oldCastDefID, string reboundCastDefID) {
      Main.LogDebug($"[PilotCastInterpolator.RebindDeadUnitReferences] Replacing selectedCastDefID on dialogue contents from '{oldCastDefID}' to '{reboundCastDefID}'");
      MissionControl.Instance.EncounterLayerData.OperateOnAllEncounterObjectGameLogic(delegate (EncounterObjectGameLogic eogl) {
        DialogueGameLogic dialogueGameLogic = eogl as DialogueGameLogic;
        if (dialogueGameLogic != null) {
          DialogueContent[] dialogueContents = dialogueGameLogic.conversationContent.contents;
          foreach (DialogueContent dialogueContent in dialogueContents) {
            if (dialogueContent.selectedCastDefId == oldCastDefID) {
              dialogueContent.selectedCastDefId = reboundCastDefID;
              dialogueContent.ContractInitialize(UnityGameInstance.Instance.Game.Combat);
            }
          }
        }
      });
    }

    private bool IsPilotPositionValid(int pilotPosition) {
      return pilotPosition > 0;
    }

    private int SelectPilotPositionInLance(string selectedCastDefID, string teamName, SpawnableUnit[] lanceConfigUnits) {
      if (IsTrueRandom(selectedCastDefID)) { // castDef_TeamPilot_Random format
        return UnityEngine.Random.Range(1, lanceConfigUnits.Length + 1);
      } else if (IsBindableRandom(selectedCastDefID)) { // castDef_TeamPilot_Random_X format
        return FindNonUsedPilotPosition(teamName, lanceConfigUnits);
      } else { // castDef_TeamPilot_X format
        return int.Parse(GetPilotPositionFromCastDef(selectedCastDefID));
      }
    }

    public void AddTakenPilotPosition(string teamName, int unitPosition, SpawnableUnit lanceConfigUnit = null) {
      if (!DynamicTakenLanceUnitPositions.ContainsKey(teamName)) DynamicTakenLanceUnitPositions.Add(teamName, new Dictionary<int, SpawnableUnit>());
      Dictionary<int, SpawnableUnit> takenUnitPositions = DynamicTakenLanceUnitPositions[teamName];

      takenUnitPositions[unitPosition] = lanceConfigUnit;
    }

    private int FindNonUsedPilotPosition(string teamName, SpawnableUnit[] lanceConfigUnits) {
      if (!DynamicTakenLanceUnitPositions.ContainsKey(teamName)) DynamicTakenLanceUnitPositions.Add(teamName, new Dictionary<int, SpawnableUnit>());
      Dictionary<int, SpawnableUnit> takenUnitPositions = DynamicTakenLanceUnitPositions[teamName];

      while (takenUnitPositions.Count < lanceConfigUnits.Length) {
        int possiblePilotPosition = UnityEngine.Random.Range(1, lanceConfigUnits.Length + 1);

        if (!takenUnitPositions.ContainsKey(possiblePilotPosition)) {
          takenUnitPositions.Add(possiblePilotPosition, lanceConfigUnits[possiblePilotPosition - 1]);
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

    public string GetDynamicCastDefIDFromBindKey(string bindKey) {
      return $"castDef_{bindKey}";
    }

    private string GetExistingBoundCastDefID(string bindingKey) {
      if (PilotCastInterpolator.Instance.DynamicCastDefs.ContainsKey(bindingKey)) {
        return PilotCastInterpolator.Instance.DynamicCastDefs[bindingKey];
      }
      return null;
    }

    private void HandlePilotCastDefAndRebinding(string pilotCastDefID, PilotDef pilotDef) {
      if (!UnityGameInstance.BattleTechGame.DataManager.CastDefs.Exists(pilotCastDefID)) {
        bool isCommander = pilotCastDefID == CustomCastDef.castDef_Commander;
        ((DictionaryStore<CastDef>)UnityGameInstance.BattleTechGame.DataManager.CastDefs).Add(pilotCastDefID, RuntimeCastFactory.CreateCast(pilotDef, isCommander ? "Commander" : "Pilot"));
      } else {
        RebindPortrait(pilotDef);
      }
    }

    private static void RebindPortrait(PilotDef pilotDef) {
      Sprite sprite = pilotDef.GetPortraitSprite(UnityGameInstance.Instance.Game.DataManager);
      DataManager.Instance.GeneratedPortraits[pilotDef.Description.Id.ToUpperFirst()] = sprite;
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

    public string GetBindIDFromCastDefID(string castDefID) {
      foreach (KeyValuePair<string, string> entry in DynamicCastDefs) {
        if (entry.Value == castDefID) return entry.Key;
      }
      return null;
    }

    public void Reset() {
      // Restore backed up dialogues
      MissionControl.Instance.CurrentContract.Override.dialogueList.Clear();
      MissionControl.Instance.CurrentContract.Override.dialogueList.AddRange(BackedUpDialogueOverrides);

      // Clear old data
      BackedUpDialogueOverrides.Clear();
      DynamicCastDefs.Clear();
      DynamicTakenLanceUnitPositions.Clear();
      BoundAbstractActors.Clear();
      BoundAbstractActorsFullIndex.Clear();
    }
  }
}