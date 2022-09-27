using UnityEngine;

using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Data;

using HBS.Data;

using MissionControl.Data;
using MissionControl.Config;
using MissionControl.Messages;
using MissionControl.Utils;

namespace MissionControl {
  public class DataManager {
    private static DataManager instance;
    public static DataManager Instance {
      get {
        if (instance == null) instance = new DataManager();
        return instance;
      }
    }

    private string ModTekVersion { get; set; }
    private bool IsModTekVersion1 { get; set; } = true;

    public bool HasLoadedDeferredDefs { get; private set; } = false;
    public string ModDirectory { get; private set; }
    private Dictionary<string, MLanceOverride> LanceOverrides { get; set; } = new Dictionary<string, MLanceOverride>();

    private List<string> Genders = new List<string> { "Male", "Female", "Unspecified" };
    private Dictionary<string, Dictionary<string, List<string>>> FirstNames = new Dictionary<string, Dictionary<string, List<string>>>(); // e.g. <Male, <FactionName, [list of names]>>
    private Dictionary<string, List<string>> LastNames = new Dictionary<string, List<string>>();  // e.g. <All, [list of names]>
    private Dictionary<string, List<string>> Ranks = new Dictionary<string, List<string>>();      // e.g. <FactionName, [list of ranks]>
    private Dictionary<string, List<string>> Portraits = new Dictionary<string, List<string>>();  // e.g. <Male, [list of male portraits]
    public Dictionary<string, Sprite> GeneratedPortraits = new Dictionary<string, Sprite>();     // e.g. <{pilotDef.Description.Icon}, Sprite>

    public Dictionary<string, Dictionary<string, JObject>> AvailableCustomContractTypeBuilds { get; set; } = new Dictionary<string, Dictionary<string, JObject>>();
    private Dictionary<string, List<ContractTypeValue>> AvailableCustomContractTypes = new Dictionary<string, List<ContractTypeValue>>();
    public Dictionary<string, ContractTypeMetadata> AvailableContractTypeMetadata = new Dictionary<string, ContractTypeMetadata>();

    private Dictionary<string, Dictionary<string, List<string>>> Dialogue = new Dictionary<string, Dictionary<string, List<string>>>();

    JsonSerializerSettings serialiserSettings = new JsonSerializerSettings() {
      TypeNameHandling = TypeNameHandling.All,
      Culture = CultureInfo.InvariantCulture
    };

    private DataManager() { }

    public void Init(string modDirectory) {
      ModDirectory = modDirectory;
      LoadSettingsOverrides();
      LoadLanceOverrides();
      LoadContractConfigOverrides();
      LoadRuntimeCastData();
      LoadDialogueData();
      InjectMessageScopes();

      ModTekVersion = GetAssemblyByName("ModTek").GetName().Version.ToString();
      IsModTekVersion1 = ModTekVersion.Substring(0, 1) == "1" || ModTekVersion.Substring(0, 1) == "0";
    }

    public void SubscribeDeferredDefs() {
      RunActionWhenDataManagerIsOrBecomesIdle(LoadDeferredDefs);
    }

    private void RunActionWhenDataManagerIsOrBecomesIdle(Action action, Action stillLoadingAction = null) {
      var dataManager = UnityGameInstance.BattleTechGame.DataManager;
      if (dataManager.IsLoading) {
        Main.LogDebug($"[DataManager.RunActionWhenDataManagerIsOrBecomesIdle] DataManager IS currently loading. Deferring load.");
        stillLoadingAction?.Invoke();
        UnityGameInstance.BattleTechGame.MessageCenter.AddFiniteSubscriber(
            MessageCenterMessageType.DataManagerLoadCompleteMessage,
            _ => {
              if (dataManager.IsLoading) {
                stillLoadingAction?.Invoke();
                return false;
              }
              action();
              return true;
            }
        );
      } else {
        action();
      }
    }

    private void LoadDeferredDefs() {
      Main.LogDebug($"[DataManager.LoadDeferredDefs] Loading Deferred Defs");
      Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

      // LoadVehicleDefs();
      LoadMechDefs();
      LoadPilotDefs();
      LoadCustomContractTypeBuilds();

      if (IsModTekVersion1) {
        Main.LogDebug($"[DataManager.SubscribeDeferredDefs] ModTek v1 version so loading EncounterLayers manually.");
        LoadCustomContractTypesV1();
      } else {
        Main.LogDebug($"[DataManager.SubscribeDeferredDefs] ModTek v2 version so EncounterLayers already loaded by ModTek.");
        LoadCustomContractTypes();
      }

      HasLoadedDeferredDefs = true;
    }

