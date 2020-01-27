using System;
using System.IO;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Data;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Data;

using MissionControl.Data;
using MissionControl.Logic;
using MissionControl.Messages;
using MissionControl.Utils;

using Harmony;

namespace MissionControl {
  public class DataManager {
    private static DataManager instance;
    public static DataManager Instance {
      get {
        if (instance == null) instance = new DataManager();
        return instance;
      }
    }

    public bool HasLoadedDeferredDefs { get; private set; } = false;
    public string ModDirectory { get; private set; }
    private Dictionary<string, MLanceOverride> LanceOverrides { get; set; } = new Dictionary<string, MLanceOverride>();

    private List<string> Genders = new List<string> { "Male", "Female", "Unspecified" };
    private Dictionary<string, Dictionary<string, List<string>>> FirstNames = new Dictionary<string, Dictionary<string, List<string>>>(); // e.g. <Male, <FactionName, [list of names]>>
    private Dictionary<string, List<string>> LastNames = new Dictionary<string, List<string>>();  // e.g. <All, [list of names]>
    private Dictionary<string, List<string>> Ranks = new Dictionary<string, List<string>>();      // e.g. <FactionName, [list of ranks]>
    private Dictionary<string, List<string>> Portraits = new Dictionary<string, List<string>>();  // e.g. <Male, [list of male portraits]

    public Dictionary<string, Dictionary<string, JObject>> AvailableCustomContractTypeBuilds { get; set; } = new Dictionary<string, Dictionary<string, JObject>>();
    private Dictionary<string, List<ContractTypeValue>> AvailableCustomContractTypes = new Dictionary<string, List<ContractTypeValue>>();

    private Dictionary<string, Dictionary<string, List<string>>> Dialogue = new Dictionary<string, Dictionary<string, List<string>>>();

    private DataManager() { }

    public void Init(string modDirectory) {
      ModDirectory = modDirectory;
      LoadLanceOverrides();
      LoadRuntimeCastData();
      LoadDialogueData();
      InjectMessageScopes();
    }

    public void LoadDeferredDefs() {
      LoadVehicleDefs();
      LoadPilotDefs();
      LoadCustomContractTypeBuilds();
      LoadCustomContractTypes();
      HasLoadedDeferredDefs = true;
    }

    private void LoadCustomContractTypeBuilds() {
      foreach (string directory in Directory.GetDirectories($"{ModDirectory}/contractTypeBuilds/")) {
        string contractTypeBuildCommonSource = File.ReadAllText($"{directory}/common.jsonc");
        JObject contractTypeCommonBuild = JsonConvert.DeserializeObject<JObject>(contractTypeBuildCommonSource);
        Main.LogDebug($"[DataManager.LoadCustomContractTypeBuilds] Loaded contract type build '{contractTypeCommonBuild["Key"]}'");

        Dictionary<string, JObject> contractTypeMapBuilds = new Dictionary<string, JObject>();
        AvailableCustomContractTypeBuilds.Add(contractTypeCommonBuild["Key"].ToString(), contractTypeMapBuilds);

        foreach (string file in Directory.GetFiles(directory, "*.json*", SearchOption.AllDirectories)) {
          string contractTypeBuildMapSource = File.ReadAllText(file);
          JObject contractTypeMapBuild = JsonConvert.DeserializeObject<JObject>(contractTypeBuildMapSource);
          string key = Path.GetFileNameWithoutExtension(file.Substring(file.LastIndexOf("\\")));

          Main.LogDebug($"[DataManager.LoadCustomContractTypeBuilds] Loaded contract type map build '{key}'");

          contractTypeMapBuilds.Add(key, contractTypeMapBuild);
        }
      }
    }

    private void LoadCustomContractTypes() {
      MetadataDatabase mdd = MetadataDatabase.Instance;
      List<ContractType_MDD> contractTypes = mdd.GetCustomContractTypes();
      Main.LogDebug($"[DataManager.LoadCustomContractTypes] Loading '{contractTypes.Count}' custom contract type(s)");
      foreach (ContractType_MDD contractType in contractTypes) {
        AddContractType(mdd, contractType);
        LoadEncounterLayers(contractType.Name);
      }
      MetadataDatabase.Instance.WriteInMemoryDBToDisk();
    }

    private void AddContractType(MetadataDatabase mdd, ContractType_MDD contractTypeMDD) {
      ContractTypeValue contractTypeValue = ContractTypeEnumeration.Instance.CreateEnumValueFromDatabase(mdd, contractTypeMDD.EnumValueRow);

      if (!AvailableCustomContractTypes.ContainsKey(contractTypeValue.Name)) AvailableCustomContractTypes.Add(contractTypeValue.Name, new List<ContractTypeValue>());
      Main.LogDebug($"[DataManager.AddContractType] Adding custom contract type: {contractTypeValue.Name}");
      AvailableCustomContractTypes[contractTypeValue.Name].Add(contractTypeValue);
    }

