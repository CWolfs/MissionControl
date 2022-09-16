using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using System;
using System.Linq;
using System.Collections;

using TMPro;

using Harmony;

using BattleTech;
using BattleTech.UI;
using BattleTech.Save;
using BattleTech.UI.TMProWrapper;

using MissionControl.Patches;
using MissionControl.Data;

namespace MissionControl {
  public class UiManager {
    private static UiManager instance;
    public static UiManager Instance {
      get {
        if (instance == null) instance = new UiManager();
        return instance;
      }
    }

    public GameObject UIPool { get; set; }

    public bool ClickedQuickSkirmish { get; set; } = false;
    public bool ReadyToLoadQuickSkirmish { get; set; } = false;
    public bool ShouldPatchMainMenu { get; set; } = true;

    public bool WaitingToCreateContractTypeCredits { get; set; } = false;

    public void Init() {
      GameObject mcUIPool = new GameObject("MCUIPool");
      mcUIPool.transform.parent = GameObject.Find("UIManager").transform;
      GameObject.DontDestroyOnLoad(mcUIPool);
      UIPool = mcUIPool;
    }

    public void SetupQuickSkirmishMenu() {
      GameObject mainMenuScreenGo = GameObject.Find("uixPrfPanl_mainMenu-Screen_V3(Clone)");
      GameObject layoutMainNavigationGo = GameObject.Find("layout_main-nav");
      GameObject mainNavigationActions = layoutMainNavigationGo.FindRecursive("layout-actions-radioSet");
      GameObject skirmishButton = mainNavigationActions.FindRecursive("button-SKIRMISH");
      GameObject quickSkirmishButtonGo = GameObject.Instantiate(skirmishButton);

      quickSkirmishButtonGo.name = "button-QUICK-SKIRMISH";
      RectTransform quickSkirmishButtonTransform = (RectTransform)quickSkirmishButtonGo.transform;
      quickSkirmishButtonTransform.SetParent(mainNavigationActions.transform, false);

      // Reposition submenus
      RectTransform layoutActionsCampaignTransform = layoutMainNavigationGo.FindRecursive("layout-actions-Campaign").GetComponent<RectTransform>();
      layoutActionsCampaignTransform.anchoredPosition = new Vector2(-183, layoutActionsCampaignTransform.anchoredPosition.y);
      RectTransform layoutActionsSkirmishTransform = layoutMainNavigationGo.FindRecursive("layout-actions-Skirmish").GetComponent<RectTransform>();
      layoutActionsSkirmishTransform.anchoredPosition = new Vector2(30, layoutActionsSkirmishTransform.anchoredPosition.y);

      // Resize the navigation area holding the buttons
      RectTransform mainNavigationActionsTransform = (RectTransform)mainNavigationActions.transform;
      mainNavigationActionsTransform.sizeDelta = new Vector2(900, mainNavigationActionsTransform.sizeDelta.y);

      // Resize the background image behind the buttons
      RectTransform mainNavigationActionsBackgroundTransform = (RectTransform)mainNavigationActions.FindRecursive("bgFill").transform;
      mainNavigationActionsBackgroundTransform.sizeDelta = new Vector2(0, mainNavigationActionsBackgroundTransform.sizeDelta.y);

      // Set the button text
      TextMeshProUGUI quickSkirmishText = quickSkirmishButtonGo.FindRecursive("button_text").GetComponent<TextMeshProUGUI>();
      quickSkirmishText.SetText("Quick Skirmish");

      // Change the decoration position
      RectTransform leftDecorationTransform = (RectTransform)mainMenuScreenGo.FindRecursive("bracket-left").transform;
      RectTransform rightDecorationTransform = (RectTransform)mainMenuScreenGo.FindRecursive("bracket-right").transform;
      leftDecorationTransform.anchoredPosition = new Vector2(-190, leftDecorationTransform.anchoredPosition.y);
      rightDecorationTransform.anchoredPosition = new Vector2(190, rightDecorationTransform.anchoredPosition.y);

      HBSRadioSet mainMenuRadioSet = mainNavigationActions.GetComponent<HBSRadioSet>();
      mainMenuRadioSet.AddButtonToRadioSet(quickSkirmishButtonGo.GetComponent<HBSButton>());

      // Set up click event
      HBSDOTweenToggle quickSkirmishToggleButton = quickSkirmishButtonGo.GetComponent<HBSDOTweenToggle>();
      UnityEvent onClickEvent = new UnityEvent();
      quickSkirmishToggleButton.OnClicked = onClickEvent;
      onClickEvent.RemoveAllListeners();
      onClickEvent.AddListener(OnQuickSkirmishButtonClicked);

      // Check if there are any valid last used lances, if not - disable the button
      CloudUserSettings playerSettings = ActiveOrDefaultSettings.CloudSettings;
      LastUsedLances lastUsedLances = playerSettings.LastUsedLances;
      if (lastUsedLances.Count <= 0) quickSkirmishToggleButton.SetState(ButtonState.Unavailable, true);
    }