    private Assembly GetAssemblyByName(string name) {
      return AppDomain.CurrentDomain.GetAssemblies().
             SingleOrDefault(assembly => assembly.GetName().Name == name);
    }

    private void LoadContractConfigOverrides() {
      string contractsDirectory = $"{ModDirectory}/config/Contracts/";
      string flashpointsDirectory = $"{ModDirectory}/config/Flashpoints/";

      if (Directory.Exists(contractsDirectory)) {
        LoadContractConfigOverridesByDirectory(contractsDirectory);
      }

      if (Directory.Exists(flashpointsDirectory)) {
        LoadContractConfigOverridesByDirectory(flashpointsDirectory);
      }
    }

    private void LoadContractConfigOverridesByDirectory(string directory) {
      foreach (string file in Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories)) {
        string rawSettingsOverride = File.ReadAllText(file);
        string fileName = Path.GetFileNameWithoutExtension(file.Substring(file.LastIndexOf("/")));
        Main.LogDebug($"[DataManager.LoadContractConfigOverrides] Loading contract (and flashpoint contract) settings override for '{fileName}'");
        JObject settingsOverrides = JsonConvert.DeserializeObject<JObject>(rawSettingsOverride, serialiserSettings);
        Main.Settings.ContractSettingsOverrides[fileName] = new ContractSettingsOverrides() { Properties = settingsOverrides };
      }
    }

    private void LoadSettingsOverrides() {
      Settings settings = Main.Settings;

      SettingsOverride modpackSettingsOverrides = new SettingsOverride(Main.Path, "modpack");
      modpackSettingsOverrides.LoadOverrides(settings);

      SettingsOverride userSettingsOverrides = new SettingsOverride(Main.Path, "user");
      userSettingsOverrides.LoadOverrides(settings);
    }

    private void LoadCustomContractTypeBuilds() {
      foreach (string directory in Directory.GetDirectories($"{ModDirectory}/contractTypeBuilds/")) {
        if (File.Exists($"{directory}/common.jsonc")) {
          string contractTypeBuildCommonSource = File.ReadAllText($"{directory}/common.jsonc");
          JObject contractTypeCommonBuild = JsonConvert.DeserializeObject<JObject>(contractTypeBuildCommonSource, serialiserSettings);
          string contractTypeName = (string)contractTypeCommonBuild["Key"];
          Main.LogDebug($"[DataManager.LoadCustomContractTypeBuilds] Loading contract type build '{contractTypeName}'");

          JObject metadataObject = contractTypeCommonBuild.ContainsKey("Metadata") ? (JObject)contractTypeCommonBuild["Metadata"] : null;
          ContractTypeMetadata metadata = metadataObject.ToObject<ContractTypeMetadata>();
          AvailableContractTypeMetadata.Add(contractTypeName, metadata);
          if (metadata == null) {
            Main.Logger.LogError($"[VIOLATION] !!!! CONTRACT TYPE '{contractTypeName}' HAS NO METADATA. THIS IS INVALID! ALL CONTRACT TYPES SHOULD CLEARLY DISPLAY METADATA INCLUDING AUTHORS AND CONTRIBUTORS !!!!");
          } else {
            Main.Logger.Log($"[DataManager] Loaded metadata for '{contractTypeName}'");
          }

          if (AvailableCustomContractTypeBuilds.ContainsKey(contractTypeName)) {
            Main.Logger.LogError($"[DataManager.LoadCustomContractTypeBuilds] Duplicate contract type build key of '{contractTypeName}' was detected. FATAL ERROR!!!");
            return;
          }

          Dictionary<string, JObject> contractTypeMapBuilds = new Dictionary<string, JObject>();
          AvailableCustomContractTypeBuilds.Add(contractTypeName, contractTypeMapBuilds);

          foreach (string file in Directory.GetFiles(directory, "*.json*", SearchOption.AllDirectories)) {
            string contractTypeBuildMapSource = File.ReadAllText(file);
            JObject contractTypeMapBuild = JsonConvert.DeserializeObject<JObject>(contractTypeBuildMapSource, serialiserSettings);
            string fileName = Path.GetFileNameWithoutExtension(file.Substring(file.LastIndexOf("/")));

            if (fileName == "common" || contractTypeMapBuild.ContainsKey("EncounterLayerId")) {
              string encounterLayerId = (fileName == "common") ? fileName : (string)contractTypeMapBuild["EncounterLayerId"];
              Main.LogDebug($"[DataManager.LoadCustomContractTypeBuilds] Loaded contract type map build '{contractTypeName}/{fileName}' with encounterLayerId '{encounterLayerId}'");

              if (contractTypeMapBuilds.ContainsKey(encounterLayerId)) {
                Main.Logger.LogError($"[DataManager.LoadCustomContractTypeBuilds] Duplicate contract type override build key of '{encounterLayerId}' in file '{file}' was detected. FATAL ERROR!!!");
                return;
              }

              contractTypeMapBuilds.Add(encounterLayerId, contractTypeMapBuild);
            } else {
              Main.Logger.LogError($"[DataManager.LoadCustomContractTypeBuilds] Unable to load contract type map build file '{fileName}' for contract type '{contractTypeName}' because no 'EncounterLayerId' exists");
            }
          }
        } else {
          Main.Logger.LogWarning($"[DataManager.LoadCustomContractTypeBuilds] Directory exists for contract type but no common.jsonc exists. Probably bad data or WIP contract type build - {directory}");
        }
      }
    }

