using System.Collections;
using UnityEngine;

public class UIMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private UIPopup _popupPanel;
    [SerializeField] private CanvasGroup _popupGroup;

    [SerializeField] private UISettingsController _settingsPanel;
    [SerializeField] private CanvasGroup _settingsGroup;

    [SerializeField] private UIMainMenu _mainMenuPanel;
    [SerializeField] private CanvasGroup _mainMenuGroup;

    [Header("Refs")]
    [SerializeField] private SaveSystem _saveSystem;
    [SerializeField] private InputReader _inputReader;

    [Header("Broadcasting On")]
    [SerializeField] private VoidEventChannelSO _startNewGameEvent;
    [SerializeField] private VoidEventChannelSO _continueGameEvent;

    private bool _hasSaveData;

    private void Awake()
    {
        SetVisible(_mainMenuGroup, true);
        SetVisible(_settingsGroup, false);
        SetVisible(_popupGroup, false);
    }

    private IEnumerator Start()
    {
        if (_inputReader != null)
            _inputReader.EnableMenuInput();

        yield return new WaitForSeconds(0.4f);

        RefreshMenuScreen();
    }

    private void OnEnable()
    {
        if (_mainMenuPanel == null)
            return;

        _mainMenuPanel.ContinueButtonAction += ContinueGame;
        _mainMenuPanel.NewGameButtonAction += ButtonStartNewGameClicked;
        _mainMenuPanel.AchievementButtonAction += OpenAchievementScreen;
        _mainMenuPanel.SettingsButtonAction += OpenSettingsScreen;
        _mainMenuPanel.QuitButtonAction += ShowExitConfirmationPopup;
    }

    private void OnDisable()
    {
        if (_mainMenuPanel == null)
            return;

        _mainMenuPanel.ContinueButtonAction -= ContinueGame;
        _mainMenuPanel.NewGameButtonAction -= ButtonStartNewGameClicked;
        _mainMenuPanel.AchievementButtonAction -= OpenAchievementScreen;
        _mainMenuPanel.SettingsButtonAction -= OpenSettingsScreen;
        _mainMenuPanel.QuitButtonAction -= ShowExitConfirmationPopup;
    }

    private void RefreshMenuScreen()
    {
        _hasSaveData = _saveSystem != null && _saveSystem.LoadSaveDataFromDisk();

        if (_mainMenuPanel != null)
            _mainMenuPanel.SetMenuScreen(_hasSaveData);
    }

    private void ContinueGame()
    {
        if (!_hasSaveData)
            return;

        if (_continueGameEvent != null)
            _continueGameEvent.RaiseEvent();
    }

    private void ButtonStartNewGameClicked()
    {
        if (!_hasSaveData)
        {
            ConfirmStartNewGame();
            return;
        }

        ShowStartNewGameConfirmationPopup();
    }

    private void ConfirmStartNewGame()
    {
        if (_startNewGameEvent != null)
            _startNewGameEvent.RaiseEvent();
    }

    private void ShowStartNewGameConfirmationPopup()
    {
        if (_popupPanel == null)
            return;

        _popupPanel.ConfirmationResponseAction += StartNewGamePopupResponse;
        _popupPanel.ClosePopupAction += HidePopup;

        _popupPanel.SetPopup(PopupType.NewGame);
        SetVisible(_popupGroup, true);
    }

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

    private void HidePopup()
    {
        if (_popupPanel != null)
            _popupPanel.ClosePopupAction -= HidePopup;

        SetVisible(_popupGroup, false);
        RefreshMenuScreen();
    }

    public void OpenAchievementScreen()
    {
        // TODO: Achievement panel도 CanvasGroup 방식으로 연결.
    }

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

    public void CloseSettingsScreen()
	{
		if (_settingsPanel != null)
			_settingsPanel.Closed -= CloseSettingsScreen;

		SetVisible(_settingsGroup, false);
		SetVisible(_mainMenuGroup, true);

		RefreshMenuScreen();
	}


    public void ShowExitConfirmationPopup()
    {
        if (_popupPanel == null)
            return;

        _popupPanel.ConfirmationResponseAction += HideExitConfirmationPopup;

        _popupPanel.SetPopup(PopupType.Quit);
        SetVisible(_popupGroup, true);
    }

    private void HideExitConfirmationPopup(bool quitConfirmed)
    {
        if (_popupPanel != null)
            _popupPanel.ConfirmationResponseAction -= HideExitConfirmationPopup;

        SetVisible(_popupGroup, false);

        if (quitConfirmed)
            Application.Quit();

        RefreshMenuScreen();
    }

    private void OnDestroy()
    {
        if (_popupPanel == null)
            return;

        _popupPanel.ConfirmationResponseAction -= HideExitConfirmationPopup;
        _popupPanel.ConfirmationResponseAction -= StartNewGamePopupResponse;
        _popupPanel.ClosePopupAction -= HidePopup;
    }

	private static void SetVisible(CanvasGroup group, bool visible)
	{
		if (group == null)
			return;

		group.alpha = visible ? 1f : 0f;
		group.interactable = visible;
		group.blocksRaycasts = visible;
	}
}