    private void OnQuickSkirmishButtonClicked() {
      ResetPatchValues();
      MainMenu mainMenu = GameObject.Find("uixPrfPanl_mainMenu-Screen_V3(Clone)").GetComponent<MainMenu>();
      mainMenu.ReceiveButtonPress("Back");
      Main.Logger.Log("[OnQuickSkirmishButtonClicked] Clicked");
      LoadingCurtain.Show();
      UnityGameInstance.Instance.StartCoroutine(WaitForInitialCurtainToShow());
    }

    private IEnumerator WaitForInitialCurtainToShow() {
      yield return null;
      ClickedQuickSkirmish = true;

      GameObject skirmishMenuGo = new GameObject("SkirmishMenuBypass");
      SkirmishSettings_Beta skirmishMenu = skirmishMenuGo.AddComponent<SkirmishSettings_Beta>();
      AccessTools.Field(typeof(SkirmishSettings_Beta), "playButton").SetValue(skirmishMenu, new HBSDOTweenButton());
      AccessTools.Field(typeof(SkirmishSettings_Beta), "playerLancePreview").SetValue(skirmishMenu, new LancePreviewPanel());
      AccessTools.Field(typeof(SkirmishSettings_Beta), "opponentLancePreview").SetValue(skirmishMenu, new LancePreviewPanel());
      AccessTools.Field(typeof(SkirmishSettings_Beta), "playerSettings").SetValue(skirmishMenu, ActiveOrDefaultSettings.CloudSettings);
      AccessTools.Field(typeof(SkirmishSettings_Beta), "mapModule").SetValue(skirmishMenu, new MapModule());
      AccessTools.Field(typeof(SkirmishSettings_Beta), "lanceBudgetDropdown").SetValue(skirmishMenu, new MockTMPDropdown());

      skirmishMenu.Init();
      AccessTools.Method(typeof(SkirmishSettings_Beta), "LoadLanceConfiguratorData").Invoke(skirmishMenu, null);

      UnityGameInstance.Instance.StartCoroutine(WaitForLoadingCurtain(skirmishMenu));
    }

    private IEnumerator WaitForLoadingCurtain(SkirmishSettings_Beta skirmishMenu) {
      while (!ReadyToLoadQuickSkirmish) yield return new WaitForSeconds(0.1f);
      LoadQuickSkirmishMap(skirmishMenu);
    }

    private void LoadQuickSkirmishMap(SkirmishSettings_Beta skirmishMenu) {
      AccessTools.Method(typeof(SkirmishSettings_Beta), "LaunchMap").Invoke(skirmishMenu, null);
    }

    private void ResetPatchValues() {
      MapModuleSelectedMapPatch.mapAndEncounter = null;
      MapModuleSelectedMoodPatch.mood = null;
    }

    public void ShowNewerVersionAvailablePopup(string currentVersion, string latestVersion) {
      GenericPopupBuilder.Create(GenericPopupType.Warning,
        $"A new version of Mission Control is available ({latestVersion}). You have {currentVersion}. (You can disable this warning in the settings.json)")
        .AddButton("OK", null, true, null).Render();
    }

    public bool HasUI(string prefabName) {
      if (UIPool.transform.Find(prefabName) != null) return true;
      return false;
    }