    public List<string> GetCustomContractTypes() {
      List<string> customContractTypeNames = new List<string>();
      MetadataDatabase mdd = MetadataDatabase.Instance;
      List<ContractType_MDD> contractTypes = mdd.GetCustomContractTypes();

      foreach (ContractType_MDD contractType in contractTypes) {
        customContractTypeNames.Add(contractType.Name);
      }

      return customContractTypeNames;
    }


    public List<string> GetStoryContractTypes() {
      List<string> storyContractTypeNames = new List<string>();
      MetadataDatabase mdd = MetadataDatabase.Instance;
      List<ContractType_MDD> contractTypes = mdd.GetStoryContractTypes();

      foreach (ContractType_MDD contractType in contractTypes) {
        storyContractTypeNames.Add(contractType.Name);
      }

      return storyContractTypeNames;
    }

    // *********************
    // ***** MODTEK V2 *****
    // *********************
    private void LoadCustomContractTypes() {
      MetadataDatabase mdd = MetadataDatabase.Instance;
      List<ContractType_MDD> contractTypes = mdd.GetCustomContractTypes();
      Main.LogDebug($"[DataManager.LoadCustomContractTypes] Loaded '{contractTypes.Count}' custom contract type(s)");

      foreach (ContractType_MDD contractType in contractTypes) {
        AddContractType(mdd, contractType);
      }
    }

    private void AddContractType(MetadataDatabase mdd, ContractType_MDD contractTypeMDD) {
      ContractTypeValue contractTypeValue = ContractTypeEnumeration.Instance.CreateEnumValueFromDatabase(mdd, contractTypeMDD.EnumValueRow);

      if (!AvailableCustomContractTypes.ContainsKey(contractTypeValue.Name)) AvailableCustomContractTypes.Add(contractTypeValue.Name, new List<ContractTypeValue>());
      Main.LogDebug($"[DataManager.AddContractType] Adding custom contract type: {contractTypeValue.Name}");
      AvailableCustomContractTypes[contractTypeValue.Name].Add(contractTypeValue);
    }

    public bool IsCustomContractType(string contractTypeName) {
      foreach (string name in AvailableCustomContractTypes.Keys) {
        if (name == contractTypeName) return true;
      }
      return false;
    }

    // *********************
    // ***** MODTEK V1 *****
    // *********************
    private void LoadCustomContractTypesV1() {
      new Thread(new ThreadStart(this.WriteMDDToDiskV1)).Start();
      UnityGameInstance.Instance.StartCoroutine(ReportOnLoadedCustomContractTypeV1());
    }

    IEnumerator ReportOnLoadedCustomContractTypeV1() {
      yield return new WaitForSeconds(5); // A largish amount of time to ensure contract types have been saved by then
      MetadataDatabase mdd = MetadataDatabase.Instance;
      List<ContractType_MDD> contractTypes = mdd.GetCustomContractTypes();
      Main.LogDebug($"[DataManager.LoadCustomContractTypes] Loaded '{contractTypes.Count}' custom contract type(s)");
    }

