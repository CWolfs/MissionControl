using System;
using System.Collections.Generic;

using MissionControl.Data;
using MissionControl.Rules;
using MissionControl.Trigger;
using MissionControl.Config;
using MissionControl.Patches;

using BattleTech;

namespace MissionControl.Logic {
  public class AddEmployerLanceBatch {
    public AddEmployerLanceBatch(EncounterRules encounterRules, string orientationTargetKey,
      SpawnLogic.LookDirection lookDirection, float minDistance, float maxDistance, List<string> lanceTags, MLanceOverride manuallySpecifiedLance = null) {

      int numberOfUnitsInLance = 4;
      string lanceGuid = Guid.NewGuid().ToString();
      List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
      string employerTeamGuid = EncounterRules.EMPLOYER_TEAM_ID;
      string spawnerName = $"Lance_Ally_SupportingForce_{lanceGuid}";

      encounterRules.EncounterLogic.Add(new AddLanceToAllyTeam(lanceGuid, unitGuids, manuallySpecifiedLance));
      encounterRules.EncounterLogic.Add(new AddLanceSpawnChunk(employerTeamGuid, lanceGuid, unitGuids, spawnerName, lanceTags, "Spawns a non-objective related ally supporting lance"));
      encounterRules.EncounterLogic.Add(new SpawnLanceMembersAroundTarget(encounterRules, spawnerName, orientationTargetKey, lookDirection, minDistance, maxDistance));

      bool useDialogue = false;

      if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.AdditionalLances_UseDialogue)) {
        useDialogue = Main.Settings.ActiveContractSettings.GetBool(ContractSettingsOverrides.AdditionalLances_UseDialogue);
        Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Additional Lances UseDialogue will be '{useDialogue}'.");
      } else {
        useDialogue = Main.Settings.AdditionalLanceSettings.UseDialogue;
      }

      if (useDialogue && !MissionControl.Instance.ContractStats.ContainsKey(ContractStats.DIALOGUE_ADDITIONAL_LANCE_ALLY_START)) {
        CastDef castDef = null;
        string dialogue = null;

        if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.AdditionalLances_DialogueCastDefId)) {
          string castDefId = Main.Settings.ActiveContractSettings.GetString(ContractSettingsOverrides.AdditionalLances_DialogueCastDefId);
          Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Additional Lances DialogueCastDefId will be '{castDefId}'.");

          if (castDefId.StartsWith(CustomCastDef.castDef_TeamPilot)) {
            DialogueApplyCastDefCommon.HandlePilotCast(MissionControl.Instance.CurrentContract, ref castDefId);
          }

          if (UnityGameInstance.BattleTechGame.DataManager.CastDefs.Exists(castDefId)) {
            castDef = UnityGameInstance.BattleTechGame.DataManager.CastDefs.Get(castDefId);
          } else {
            Main.Logger.LogError($"[Additional Lance Dialogue] Attempted to use a castdef of '{castDefId}' but this was not found.");
          }
        }

        if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.AdditionalLances_Dialogue)) {
          dialogue = Main.Settings.ActiveContractSettings.GetString(ContractSettingsOverrides.AdditionalLances_Dialogue);
          Main.Logger.Log($"[{this.GetType().Name}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Additional Lances Dialogue will be '{dialogue}'.");
        }

        MissionControl.Instance.ContractStats.Add(ContractStats.DIALOGUE_ADDITIONAL_LANCE_ALLY_START, true);
        encounterRules.EncounterLogic.Add(new AddDialogueChunk(
          ChunkLogic.DIALOGUE_ADDITIONAL_LANCE_ALLY_START_GUID,
          "AdditionalLanceAllyStart",
          "Start Conversation For Additional Lance Ally",
          lanceGuid,
          true,
          dialogue,
          castDef
        ));
        encounterRules.EncounterLogic.Add(new DialogTrigger(MessageCenterMessageType.OnEncounterIntroComplete, ChunkLogic.DIALOGUE_ADDITIONAL_LANCE_ALLY_START_GUID));
      }

      encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}