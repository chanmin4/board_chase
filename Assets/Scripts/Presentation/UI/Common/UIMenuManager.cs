/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Coordinates the UIMenuManager subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private UIPopup _popupPanel;
    [SerializeField] private CanvasGroup _popupGroup;

    [SerializeField] private UISettingsController _settingsPanel;
    [SerializeField] private CanvasGroup _settingsGroup;

    [SerializeField] private CreditPanel _creditPanel;
    [SerializeField] private CanvasGroup _creditGroup;

    [SerializeField] private CanvasGroup _achievementGroup;
    [SerializeField] private Button _achievementBackButton;

    [SerializeField] private UIMainMenu _mainMenuPanel;
    [SerializeField] private CanvasGroup _mainMenuGroup;

    [Header("WIP Legacy Meta System")]
    [Tooltip("Kept as serialized compatibility only. The active game no longer exposes permanent-currency upgrades.")]
    [SerializeField] private MetaSystemPanelUI _metaSystemPanel;
    [SerializeField] private CanvasGroup _metaSystemGroup;

    [Header("Refs")]
    [SerializeField] private SaveSystem _saveSystem;
    [SerializeField] private InputReader _inputReader;

    [Header("Broadcasting On")]
    [SerializeField] private VoidEventChannelSO _startNewGameEvent;
    [SerializeField] private VoidEventChannelSO _continueGameEvent;

    private bool _hasSaveData;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        SetVisible(_mainMenuGroup, true);
        SetVisible(_settingsGroup, false);
        SetVisible(_creditGroup, false);
        SetVisible(_achievementGroup, false);
        SetVisible(_metaSystemGroup, false);
        SetVisible(_popupGroup, false);
    }

    // Purpose: Completes initialization after scene dependencies are ready.
    private IEnumerator Start()
    {
        if (_inputReader != null)
            _inputReader.EnableMenuInput();

        yield return new WaitForSeconds(0.4f);

        RefreshMenuScreen();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_mainMenuPanel != null)
        {
            _mainMenuPanel.ContinueButtonAction += ContinueGame;
            _mainMenuPanel.NewGameButtonAction += ButtonStartNewGameClicked;
            _mainMenuPanel.AchievementButtonAction += OpenAchievementScreen;
            _mainMenuPanel.CreditButtonAction += OpenCreditScreen;
            _mainMenuPanel.SettingsButtonAction += OpenSettingsScreen;
            _mainMenuPanel.QuitButtonAction += ShowExitConfirmationPopup;
        }

        if (_achievementBackButton != null)
            _achievementBackButton.onClick.AddListener(CloseAchievementScreen);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_mainMenuPanel != null)
        {
            _mainMenuPanel.ContinueButtonAction -= ContinueGame;
            _mainMenuPanel.NewGameButtonAction -= ButtonStartNewGameClicked;
            _mainMenuPanel.AchievementButtonAction -= OpenAchievementScreen;
            _mainMenuPanel.CreditButtonAction -= OpenCreditScreen;
            _mainMenuPanel.SettingsButtonAction -= OpenSettingsScreen;
            _mainMenuPanel.QuitButtonAction -= ShowExitConfirmationPopup;
        }

        if (_achievementBackButton != null)
            _achievementBackButton.onClick.RemoveListener(CloseAchievementScreen);
    }

    // Purpose: Refreshes the displayed or cached menu screen.
    private void RefreshMenuScreen()
    {
        _hasSaveData = _saveSystem != null && _saveSystem.LoadSaveDataFromDisk();

        if (_mainMenuPanel != null)
            _mainMenuPanel.SetMenuScreen(_hasSaveData);
    }

    // Purpose: Performs the continue game workflow for this component.
    private void ContinueGame()
    {
        if (!_hasSaveData)
            return;

        if (_continueGameEvent != null)
            _continueGameEvent.RaiseEvent();
    }

    // Purpose: Performs the button start new game clicked workflow for this component.
    private void ButtonStartNewGameClicked()
    {
        if (!_hasSaveData)
        {
            ConfirmStartNewGame();
            return;
        }

        ShowStartNewGameConfirmationPopup();
    }

    // Purpose: Commits the current start new game selection or change.
    private void ConfirmStartNewGame()
    {
        if (_startNewGameEvent != null)
            _startNewGameEvent.RaiseEvent();
    }

    // Purpose: Shows the start new game confirmation popup UI or visual state.
    private void ShowStartNewGameConfirmationPopup()
    {
        if (_popupPanel == null)
            return;

        _popupPanel.ConfirmationResponseAction += StartNewGamePopupResponse;
        _popupPanel.ClosePopupAction += HidePopup;

        _popupPanel.SetPopup(PopupType.NewGame);
        SetVisible(_popupGroup, true);
    }

    // Purpose: Begins the new game popup response workflow.
    private void StartNewGamePopupResponse(bool startNewGameConfirmed)
    {
        if (_popupPanel != null)
        {
            _popupPanel.ConfirmationResponseAction -= StartNewGamePopupResponse;
            _popupPanel.ClosePopupAction -= HidePopup;
        }

        SetVisible(_popupGroup, false);

        if (startNewGameConfirmed)
            ConfirmStartNewGame();

        RefreshMenuScreen();
    }

    // Purpose: Hides the popup UI or visual state.
    private void HidePopup()
    {
        if (_popupPanel != null)
            _popupPanel.ClosePopupAction -= HidePopup;

        SetVisible(_popupGroup, false);
        RefreshMenuScreen();
    }

    // Purpose: Opens the achievement screen UI or workflow.
    public void OpenAchievementScreen()
    {
        if (_achievementGroup == null)
        {
            Debug.LogError("[UIMenuManager] Achievement group is missing.", this);
            return;
        }

        SetVisible(_mainMenuGroup, false);
        SetVisible(_achievementGroup, true);
    }

    // Purpose: Closes the active achievement screen UI or workflow.
    public void CloseAchievementScreen()
    {
        SetVisible(_achievementGroup, false);
        SetVisible(_mainMenuGroup, true);

        RefreshMenuScreen();
    }

    // Purpose: Opens the meta system screen UI or workflow.
    public void OpenMetaSystemScreen()
    {
        Debug.LogWarning("[UIMenuManager] Meta upgrades are WIP and are intentionally unavailable in the current player flow.", this);
    }

    // Purpose: Closes the active meta system screen UI or workflow.
    public void CloseMetaSystemScreen()
    {
        if (_metaSystemPanel != null)
            _metaSystemPanel.Closed -= CloseMetaSystemScreen;

        SetVisible(_metaSystemGroup, false);
        SetVisible(_mainMenuGroup, true);

        RefreshMenuScreen();
    }

    // Purpose: Opens the settings screen UI or workflow.
    public void OpenSettingsScreen()
    {
        if (_settingsPanel == null)
        {
            Debug.LogError("[UIMenuManager] Settings panel is missing.", this);
            return;
        }

        SetVisible(_mainMenuGroup, false);
        SetVisible(_settingsGroup, true);

        _settingsPanel.Closed -= CloseSettingsScreen;
        _settingsPanel.Closed += CloseSettingsScreen;
        _settingsPanel.OpenSettingsScreen();
    }

    // Purpose: Closes the active settings screen UI or workflow.
    public void CloseSettingsScreen()
    {
        if (_settingsPanel != null)
            _settingsPanel.Closed -= CloseSettingsScreen;

        SetVisible(_settingsGroup, false);
        SetVisible(_mainMenuGroup, true);

        RefreshMenuScreen();
    }

    // Purpose: Opens the credit screen UI or workflow.
    public void OpenCreditScreen()
    {
        Debug.Log("[UIMenuManager] OpenCreditScreen", this);
        if (_creditPanel == null)
        {
            Debug.LogError("[UIMenuManager] Credit panel is missing.", this);
            return;
        }

        SetVisible(_mainMenuGroup, false);
        SetVisible(_creditGroup, true);

        _creditPanel.Closed -= CloseCreditScreen;
        _creditPanel.Closed += CloseCreditScreen;
        _creditPanel.OpenCreditPanel();
    }

    // Purpose: Closes the active credit screen UI or workflow.
    public void CloseCreditScreen()
    {
        if (_creditPanel != null)
            _creditPanel.Closed -= CloseCreditScreen;

        SetVisible(_creditGroup, false);
        SetVisible(_mainMenuGroup, true);

        RefreshMenuScreen();
    }

    // Purpose: Shows the exit confirmation popup UI or visual state.
    public void ShowExitConfirmationPopup()
    {
        if (_popupPanel == null)
            return;

        _popupPanel.ConfirmationResponseAction += HideExitConfirmationPopup;

        _popupPanel.SetPopup(PopupType.Quit);
        SetVisible(_popupGroup, true);
    }

    // Purpose: Hides the exit confirmation popup UI or visual state.
    private void HideExitConfirmationPopup(bool quitConfirmed)
    {
        if (_popupPanel != null)
            _popupPanel.ConfirmationResponseAction -= HideExitConfirmationPopup;

        SetVisible(_popupGroup, false);

        if (quitConfirmed)
            Application.Quit();

        RefreshMenuScreen();
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (_popupPanel != null)
        {
            _popupPanel.ConfirmationResponseAction -= HideExitConfirmationPopup;
            _popupPanel.ConfirmationResponseAction -= StartNewGamePopupResponse;
            _popupPanel.ClosePopupAction -= HidePopup;
        }

        if (_metaSystemPanel != null)
            _metaSystemPanel.Closed -= CloseMetaSystemScreen;

        if (_creditPanel != null)
            _creditPanel.Closed -= CloseCreditScreen;

        if (_achievementBackButton != null)
            _achievementBackButton.onClick.RemoveListener(CloseAchievementScreen);
    }

    // Purpose: Updates the visible state.
    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