    private void WriteMDDToDiskV1() {
      Thread.Sleep(Main.Settings.ContractTypeLoaderWait);
      MetadataDatabase mdd = MetadataDatabase.Instance;
      List<ContractType_MDD> contractTypes = mdd.GetCustomContractTypes();
      foreach (ContractType_MDD contractType in contractTypes) {
        AddContractType(mdd, contractType);
        LoadEncounterLayersV1(contractType.Name);
      }
      MetadataDatabase.Instance.WriteInMemoryDBToDisk();
    }

    private void LoadEncounterLayersV1(string name) {
      foreach (string file in Directory.GetFiles($"{ModDirectory}/overrides/encounterLayers/{name.ToLower()}", "*.json", SearchOption.AllDirectories)) {
        Main.LogDebug($"[DataManager.LoadCustomContractTypes] Loading '{file.Substring(file.LastIndexOf('/') + 1)}' custom encounter layer");
        string encounterLayer = File.ReadAllText(file);
        EncounterLayer encounterLayerData = JsonConvert.DeserializeObject<EncounterLayer>(encounterLayer, serialiserSettings);

        MetadataDatabase.Instance.InsertOrUpdateEncounterLayer(encounterLayerData);
      }
    }

    // *********************

    private void LoadLanceOverrides() {
      foreach (string file in Directory.GetFiles($"{ModDirectory}/lances", "*.json", SearchOption.AllDirectories)) {
        string lanceData = File.ReadAllText(file);
        MLanceOverrideData lanceOverrideData = JsonConvert.DeserializeObject<MLanceOverrideData>(lanceData, serialiserSettings);

        if (lanceOverrideData.LanceKey != null) {
          LanceOverrides.Add(lanceOverrideData.LanceKey, new MLanceOverride(lanceOverrideData));

          Main.Logger.Log($"[DataManager] Loaded lance override '{lanceOverrideData.LanceKey}'");

          /*
          if (Main.Settings.DebugMode) {
            Main.Logger.Log($"[DataManager] Lance def id '{lanceOverrideData.LanceDefId}'");
            Main.Logger.Log($"[DataManager] Lance Tag Set items '{string.Join(",", lanceOverrideData.LanceTagSet.Items)}' and source file '{lanceOverrideData.LanceTagSet.TagSetSourceFile}'");
            Main.Logger.Log($"[DataManager] Lance Excluded Tag Set items '{string.Join(",", lanceOverrideData.LanceExcludedTagSet.Items)}' and source file '{lanceOverrideData.LanceExcludedTagSet.TagSetSourceFile}'");
            Main.Logger.Log($"[DataManager] Spawn Effect Tags items '{string.Join(",", lanceOverrideData.LanceTagSet.Items)}' and source file '{lanceOverrideData.LanceTagSet.TagSetSourceFile}'");

            foreach (MUnitSpawnPointOverrideData unitSpawnPointOverrideData in lanceOverrideData.UnitSpawnPointOverride) {
              Main.Logger.Log($"[DataManager] Unit type '{unitSpawnPointOverrideData.UnitType}'");
              Main.Logger.Log($"[DataManager] Unit def id '{unitSpawnPointOverrideData.UnitDefId}'");
              Main.Logger.Log($"[DataManager] Unit Tag Set items '{string.Join(",", unitSpawnPointOverrideData.UnitTagSet.Items)}' and source file '{unitSpawnPointOverrideData.UnitTagSet.TagSetSourceFile}'");
              Main.Logger.Log($"[DataManager] Unit Excluded Tag Set items '{string.Join(",", unitSpawnPointOverrideData.UnitExcludedTagSet.Items)}' and source file '{unitSpawnPointOverrideData.UnitExcludedTagSet.TagSetSourceFile}'");
              Main.Logger.Log($"[DataManager] Spawn Effect Tags items '{string.Join(",", unitSpawnPointOverrideData.SpawnEffectTags.Items)}' and source file '{unitSpawnPointOverrideData.SpawnEffectTags.TagSetSourceFile}'");
              Main.Logger.Log($"[DataManager] Pilot def id '{unitSpawnPointOverrideData.PilotDefId}'");
              Main.Logger.Log($"[DataManager] Pilot Tag Set items '{string.Join(",", unitSpawnPointOverrideData.PilotTagSet.Items)}' and source file '{unitSpawnPointOverrideData.PilotTagSet.TagSetSourceFile}'");
              Main.Logger.Log($"[DataManager] Pilot Excluded Tag Set items '{string.Join(",", unitSpawnPointOverrideData.PilotExcludedTagSet.Items)}' and source file '{unitSpawnPointOverrideData.PilotExcludedTagSet.TagSetSourceFile}'");
            }
          }
          */
        } else {
          Main.Logger.LogError($"[DataManager] Json format is wrong for file '{file}'. Read the documentation on the lance override format.");
        }
      }
    }

