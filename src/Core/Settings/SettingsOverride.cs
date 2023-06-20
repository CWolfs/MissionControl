using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace MissionControl.Config {
  public class SettingsOverride {
    public static string DebugMode = "DebugMode";
    public static string VersionCheck = "VersionCheck";

    public static string EnableSkirmishMode = "EnableSkirmishMode";
    public static string DebugSkirmishMode = "DebugSkirmishMode";

    public static string EnableFlashpointOverrides = "EnableFlashpointOverrides";
    public static string EnableAdditionalPlayerMechsForFlashpoints = "EnableAdditionalPlayerMechsForFlashpoints";
    public static string NeverFailSimGameInFlashpoints = "NeverFailSimGameInFlashpoints";

    public static string EnableStoryOverrides = "EnableStoryOverrides";
    public static string EnableAdditionalPlayerMechsForStory = "EnableAdditionalPlayerMechsForStory";

    public static string RandomSpawns_Enable = "RandomSpawns.Enable";
    public static string RandomSpawns_EnableForFlashpoints = "RandomSpawns.EnableForFlashpoints";
    public static string RandomSpawns_EnableForStory = "RandomSpawns.EnableForStory";
    public static string RandomSpawns_IncludeContractTypes = "RandomSpawns.IncludeContractTypes";
    public static string RandomSpawns_ExcludeContractTypes = "RandomSpawns.ExcludeContractTypes";

    public static string HotDropProtection_Enable = "HotDropProtection.Enable";
    public static string HotDropProtection_EnableForFlashpoints = "HotDropProtection.EnableForFlashpoints";
    public static string HotDropProtection_EnableForStory = "HotDropProtection.EnableForStory";
    public static string HotDropProtection_IncludeContractTypes = "HotDropProtection.IncludeContractTypes";
    public static string HotDropProtection_ExcludeContractTypes = "HotDropProtection.ExcludeContractTypes";
    public static string HotDropProtection_GuardOnHotDrop = "HotDropProtection.GuardOnHotDrop";
    public static string HotDropProtection_EvasionPipsOnHotDrop = "HotDropProtection.EvasionPipsOnHotDrop";
    public static string HotDropProtection_IncludeEnemies = "HotDropProtection.IncludeEnemies";
    public static string HotDropProtection_IncludeAllyTurrets = "HotDropProtection.IncludeAllyTurrets";
    public static string HotDropProtection_IncludeEnemyTurrets = "HotDropProtection.IncludeEnemyTurrets";

    public static string AdditionalLances_Enable = "AdditionalLances.Enable";
    public static string AdditionalLances_EnableForFlashpoints = "AdditionalLances.EnableForFlashpoints";
    public static string AdditionalLances_EnableForStory = "AdditionalLances.EnableForStory";
    public static string AdditionalLances_IncludeContractTypes = "AdditionalLances.IncludeContractTypes";
    public static string AdditionalLances_ExcludeContractTypes = "AdditionalLances.ExcludeContractTypes";
    public static string AdditionalLances_IsPrimaryObjectiveIn = "AdditionalLances.IsPrimaryObjectiveIn";
    public static string AdditionalLances_ExcludeFromAutocomplete = "AdditionalLances.ExcludeFromAutocomplete";
    public static string AdditionalLances_HideObjective = "AdditionalLances.HideObjective";
    public static string AdditionalLances_ShowObjectiveOnLanceDetected = "AdditionalLances.ShowObjectiveOnLanceDetected";
    public static string AdditionalLances_AlwaysDisplayHiddenObjectiveIfPrimary = "AdditionalLances.AlwaysDisplayHiddenObjectiveIfPrimary";
    public static string AdditionalLances_UseElites = "AdditionalLances.UseElites";
    public static string AdditionalLances_UseDialogue = "AdditionalLances.UseDialogue";
    public static string AdditionalLances_SkullValueMatters = "AdditionalLances.SkullValueMatters";
    public static string AdditionalLances_BasedOnVisibleSkullValue = "AdditionalLances.BasedOnVisibleSkullValue";
    public static string AdditionalLances_UseGeneralProfileForSkirmish = "AdditionalLances.UseGeneralProfileForSkirmish";

    public static string AdditionalLances_DisableWhenMaxTonnage_Enable = "AdditionalLances.DisableWhenMaxTonnage.Enable";
    public static string AdditionalLances_DisableWhenMaxTonnage_Limited = "AdditionalLances.DisableWhenMaxTonnage.Limited";
    public static string AdditionalLances_DisableWhenMaxTonnage_LimitedToUnder = "AdditionalLances.DisableWhenMaxTonnage.LimitedToUnder";

    public static string AdditionalLances_MatchAllyLanceCountToEnemy = "AdditionalLances.MatchAllyLanceCountToEnemy";

    public static string AdditionalLances_DropWeightInfluence_Enable = "AdditionalLances.DropWeightInfluence.Enable";
    public static string AdditionalLances_DropWeightInfluence_EnemySpawnInfluenceMax = "AdditionalLances.DropWeightInfluence.EnemySpawnInfluenceMax";
    public static string AdditionalLances_DropWeightInfluence_AllySpawnInfluenceMax = "AdditionalLances.DropWeightInfluence.AllySpawnInfluenceMax";
    public static string AdditionalLances_DropWeightInfluence_EnemySpawnInfluencePerHalfSkullAbove = "AdditionalLances.DropWeightInfluence.EnemySpawnInfluencePerHalfSkullAbove";
    public static string AdditionalLances_DropWeightInfluence_AllySpawnInfluencePerHalfSkullAbove = "AdditionalLances.DropWeightInfluence.AllySpawnInfluencePerHalfSkullAbove";
    public static string AdditionalLances_DropWeightInfluence_EnemySpawnInfluencePerHalfSkullBelow = "AdditionalLances.DropWeightInfluence.EnemySpawnInfluencePerHalfSkullBelow";
    public static string AdditionalLances_DropWeightInfluence_AllySpawnInfluencePerHalfSkullBelow = "AdditionalLances.DropWeightInfluence.AllySpawnInfluencePerHalfSkullBelow";

    public static string AdditionalLances_DisableAllies = "AdditionalLances.DisableAllies";
    public static string AdditionalLances_DisableEnemies = "AdditionalLances.DisableEnemies";

    public static string ExtendedLances_Enable = "ExtendedLances.Enable";
    public static string ExtendedLances_EnableForFlashpoints = "ExtendedLances.EnableForFlashpoints";
    public static string ExtendedLances_EnableForStory = "ExtendedLances.EnableForStory";
    public static string ExtendedLances_EnableForTargetAlly = "ExtendedLances.EnableForTargetAlly";
    public static string ExtendedLances_EnableForEmployerAlly = "ExtendedLances.EnableForEmployerAlly";
    public static string ExtendedLances_EnableForHostileToAll = "ExtendedLances.EnableForHostileToAll";
    public static string ExtendedLances_EnableForNeutralToAll = "ExtendedLances.EnableForNeutralToAll";
    public static string ExtendedLances_Autofill = "ExtendedLances.Autofill";
    public static string ExtendedLances_AutofillType = "ExtendedLances.AutofillType";
    public static string ExtendedLances_AutofillUnitCopyType = "ExtendedLances.AutofillUnitCopyType";
    public static string ExtendedLances_AutofillStartingFromContractDifficulty = "ExtendedLances.AutofillStartingFromContractDifficulty";
    public static string ExtendedLances_IncludeContractTypes = "ExtendedLances.IncludeContractTypes";
    public static string ExtendedLances_ExcludeContractTypes = "ExtendedLances.ExcludeContractTypes";
    public static string ExtendedLances_SkipWhenTaggedWithAny = "ExtendedLances.SkipWhenTaggedWithAny";
    public static string ExtendedLances_SkipWhenTaggedWithAll = "ExtendedLances.SkipWhenTaggedWithAll";
    public static string ExtendedLances_SkipWhenExcludeTagsContain = "ExtendedLances.SkipWhenExcludeTagsContain";
    public static string ExtendedLances_ForceLanceOverrideSizeWithTag = "ExtendedLances.ForceLanceOverrideSizeWithTag";
    public static string ExtendedLances_ForceLanceDefSizeWithTag = "ExtendedLances.ForceLanceDefSizeWithTag";
    public static string ExtendedLances_LanceSizes = "ExtendedLances.LanceSizes";

    public static string ExtendedBoundaries_Enable = "ExtendedBoundaries.Enable";
    public static string ExtendedBoundaries_EnableForFlashpoints = "ExtendedBoundaries.EnableForFlashpoints";
    public static string ExtendedBoundaries_EnableForStory = "ExtendedBoundaries.EnableForStory";
    public static string ExtendedBoundaries_IncludeContractTypes = "ExtendedBoundaries.IncludeContractTypes";
    public static string ExtendedBoundaries_ExcludeContractTypes = "ExtendedBoundaries.ExcludeContractTypes";
    public static string ExtendedBoundaries_IncreaseBoundarySizeByPercentage = "ExtendedBoundaries.IncreaseBoundarySizeByPercentage";
    public static string ExtendedBoundaries_Overrides = "ExtendedBoundaries.Overrides";

    public static string DynamicWithdraw_Enable = "DynamicWithdraw.Enable";
    public static string DynamicWithdraw_EnableForFlashpoints = "DynamicWithdraw.EnableForFlashpoints";
    public static string DynamicWithdraw_EnableForStory = "DynamicWithdraw.EnableForStory";
    public static string DynamicWithdraw_IncludeContractTypes = "DynamicWithdraw.IncludeContractTypes";
    public static string DynamicWithdraw_ExcludeContractTypes = "DynamicWithdraw.ExcludeContractTypes";
    public static string DynamicWithdraw_MinDistanceForZone = "DynamicWithdraw.MinDistanceForZone";
    public static string DynamicWithdraw_MaxDistanceForZone = "DynamicWithdraw.MaxDistanceForZone";
    public static string DynamicWithdraw_DisorderlyWithdrawalCompatibility = "DynamicWithdraw.DisorderlyWithdrawalCompatibility";

    public static string AI_FollowPlayer_Pathfinding = "AI.FollowPlayer.Pathfinding";
    public static string AI_FollowPlayer_Target = "AI.FollowPlayer.Target";
    public static string AI_FollowPlayer_StopWhen = "AI.FollowPlayer.StopWhen";
    public static string AI_FollowPlayer_MaxDistanceFromTargetBeforeSprinting = "AI.FollowPlayer.MaxDistanceFromTargetBeforeSprinting";
    public static string AI_FollowPlayer_TargetZoneRadius = "AI.FollowPlayer.TargetZoneRadius";
    public static string AI_FollowPlayer_TimeToWaitForPathfinding = "AI.FollowPlayer.TimeToWaitForPathfinding";
    public static string AI_FollowPlayer_TicksToWaitForPathfinding = "AI.FollowPlayer.TicksToWaitForPathfinding";

    public static string Spawners_SpawnLanceAtEdgeBoundary_MinBuffer = "Spawners.SpawnLanceAtEdgeBoundary.MinBuffer";
    public static string Spawners_SpawnLanceAtEdgeBoundary_MaxBuffer = "Spawners.SpawnLanceAtEdgeBoundary.MaxBuffer";

    public static string CustomData_Search = "CustomData.Search";
    public static string CustomData_SearchType = "CustomData.SearchType";

    public static string Misc_LanceSelectionDivergenceOverride_Enable = "Misc.LanceSelectionDivergenceOverride.Enable";
    public static string Misc_LanceSelectionDivergenceOverride_Divergence = "Misc.LanceSelectionDivergenceOverride.Divergence";

    public static string AdditionalPlayerMechs_Enable = "AdditionalPlayerMechs";

    JsonSerializerSettings serialiserSettings = new JsonSerializerSettings() {
      TypeNameHandling = TypeNameHandling.All,
      Culture = CultureInfo.InvariantCulture
    };

    public string ModDirectory { get; private set; }
    public string Type { get; private set; }

    public bool Enabled {
      get => Properties != null;
    }
    public JObject Properties { get; set; }

    public bool Has(string path) {
      if (Properties == null) return false;
      JToken token = Properties.SelectToken(path);
      return token != null;
    }

    public bool GetBool(string path) {
      JToken token = Properties.SelectToken(path);
      return token.ToObject<bool>();
    }

    public int GetInt(string path) {
      JToken token = Properties.SelectToken(path);
      return token.ToObject<int>();
    }

    public float GetFloat(string path) {
      JToken token = Properties.SelectToken(path);
      return token.ToObject<float>();
    }

    public string GetString(string path) {
      JToken token = Properties.SelectToken(path);
      return token.ToString();
    }

    public List<string> GetStringList(string path) {
      JToken token = Properties.SelectToken(path);
      return token.ToObject<List<string>>();
    }

    public SettingsOverride(string modDirectory, string type) {
      ModDirectory = modDirectory;
      Type = type;

      string filePath = $"{ModDirectory}/settings.{Type}.json";

      if (File.Exists(filePath)) {
        Main.LogDebug($"[SettingsOverride] Loading 'settings.{Type}.json'");
        string rawSettings = File.ReadAllText(filePath);
        Properties = JsonConvert.DeserializeObject<JObject>(rawSettings, serialiserSettings);
      }
    }

    public Settings LoadOverrides(Settings settings) {
      if (Properties != null) {
        LoadRoot(settings);
        LoadRandomSpawns(settings);
        LoadHotDropProtection(settings);
        LoadAdditionalLances(settings);
        LoadExtendedLances(settings);
        LoadExtendedBoundaries(settings);
        LoadDynamicWithdraw(settings);
        LoadAI(settings);
        LoadSpawners(settings);
        LoadCustomData(settings);
        LoadMisc(settings);
      }

      return settings;
    }

    private void LoadRoot(Settings settings) {
      if (Has(DebugMode)) settings.DebugMode = GetBool(DebugMode);
      if (Has(VersionCheck)) settings.VersionCheck = GetBool(VersionCheck);

      if (Has(EnableSkirmishMode)) settings.EnableSkirmishMode = GetBool(EnableSkirmishMode);
      if (Has(DebugSkirmishMode)) settings.DebugSkirmishMode = GetBool(DebugSkirmishMode);

      if (Has(EnableFlashpointOverrides)) settings.EnableFlashpointOverrides = GetBool(EnableFlashpointOverrides);
      if (Has(EnableAdditionalPlayerMechsForFlashpoints)) settings.EnableAdditionalPlayerMechsForFlashpoints = GetBool(EnableAdditionalPlayerMechsForFlashpoints);
      if (Has(NeverFailSimGameInFlashpoints)) settings.NeverFailSimGameInFlashpoints = GetBool(NeverFailSimGameInFlashpoints);

      if (Has(EnableStoryOverrides)) settings.EnableStoryOverrides = GetBool(EnableStoryOverrides);
      if (Has(EnableAdditionalPlayerMechsForStory)) settings.EnableAdditionalPlayerMechsForStory = GetBool(EnableAdditionalPlayerMechsForStory);
    }

    private void LoadRandomSpawns(Settings settings) {
      if (Has(RandomSpawns_Enable)) settings.RandomSpawns.Enable = GetBool(RandomSpawns_Enable);
      if (Has(RandomSpawns_EnableForFlashpoints)) settings.RandomSpawns.EnableForFlashpoints = GetBool(RandomSpawns_EnableForFlashpoints);
      if (Has(RandomSpawns_EnableForStory)) settings.RandomSpawns.EnableForStory = GetBool(RandomSpawns_EnableForStory);
      if (Has(RandomSpawns_IncludeContractTypes)) settings.RandomSpawns.IncludeContractTypes = GetStringList(RandomSpawns_IncludeContractTypes);
      if (Has(RandomSpawns_ExcludeContractTypes)) settings.RandomSpawns.ExcludeContractTypes = GetStringList(RandomSpawns_ExcludeContractTypes);
    }

    private void LoadHotDropProtection(Settings settings) {
      if (Has(HotDropProtection_Enable)) settings.HotDropProtection.Enable = GetBool(HotDropProtection_Enable);
      if (Has(HotDropProtection_EnableForFlashpoints)) settings.HotDropProtection.EnableForFlashpoints = GetBool(HotDropProtection_EnableForFlashpoints);
      if (Has(HotDropProtection_EnableForStory)) settings.HotDropProtection.EnableForStory = GetBool(HotDropProtection_EnableForStory);
      if (Has(HotDropProtection_IncludeContractTypes)) settings.HotDropProtection.IncludeContractTypes = GetStringList(HotDropProtection_IncludeContractTypes);
      if (Has(HotDropProtection_ExcludeContractTypes)) settings.HotDropProtection.ExcludeContractTypes = GetStringList(HotDropProtection_ExcludeContractTypes);
      if (Has(HotDropProtection_GuardOnHotDrop)) settings.HotDropProtection.GuardOnHotDrop = GetBool(HotDropProtection_GuardOnHotDrop);
      if (Has(HotDropProtection_EvasionPipsOnHotDrop)) settings.HotDropProtection.EvasionPipsOnHotDrop = GetInt(HotDropProtection_EvasionPipsOnHotDrop);
      if (Has(HotDropProtection_IncludeEnemies)) settings.HotDropProtection.IncludeEnemies = GetBool(HotDropProtection_IncludeEnemies);
      if (Has(HotDropProtection_IncludeAllyTurrets)) settings.HotDropProtection.IncludeAllyTurrets = GetBool(HotDropProtection_IncludeAllyTurrets);
      if (Has(HotDropProtection_IncludeEnemyTurrets)) settings.HotDropProtection.IncludeEnemyTurrets = GetBool(HotDropProtection_IncludeEnemyTurrets);
    }

    private void LoadAdditionalLances(Settings settings) {
      if (Has(AdditionalLances_Enable)) settings.AdditionalLanceSettings.Enable = GetBool(AdditionalLances_Enable);
      if (Has(AdditionalLances_EnableForFlashpoints)) settings.AdditionalLanceSettings.EnableForFlashpoints = GetBool(AdditionalLances_EnableForFlashpoints);
      if (Has(AdditionalLances_EnableForStory)) settings.AdditionalLanceSettings.EnableForStory = GetBool(AdditionalLances_EnableForStory);
      if (Has(AdditionalLances_IncludeContractTypes)) settings.AdditionalLanceSettings.IncludeContractTypes = GetStringList(AdditionalLances_IncludeContractTypes);
      if (Has(AdditionalLances_ExcludeContractTypes)) settings.AdditionalLanceSettings.ExcludeContractTypes = GetStringList(AdditionalLances_ExcludeContractTypes);
      if (Has(AdditionalLances_IsPrimaryObjectiveIn)) settings.AdditionalLanceSettings.IsPrimaryObjectiveIn = GetStringList(AdditionalLances_IsPrimaryObjectiveIn);
      if (Has(AdditionalLances_ExcludeFromAutocomplete)) settings.AdditionalLanceSettings.ExcludeFromAutocomplete = GetStringList(AdditionalLances_ExcludeFromAutocomplete);

      if (Has(AdditionalLances_HideObjective)) settings.AdditionalLanceSettings.HideObjective = GetBool(AdditionalLances_HideObjective);
      if (Has(AdditionalLances_ShowObjectiveOnLanceDetected)) settings.AdditionalLanceSettings.ShowObjectiveOnLanceDetected = GetBool(AdditionalLances_ShowObjectiveOnLanceDetected);
      if (Has(AdditionalLances_AlwaysDisplayHiddenObjectiveIfPrimary)) settings.AdditionalLanceSettings.AlwaysDisplayHiddenObjectiveIfPrimary = GetBool(AdditionalLances_AlwaysDisplayHiddenObjectiveIfPrimary);
      if (Has(AdditionalLances_UseElites)) settings.AdditionalLanceSettings.UseElites = GetBool(AdditionalLances_UseElites);
      if (Has(AdditionalLances_UseDialogue)) settings.AdditionalLanceSettings.UseDialogue = GetBool(AdditionalLances_UseDialogue);
      if (Has(AdditionalLances_SkullValueMatters)) settings.AdditionalLanceSettings.SkullValueMatters = GetBool(AdditionalLances_SkullValueMatters);
      if (Has(AdditionalLances_BasedOnVisibleSkullValue)) settings.AdditionalLanceSettings.BasedOnVisibleSkullValue = GetBool(AdditionalLances_BasedOnVisibleSkullValue);
      if (Has(AdditionalLances_UseGeneralProfileForSkirmish)) settings.AdditionalLanceSettings.UseGeneralProfileForSkirmish = GetBool(AdditionalLances_UseGeneralProfileForSkirmish);

      if (Has(AdditionalLances_DisableWhenMaxTonnage_Enable)) settings.AdditionalLanceSettings.DisableWhenMaxTonnage.Enable = GetBool(AdditionalLances_DisableWhenMaxTonnage_Enable);
      if (Has(AdditionalLances_DisableWhenMaxTonnage_Limited)) settings.AdditionalLanceSettings.DisableWhenMaxTonnage.Limited = GetBool(AdditionalLances_DisableWhenMaxTonnage_Limited);
      if (Has(AdditionalLances_DisableWhenMaxTonnage_LimitedToUnder)) settings.AdditionalLanceSettings.DisableWhenMaxTonnage.LimitedToUnder = GetInt(AdditionalLances_DisableWhenMaxTonnage_LimitedToUnder);

      if (Has(AdditionalLances_MatchAllyLanceCountToEnemy)) settings.AdditionalLanceSettings.MatchAllyLanceCountToEnemy = GetBool(AdditionalLances_MatchAllyLanceCountToEnemy);

      if (Has(AdditionalLances_DropWeightInfluence_Enable)) settings.AdditionalLanceSettings.DropWeightInfluenceSettings.Enable = GetBool(AdditionalLances_DropWeightInfluence_Enable);
      if (Has(AdditionalLances_DropWeightInfluence_EnemySpawnInfluenceMax)) settings.AdditionalLanceSettings.DropWeightInfluenceSettings.EnemySpawnInfluenceMax = GetFloat(AdditionalLances_DropWeightInfluence_EnemySpawnInfluenceMax);
      if (Has(AdditionalLances_DropWeightInfluence_AllySpawnInfluenceMax)) settings.AdditionalLanceSettings.DropWeightInfluenceSettings.AllySpawnInfluenceMax = GetFloat(AdditionalLances_DropWeightInfluence_AllySpawnInfluenceMax);
      if (Has(AdditionalLances_DropWeightInfluence_EnemySpawnInfluencePerHalfSkullAbove)) settings.AdditionalLanceSettings.DropWeightInfluenceSettings.EnemySpawnInfluencePerHalfSkullAbove = GetFloat(AdditionalLances_DropWeightInfluence_EnemySpawnInfluencePerHalfSkullAbove);
      if (Has(AdditionalLances_DropWeightInfluence_AllySpawnInfluencePerHalfSkullAbove)) settings.AdditionalLanceSettings.DropWeightInfluenceSettings.AllySpawnInfluencePerHalfSkullAbove = GetFloat(AdditionalLances_DropWeightInfluence_AllySpawnInfluencePerHalfSkullAbove);
      if (Has(AdditionalLances_DropWeightInfluence_EnemySpawnInfluencePerHalfSkullBelow)) settings.AdditionalLanceSettings.DropWeightInfluenceSettings.EnemySpawnInfluencePerHalfSkullBelow = GetFloat(AdditionalLances_DropWeightInfluence_EnemySpawnInfluencePerHalfSkullBelow);
      if (Has(AdditionalLances_DropWeightInfluence_AllySpawnInfluencePerHalfSkullBelow)) settings.AdditionalLanceSettings.DropWeightInfluenceSettings.AllySpawnInfluencePerHalfSkullBelow = GetFloat(AdditionalLances_DropWeightInfluence_AllySpawnInfluencePerHalfSkullBelow);

      if (Has(AdditionalLances_DisableAllies)) settings.AdditionalLanceSettings.DisableAllies = GetBool(AdditionalLances_DisableAllies);
      if (Has(AdditionalLances_DisableEnemies)) settings.AdditionalLanceSettings.DisableEnemies = GetBool(AdditionalLances_DisableEnemies);
    }

    private void LoadExtendedLances(Settings settings) {
      if (Has(ExtendedLances_Enable)) settings.ExtendedLances.Enable = GetBool(ExtendedLances_Enable);
      if (Has(ExtendedLances_EnableForFlashpoints)) settings.ExtendedLances.EnableForFlashpoints = GetBool(ExtendedLances_EnableForFlashpoints);
      if (Has(ExtendedLances_EnableForStory)) settings.ExtendedLances.EnableForStory = GetBool(ExtendedLances_EnableForStory);
      if (Has(ExtendedLances_EnableForTargetAlly)) settings.ExtendedLances.EnableForTargetAlly = GetBool(ExtendedLances_EnableForTargetAlly);
      if (Has(ExtendedLances_EnableForEmployerAlly)) settings.ExtendedLances.EnableForEmployerAlly = GetBool(ExtendedLances_EnableForEmployerAlly);
      if (Has(ExtendedLances_EnableForHostileToAll)) settings.ExtendedLances.EnableForHostileToAll = GetBool(ExtendedLances_EnableForHostileToAll);
      if (Has(ExtendedLances_EnableForNeutralToAll)) settings.ExtendedLances.EnableForNeutralToAll = GetBool(ExtendedLances_EnableForNeutralToAll);
      if (Has(ExtendedLances_Autofill)) settings.ExtendedLances.Autofill = GetBool(ExtendedLances_Autofill);
      if (Has(ExtendedLances_AutofillType)) settings.ExtendedLances.AutofillType = GetString(ExtendedLances_AutofillType);
      if (Has(ExtendedLances_AutofillUnitCopyType)) settings.ExtendedLances.AutofillUnitCopyType = GetString(ExtendedLances_AutofillUnitCopyType);
      if (Has(ExtendedLances_AutofillStartingFromContractDifficulty)) settings.ExtendedLances.AutofillStartingFromContractDifficulty = GetInt(ExtendedLances_AutofillStartingFromContractDifficulty);
      if (Has(ExtendedLances_IncludeContractTypes)) settings.ExtendedLances.IncludeContractTypes = GetStringList(ExtendedLances_IncludeContractTypes);
      if (Has(ExtendedLances_ExcludeContractTypes)) settings.ExtendedLances.ExcludeContractTypes = GetStringList(ExtendedLances_ExcludeContractTypes);
      if (Has(ExtendedLances_SkipWhenTaggedWithAny)) settings.ExtendedLances.SkipWhenTaggedWithAny = GetStringList(ExtendedLances_SkipWhenTaggedWithAny);
      if (Has(ExtendedLances_SkipWhenTaggedWithAll)) settings.ExtendedLances.SkipWhenTaggedWithAll = GetStringList(ExtendedLances_SkipWhenTaggedWithAll);
      if (Has(ExtendedLances_SkipWhenExcludeTagsContain)) settings.ExtendedLances.SkipWhenExcludeTagsContain = GetStringList(ExtendedLances_SkipWhenExcludeTagsContain);
      if (Has(ExtendedLances_ForceLanceOverrideSizeWithTag)) settings.ExtendedLances.ForceLanceOverrideSizeWithTag = GetString(ExtendedLances_ForceLanceOverrideSizeWithTag);
      if (Has(ExtendedLances_ForceLanceDefSizeWithTag)) settings.ExtendedLances.ForceLanceDefSizeWithTag = GetString(ExtendedLances_ForceLanceDefSizeWithTag);

      if (Has(ExtendedLances_LanceSizes)) {
        JObject lanceSizesObject = (JObject)Properties.SelectToken(ExtendedLances_LanceSizes);
        Dictionary<string, List<ExtendedLance>> lanceSizes = lanceSizesObject.ToObject<Dictionary<string, List<ExtendedLance>>>();
        settings.ExtendedLances.LanceSizes = lanceSizes;
      }
    }

    private void LoadExtendedBoundaries(Settings settings) {
      if (Has(ExtendedBoundaries_Enable)) settings.ExtendedBoundaries.Enable = GetBool(ExtendedBoundaries_Enable);
      if (Has(ExtendedBoundaries_EnableForFlashpoints)) settings.ExtendedBoundaries.EnableForFlashpoints = GetBool(ExtendedBoundaries_EnableForFlashpoints);
      if (Has(ExtendedBoundaries_EnableForStory)) settings.ExtendedBoundaries.EnableForStory = GetBool(ExtendedBoundaries_EnableForStory);
      if (Has(ExtendedBoundaries_IncludeContractTypes)) settings.ExtendedBoundaries.IncludeContractTypes = GetStringList(ExtendedBoundaries_IncludeContractTypes);
      if (Has(ExtendedBoundaries_ExcludeContractTypes)) settings.ExtendedBoundaries.ExcludeContractTypes = GetStringList(ExtendedBoundaries_ExcludeContractTypes);
      if (Has(ExtendedBoundaries_IncreaseBoundarySizeByPercentage)) settings.ExtendedBoundaries.IncreaseBoundarySizeByPercentage = GetFloat(ExtendedBoundaries_IncreaseBoundarySizeByPercentage);

      if (Has(ExtendedBoundaries_Overrides)) {
        JArray boundaryOverridesObject = (JArray)Properties.SelectToken(ExtendedBoundaries_Overrides);
        List<ExtendedBoundariesOverride> boundaryOverrides = boundaryOverridesObject.ToObject<List<ExtendedBoundariesOverride>>();
        settings.ExtendedBoundaries.Overrides = boundaryOverrides;
      }
    }

    public void LoadDynamicWithdraw(Settings settings) {
      if (Has(DynamicWithdraw_Enable)) settings.DynamicWithdraw.Enable = GetBool(DynamicWithdraw_Enable);
      if (Has(DynamicWithdraw_EnableForFlashpoints)) settings.DynamicWithdraw.EnableForFlashpoints = GetBool(DynamicWithdraw_EnableForFlashpoints);
      if (Has(DynamicWithdraw_EnableForStory)) settings.DynamicWithdraw.EnableForStory = GetBool(DynamicWithdraw_EnableForStory);
      if (Has(DynamicWithdraw_IncludeContractTypes)) settings.DynamicWithdraw.IncludeContractTypes = GetStringList(DynamicWithdraw_IncludeContractTypes);
      if (Has(DynamicWithdraw_ExcludeContractTypes)) settings.DynamicWithdraw.ExcludeContractTypes = GetStringList(DynamicWithdraw_ExcludeContractTypes);
      if (Has(DynamicWithdraw_MinDistanceForZone)) settings.DynamicWithdraw.MinDistanceForZone = GetInt(DynamicWithdraw_MinDistanceForZone);
      if (Has(DynamicWithdraw_MaxDistanceForZone)) settings.DynamicWithdraw.MaxDistanceForZone = GetInt(DynamicWithdraw_MaxDistanceForZone);
      if (Has(DynamicWithdraw_DisorderlyWithdrawalCompatibility)) settings.DynamicWithdraw.DisorderlyWithdrawalCompatibility = GetBool(DynamicWithdraw_DisorderlyWithdrawalCompatibility);
    }

    public void LoadAI(Settings settings) {
      if (Has(AI_FollowPlayer_Pathfinding)) settings.AiSettings.FollowAiSettings.Pathfinding = GetString(AI_FollowPlayer_Pathfinding);
      if (Has(AI_FollowPlayer_Target)) settings.AiSettings.FollowAiSettings.Target = GetString(AI_FollowPlayer_Target);
      if (Has(AI_FollowPlayer_StopWhen)) settings.AiSettings.FollowAiSettings.StopWhen = GetString(AI_FollowPlayer_StopWhen);
      if (Has(AI_FollowPlayer_MaxDistanceFromTargetBeforeSprinting)) settings.AiSettings.FollowAiSettings.MaxDistanceFromTargetBeforeSprinting = GetInt(AI_FollowPlayer_MaxDistanceFromTargetBeforeSprinting);
      if (Has(AI_FollowPlayer_TargetZoneRadius)) settings.AiSettings.FollowAiSettings.TargetZoneRadius = GetInt(AI_FollowPlayer_TargetZoneRadius);
      if (Has(AI_FollowPlayer_TimeToWaitForPathfinding)) settings.AiSettings.FollowAiSettings.TimeToWaitForPathfinding = GetInt(AI_FollowPlayer_TimeToWaitForPathfinding);
      if (Has(AI_FollowPlayer_TicksToWaitForPathfinding)) settings.AiSettings.FollowAiSettings.TicksToWaitForPathfinding = GetInt(AI_FollowPlayer_TicksToWaitForPathfinding);
    }

    public void LoadSpawners(Settings settings) {
      if (Has(Spawners_SpawnLanceAtEdgeBoundary_MinBuffer)) settings.Spawners.SpawnLanceAtBoundary.MinBuffer = GetInt(Spawners_SpawnLanceAtEdgeBoundary_MinBuffer);
      if (Has(Spawners_SpawnLanceAtEdgeBoundary_MaxBuffer)) settings.Spawners.SpawnLanceAtBoundary.MaxBuffer = GetInt(Spawners_SpawnLanceAtEdgeBoundary_MaxBuffer);
    }

    public void LoadCustomData(Settings settings) {
      if (Has(CustomData_Search)) settings.CustomData.Search = GetBool(CustomData_Search);
      if (Has(CustomData_SearchType)) settings.CustomData.SearchType = GetString(CustomData_SearchType);
    }

    public void LoadMisc(Settings settings) {
      if (Has(Misc_LanceSelectionDivergenceOverride_Enable)) settings.Misc.LanceSelectionDivergenceOverride.Enable = GetBool(Misc_LanceSelectionDivergenceOverride_Enable);
      if (Has(Misc_LanceSelectionDivergenceOverride_Divergence)) settings.Misc.LanceSelectionDivergenceOverride.Divergence = GetInt(Misc_LanceSelectionDivergenceOverride_Divergence);
    }
  }
}