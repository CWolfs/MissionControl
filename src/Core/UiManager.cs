using UnityEngine;
using UnityEngine.Events;

using System.Collections;

using TMPro;

using Harmony;

using BattleTech;
using BattleTech.UI;
using BattleTech.Save;

using MissionControl.Patches;

namespace MissionControl {
  public class UiManager {
    private static UiManager instance;
    public static UiManager Instance {
      get {
        if (instance == null) instance = new UiManager();
        return instance;
      }
    }

    public bool ClickedQuickSkirmish { get; set; } = false;
    public bool ReadyToLoadQuickSkirmish { get; set; } = false;
    public bool ShouldPatchMainMenu { get; set; } = true;

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
  }
}