    private bool LoadDirectLanceReference(string key) {
      BattleTech.Data.DataManager dataManager = UnityGameInstance.BattleTechGame.DataManager;
      if (dataManager.ResourceEntryExists(BattleTechResourceType.LanceDef, key)) {
        Main.Logger.Log($"[LoadDirectLanceReference] Lance definition of '{key}' exists but has not been loaded yet. Loading it. [Experimental: If a crash occurs after this please report it]");
        RequestResourcesAndProcess(BattleTechResourceType.LanceDef, key);
        return true;
      }

      Main.Logger.Log($"[LoadDirectLanceReference] No direct lance reference found for '{key}'");
      return false;
    }

    public bool DoesLanceOverrideExist(string key) {
      if (LanceOverrides.ContainsKey(key)) return true;
      if (UnityGameInstance.BattleTechGame.DataManager.LanceDefs.Exists(key)) return true;
      return LoadDirectLanceReference(key);
    }

    public MLanceOverride GetLanceOverride(string key) {
      IDataItemStore<string, LanceDef> lanceDefs = UnityGameInstance.BattleTechGame.DataManager.LanceDefs;

      if (LanceOverrides.ContainsKey(key)) {
        Main.Logger.Log($"[GetLanceOverride] Found a lance override for '{key}'");
        return LanceOverrides[key].Copy();
      }

      LanceDef lanceDef = null;
      lanceDefs.TryGet(key, out lanceDef);
      if (lanceDef != null) {
        MLanceOverride lanceOverride = new MLanceOverride(lanceDef);
        LanceOverrides.Add(lanceOverride.lanceDefId, lanceOverride);
        Main.Logger.Log($"[GetLanceOverride] Found a lance def for '{key}', creating and caching a lance override for it. Using defaults of 'adjustedDifficulty' of '0' and no 'spawnEffectTags'");
        return lanceOverride.Copy();
      } else {
        Main.Logger.Log($"[GetLanceOverride] No loaded LanceDef was found for '{key}'. Attempting to load the LanceDef.");
        lanceDef = BattleTechResourceLoader.LoadDefFromId<LanceDef>(key, BattleTechResourceType.LanceDef);
        DataManager.Instance.RequestResourcesAndProcess(BattleTechResourceType.LanceDef, key);

        if (lanceDef != null) {
          MLanceOverride lanceOverride = new MLanceOverride(lanceDef);
          LanceOverrides.Add(lanceOverride.lanceDefId, lanceOverride);
          Main.Logger.Log($"[GetLanceOverride] Load succeeded. Found a lance def for '{key}', creating and caching a lance override for it. Using defaults of 'adjustedDifficulty' of '0' and no 'spawnEffectTags'");
          return lanceOverride.Copy();
        }
      }

      Main.Logger.LogError($"[GetLanceOverride] No MC Lance or LanceDef found with key '{key}'. This is a case sensitive search.'");
      return null;
    }

    // public void LoadVehicleDefs() {
    // RequestResourcesAndProcess(BattleTechResourceType.VehicleDef, "vehicledef_DEMOLISHER");
    // }

    public void LoadMechDefs() {
      RequestResourcesAndProcess(BattleTechResourceType.MechDef, "mechdef_spider_SDR-5V");
    }

    public void LoadPilotDefs() {
      RequestResourcesAndProcess(BattleTechResourceType.PilotDef, UnitSpawnPointGameLogic.PilotDef_Default);
    }

    /* RUNTIME CREW NAMES */
    public void LoadRuntimeCastData() {
      LoadCastFirstNames();
      LoadCastLastNames();
      LoadCastRanks();
      LoadPortraits();
    }