    public void BuildContractTypeCreditsPrefab(GameObject widget) {
      Main.LogDebug("[UiManager.BuildContractTypeCredits] About to build credits prefab");

      GameObject panelGo = widget;
      GameObject creditsPanelPrefab = GameObject.Instantiate(panelGo, UIPool.transform, false);
      creditsPanelPrefab.name = "MCUI_Contract_Type_Credits_Panel";
      creditsPanelPrefab.SetActive(false);

      MonoBehaviour.Destroy(creditsPanelPrefab.GetComponent<HorizontalLayoutGroup>());

      // Delete unrequired Gos and components
      GameObject.Destroy(creditsPanelPrefab.transform.Find("loadingSpinner").gameObject);
      GameObject.Destroy(creditsPanelPrefab.transform.Find("tipHeader").gameObject);
      GameObject.Destroy(creditsPanelPrefab.GetComponent<LoadingSpinnerAndTip_Widget>());

      GameObject messageTextGo = creditsPanelPrefab.transform.Find("message_text").gameObject;
      messageTextGo.name = "ContractTypeName";
      LocalizableText messageText = messageTextGo.GetComponent<LocalizableText>();
      TMP_FontAsset font = messageText.font;
      MonoBehaviour.Destroy(messageText);

      UnityGameInstance.Instance.StartCoroutine(FinishBuildContractTypeCreditsPrefab(font));
    }

    IEnumerator FinishBuildContractTypeCreditsPrefab(TMP_FontAsset font) {
      yield return new WaitForEndOfFrame();

      GameObject creditsPanelPrefab = UIPool.transform.Find("MCUI_Contract_Type_Credits_Panel").gameObject;
      VerticalLayoutGroup layoutGroup = creditsPanelPrefab.AddComponent<VerticalLayoutGroup>();
      layoutGroup.padding.right = 12;

      GameObject contractTypeTextGo = creditsPanelPrefab.transform.Find("ContractTypeName").gameObject;
      TextMeshProUGUI contractTypeText = contractTypeTextGo.AddComponent<TextMeshProUGUI>();
      contractTypeText.font = font;
      contractTypeText.fontSize = 36;

      // Authors
      GameObject authorTextGo = GameObject.Instantiate(contractTypeTextGo, creditsPanelPrefab.transform, false);
      authorTextGo.name = "Author";
      TextMeshProUGUI authorText = authorTextGo.GetComponent<TextMeshProUGUI>();
      authorText.fontSize = 28;

      // Contributors
      GameObject contributorsTextGo = GameObject.Instantiate(authorTextGo, creditsPanelPrefab.transform, false);
      contributorsTextGo.name = "Contributors";
      TextMeshProUGUI contributorsText = contributorsTextGo.GetComponent<TextMeshProUGUI>();
      contributorsText.fontSize = 28;

      // Created with the Designer
      GameObject designedWithTextGo = GameObject.Instantiate(contributorsTextGo, creditsPanelPrefab.transform, false);
      designedWithTextGo.name = "DesignedWith";
      TextMeshProUGUI designedWithText = designedWithTextGo.GetComponent<TextMeshProUGUI>();
      designedWithText.fontSize = 14;
    }

    public GameObject CreatePrefab(string prefabName, Transform parent) {
      if (HasUI(prefabName)) {
        GameObject prefab = UIPool.transform.Find(prefabName).gameObject;
        GameObject go = GameObject.Instantiate(prefab, parent, false);
        go.name = go.name.Replace("(Clone)", "");
        go.SetActive(true);
        return go;
      }

      Main.Logger.LogError($"[UiManager.CreatePrefab] Prefab '{prefabName}' does not exist. It should be built first.");
      return null;
    }