    private void LoadEncounterLayers(string name) {
      foreach (string file in Directory.GetFiles($"{ModDirectory}/overrides/encounterLayers/{name.ToLower()}", "*.json", SearchOption.AllDirectories)) {
        Main.LogDebug($"[DataManager.LoadCustomContractTypes] Loading '{file}' custom encounter layer");
        string encounterLayer = File.ReadAllText(file);
        EncounterLayer encounterLayerData = JsonConvert.DeserializeObject<EncounterLayer>(encounterLayer);

        MetadataDatabase.Instance.InsertOrUpdateEncounterLayer(encounterLayerData);
      }
    }

    private void LoadLanceOverrides() {
      foreach (string file in Directory.GetFiles($"{ModDirectory}/lances", "*.json", SearchOption.AllDirectories)) {
        string lanceData = File.ReadAllText(file);
        MLanceOverrideData lanceOverrideData = JsonConvert.DeserializeObject<MLanceOverrideData>(lanceData);

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
          Main.Logger.LogError($"[DataManager] Json format is wrong. Read the documentation on the lance override format.");
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
        return LanceOverrides[key];
      }

      LanceDef lanceDef = null;
      lanceDefs.TryGet(key, out lanceDef);
      if (lanceDef != null) {
        MLanceOverride lanceOverride = new MLanceOverride(lanceDef);
        LanceOverrides.Add(lanceOverride.lanceDefId, lanceOverride);
        Main.Logger.Log($"[GetLanceOverride] Found a lance def for '{key}', creating and caching a lance override for it. Using defaults of 'adjustedDifficulty - 0' and no 'spawnEffectTags'");
        return lanceOverride;
      }

      return null;
    }

    public void LoadVehicleDefs() {
      RequestResourcesAndProcess(BattleTechResourceType.VehicleDef, "vehicledef_DEMOLISHER");
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
      MCastFirstNames firstNames = JsonConvert.DeserializeObject<MCastFirstNames>(firstNameJson);
      this.FirstNames.Add("All", firstNames.All);
      this.FirstNames.Add("Male", firstNames.Male);
      this.FirstNames.Add("Female", firstNames.Female);
    }

    private void LoadCastLastNames() {
      string lastNameJson = File.ReadAllText($"{ModDirectory}/cast/LastNames.json");
      this.LastNames = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(lastNameJson);
    }

    private void LoadCastRanks() {
      string rankJson = File.ReadAllText($"{ModDirectory}/cast/Ranks.json");
      this.Ranks = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(rankJson);
    }

    private void LoadPortraits() {
      string portraitJson = File.ReadAllText($"{ModDirectory}/cast/Portraits.json");
      this.Portraits = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(portraitJson);
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
      Dialogue.Add("AllyDrop", JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(allyDropJson));
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

    public JObject GetAvailableCustomContractTypeBuilds(string contractTypeName, string mapId) {
      if (AvailableCustomContractTypeBuilds.ContainsKey(contractTypeName)) {
        Dictionary<string, JObject> contractTypeMapBuilds = AvailableCustomContractTypeBuilds[contractTypeName];

        if (!contractTypeMapBuilds.ContainsKey("common")) {
          Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] No common.json for '{contractTypeName}'. It's mandatory for custom contract types.");
          return null;
        }

        JObject commonBuild = contractTypeMapBuilds["common"];

        if (!contractTypeMapBuilds.ContainsKey(mapId)) {
          Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] No map specific build for '{contractTypeName}' and '{mapId}'. Using only the common build.");
        } else {
          JObject mapBuild = contractTypeMapBuilds[mapId];

          Main.LogDebug($"[GetAvailableCustomContractTypeBuilds] Merging common build for '{contractTypeName}' with map build '{mapId}'");

          JArray overridesArray = (JArray)mapBuild["Overrides"];
          if (overridesArray == null) {
            Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] No overrides found in contract type map build file '{contractTypeName}/{mapId}'");
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
        Main.Logger.LogError($"[GetAvailableCustomContractTypeBuilds] Requested custom contract type for '{contractTypeName}' and map id '{mapId}' has no custom contract type loaded.");
      }
      return null;
    }

    public void Reset() {

    }

    public void RequestResourcesAndProcess(BattleTechResourceType resourceType, string resourceId, bool filterByOwnership = false) {
      LoadRequest loadRequest = UnityGameInstance.BattleTechGame.DataManager.CreateLoadRequest(delegate (LoadRequest request) {
        Main.LogDebug($"[RequestResourcesAndProcess] Finished load request for {resourceId}");
      }, filterByOwnership);
      loadRequest.AddBlindLoadRequest(resourceType, resourceId);
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