    private void LoadCastFirstNames() {
      string firstNameJson = File.ReadAllText($"{ModDirectory}/cast/FirstNames.json");
      MCastFirstNames firstNames = JsonConvert.DeserializeObject<MCastFirstNames>(firstNameJson, serialiserSettings);
      this.FirstNames.Add("All", firstNames.All);
      this.FirstNames.Add("Male", firstNames.Male);
      this.FirstNames.Add("Female", firstNames.Female);
    }

    private void LoadCastLastNames() {
      string lastNameJson = File.ReadAllText($"{ModDirectory}/cast/LastNames.json");
      this.LastNames = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(lastNameJson, serialiserSettings);
    }

    private void LoadCastRanks() {
      string rankJson = File.ReadAllText($"{ModDirectory}/cast/Ranks.json");
      this.Ranks = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(rankJson, serialiserSettings);
    }

    private void LoadPortraits() {
      string portraitJson = File.ReadAllText($"{ModDirectory}/cast/Portraits.json");
      this.Portraits = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(portraitJson, serialiserSettings);
    }

    public string GetRandomGender() {
      return Genders[UnityEngine.Random.Range(0, Genders.Count)];
    }

    public string GetRandomFirstName(string gender, string factionKey) {
      List<string> names = new List<string>();
      names.AddRange(this.FirstNames["All"]["All"]);
      if (gender == "Male" || gender == "Female") {
        Dictionary<string, List<string>> genderNames = this.FirstNames[gender];
        names.AddRange(genderNames["All"]);
        if (genderNames.ContainsKey(factionKey)) names.AddRange(genderNames[factionKey]);
      }

      return names[UnityEngine.Random.Range(0, names.Count)];
    }

    public string GetRandomLastName(string factionKey) {
      List<string> names = null;
      float chance = UnityEngine.Random.Range(0f, 100f);
      bool useFactionName = false;
      if (chance < 75) useFactionName = true;

      if (this.LastNames.ContainsKey(factionKey) && this.LastNames[factionKey].Count > 0 && useFactionName) {
        names = this.LastNames[factionKey];
      } else {
        names = this.LastNames["All"];
      }

      return names[UnityEngine.Random.Range(0, names.Count)];
    }

    public string GetRandomRank(string factionKey) {
      List<string> ranks = new List<string>();

      if (this.Ranks.ContainsKey(factionKey) && this.Ranks[factionKey].Count > 0) {
        ranks.AddRange(this.Ranks[factionKey]);
      } else {
        ranks.AddRange(this.Ranks["Fallback"]);
      }

      return ranks[UnityEngine.Random.Range(0, ranks.Count)];
    }

    public string GetRandomPortraitPath(string gender) {
      List<string> portraits = new List<string>();
      portraits.AddRange(this.Portraits["All"]);
      if (this.Portraits.ContainsKey(gender)) portraits.AddRange(this.Portraits[gender]);

      return portraits[UnityEngine.Random.Range(0, portraits.Count)];
    }

    public void LoadDialogueData() {
      string allyDropJson = File.ReadAllText($"{ModDirectory}/dialogue/AllyDrop.json");
      Dialogue.Add("AllyDrop", JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(allyDropJson, serialiserSettings));
    }

    public string GetRandomDialogue(string type, string contractType, string contractSubType) {
      List<string> dialogues = new List<string>();

      if (Dialogue.ContainsKey(type)) {
        Dictionary<string, List<string>> dialogueOfType = Dialogue[type];

        if (dialogueOfType.ContainsKey("All")) dialogues.AddRange(dialogueOfType["All"]);
        if (dialogueOfType.ContainsKey(contractType)) dialogues.AddRange(dialogueOfType[contractType]);
        if (dialogueOfType.ContainsKey(contractSubType)) dialogues.AddRange(dialogueOfType[contractSubType]);

        return dialogues[UnityEngine.Random.Range(0, dialogues.Count)];
      }

      return "Let's get them, Commander!";
    }