    public void CreateContractTypeCredits() {
      ContractTypeMetadata metaddata = DataManager.Instance.AvailableContractTypeMetadata[MissionControl.Instance.CurrentContractType];
      Transform parentGo = GameObject.Find("uixPrfPanl_combatMissionLoad-overlay_V2-MANAGED").transform;
      GameObject creditsGo = CreatePrefab("MCUI_Contract_Type_Credits_Panel", parentGo.Find("Representation"));

      // Resize container
      RectTransform creditsTransform = ((RectTransform)creditsGo.transform);
      Vector3 anchoredPos = ((RectTransform)creditsGo.transform).anchoredPosition;
      creditsTransform.pivot = new Vector2(1, 1);
      creditsTransform.anchoredPosition = new Vector2(1900f, anchoredPos.y);

      // Set text and alignment for Contract Type Name
      GameObject contractTypeNameTextGo = creditsGo.FindRecursive("ContractTypeName");
      TextMeshProUGUI contractTypeNameText = contractTypeNameTextGo.GetComponent<TextMeshProUGUI>();
      contractTypeNameText.text = MissionControl.Instance.CurrentContractTypeValue.FriendlyName;
      contractTypeNameText.alignment = TextAlignmentOptions.TopRight;

      RectTransform contractTypeNameTextTransform = contractTypeNameTextGo.GetComponent<RectTransform>();
      float contractTypeNameTextSizeDeltaX = contractTypeNameTextTransform.sizeDelta.x;

      // Set text and alignment for Author
      GameObject authorTextGo = creditsGo.FindRecursive("Author");
      TextMeshProUGUI authorText = authorTextGo.GetComponent<TextMeshProUGUI>();
      authorText.text = $"Created by: {metaddata.Author}";
      authorText.alignment = TextAlignmentOptions.TopRight;

      // Set text and alignment for Contributors
      GameObject contributorsTextGo = null;
      if (metaddata.Contributors != null && metaddata.Contributors.Count > 0) {
        contributorsTextGo = creditsGo.FindRecursive("Contributors");
        TextMeshProUGUI contributorsText = contributorsTextGo.GetComponent<TextMeshProUGUI>();
        contributorsText.text = $"Contributed: {String.Join(", ", metaddata.Contributors)}";
        contributorsText.alignment = TextAlignmentOptions.TopRight;
      } else {
        GameObject.Destroy(creditsGo.FindRecursive("Contributors"));
      }

      // Set text and alignment for Designed With
      GameObject designedWithTextGo = creditsGo.FindRecursive("DesignedWith");
      TextMeshProUGUI designedWithText = designedWithTextGo.GetComponent<TextMeshProUGUI>();
      designedWithText.text = "Designed with CWolf's MC Designer";
      designedWithText.alignment = TextAlignmentOptions.TopRight;

      UnityGameInstance.Instance.StartCoroutine(FinishCreateContractTypeCreditsPrefab(creditsTransform, contractTypeNameTextGo, authorTextGo, contributorsTextGo, designedWithTextGo));
    }

    IEnumerator FinishCreateContractTypeCreditsPrefab(RectTransform creditsTransform, GameObject contractTypeNameTextGo, GameObject authorTextGo, GameObject contributorsTextGo, GameObject designedWithTextGo) {
      yield return new WaitForEndOfFrame();

      VerticalLayoutGroup verticalLayout = creditsTransform.gameObject.GetComponent<VerticalLayoutGroup>();

      float contractTypeNameTextSizeDeltaX = contractTypeNameTextGo.GetComponent<TextMeshProUGUI>().bounds.size.x;
      float authorTextSizeDeltaX = authorTextGo.GetComponent<TextMeshProUGUI>().bounds.size.x;

      float contributorsTextSizeDeltaX = 0;
      if (contributorsTextGo != null) {
        contributorsTextSizeDeltaX = contributorsTextGo.GetComponent<TextMeshProUGUI>().bounds.size.x;
      }

      float designedWithTextSizeDeltaX = designedWithTextGo.GetComponent<TextMeshProUGUI>().bounds.size.x;

      // Update width and height of the container
      float[] sizeDeltaX = new float[] { contractTypeNameTextSizeDeltaX, authorTextSizeDeltaX, contributorsTextSizeDeltaX, designedWithTextSizeDeltaX };
      float largestX = sizeDeltaX.Max();
      creditsTransform.sizeDelta = new Vector2(largestX + verticalLayout.padding.right, 110f);
    }
  }
}