    public JObject GetAvailableCustomContractTypeBuilds(string contractTypeName, string encounterLayerId) {
      if (AvailableCustomContractTypeBuilds.ContainsKey(contractTypeName)) {
        Dictionary<string, JObject> contractTypeMapBuilds = AvailableCustomContractTypeBuilds[contractTypeName];

        if (!contractTypeMapBuilds.ContainsKey("common")) {
          Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] No common.json for '{contractTypeName}'. It's mandatory for custom contract types.");
          return null;
        }

        JObject commonBuild = (JObject)contractTypeMapBuilds["common"].DeepClone();

        if (!contractTypeMapBuilds.ContainsKey(encounterLayerId)) {
          Main.Logger.LogWarning($"[GetAvailableCustomContractTypeBuilds] No map specific build for '{contractTypeName}' and '{encounterLayerId}'. Using only the common build.");
        } else {
          JObject mapBuild = contractTypeMapBuilds[encounterLayerId];

          Main.LogDebug($"[GetAvailableCustomContractTypeBuilds] Merging common build for '{contractTypeName}' with map build '{encounterLayerId}'");

          JArray overridesArray = (JArray)mapBuild["Overrides"];
          if (overridesArray == null) {
            Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] No overrides found in contract type map build file for contract type '{contractTypeName}' with encounterLayerId '{encounterLayerId}'");
            return null;
          }

          foreach (JObject ovr in overridesArray.Children<JObject>()) {
            string path = (string)ovr["Path"];
            string action = (string)ovr["Action"];
            JToken value = ovr["Value"];

            JToken token = commonBuild.SelectToken(path);
            if (token == null) {
              Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] No match found for path '{path}'");
              return null;
            }

            if (action == "Replace") {
              Main.LogDebug($"[GetAvailableCustomContractTypeBuilds] Replacing value for path '{path}'");
              token.Replace(value);
            } else if (action == "Remove") {
              Main.LogDebug($"[GetAvailableCustomContractTypeBuilds] Removing value for path '{path}'");
              token.Remove();
            } else if (action == "ObjectMerge") {
              Main.LogDebug($"[GetAvailableCustomContractTypeBuilds] Object merging value for path '{path}'");
              JObject objTarget = (JObject)token;
              JObject objValue = (JObject)value;

              objTarget.Merge(objValue);
            }
          }
        }

        return commonBuild;
      } else {
        Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] Requested custom contract type for '{contractTypeName}' and encounterLayerId '{encounterLayerId}' has no custom contract type loaded.");
      }
      return null;
    }

    public void ResetBetweenContracts() {
      DataManager.Instance.GeneratedPortraits.Clear();
    }

    public void RequestResourcesAndProcess(BattleTechResourceType resourceType, string resourceId, bool filterByOwnership = false) {
      LoadRequest loadRequest = UnityGameInstance.BattleTechGame.DataManager.CreateLoadRequest(delegate (LoadRequest request) {
        Main.LogDebug($"[RequestResourcesAndProcess] Finished load request for {resourceId}");
      }, filterByOwnership);
      loadRequest.AddBlindLoadRequest(resourceType, resourceId);

      // Without 1000u this does not work for my usecase
      // Data loading has been updated for the Main Menu loads so it no longer should cause deadlocks
      loadRequest.ProcessRequests(1000u);
    }

    public DateTime? GetSimGameCurrentDate() {
      DateTime? result = null;
      if (UnityGameInstance.BattleTechGame != null && UnityGameInstance.BattleTechGame.Simulation != null) {
        result = new DateTime?(UnityGameInstance.BattleTechGame.Simulation.CurrentDate);
      }
      return result;
    }

    /*
      This adds any custom messages to the message center scopes
      Without this, when leaving combat (restarting or exiting) an error will occur
    */
    public void InjectMessageScopes() {
      Dictionary<MessageCenterMessageType, MessageCenter.MessageScope> messageScopes = (Dictionary<MessageCenterMessageType, MessageCenter.MessageScope>)ReflectionHelper.GetPrivateStaticField(typeof(MessageCenter), "messageScopes");
      MessageTypes[] customMessageTypes = (MessageTypes[])Enum.GetValues(typeof(MessageTypes));
      for (int i = 0; i < customMessageTypes.Length; i++) {
        Main.LogDebug($"[InjectMessageScopes] Injecting custom message {customMessageTypes.ToString()} into 'MessageCenter.MessageScope.CombatGame'");
        messageScopes.Add((MessageCenterMessageType)customMessageTypes[i], MessageCenter.MessageScope.CombatGame);
      }
    }
  }
}