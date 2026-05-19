using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-950)]
public class EscPauseMenuController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private GameStateSO _gameState;

    [Header("View")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private int _sortingOrder = 10000;
    [SerializeField] private CanvasGroup _rootGroup;
    [SerializeField] private CanvasGroup _pauseMenuGroup;
    [SerializeField] private CanvasGroup _settingsGroup;
    [SerializeField] private UIPause _pauseView;
    [SerializeField] private UISettingsController _settingsController;

    [Header("Scene")]
    [SerializeField] private LoadEventChannelSO _loadMenuChannel;
    [SerializeField] private GameSceneSO _mainMenuScene;

    [Header("Emergency")]
    [SerializeField] private PlayerSectorCenterRescue _sectorCenterRescue;
    [Header("Confirmation Popup")]
    [SerializeField] private UIPopup _confirmationPopup;
    [SerializeField] private CanvasGroup _confirmationPopupGroup;

    private enum PauseConfirmationAction
    {
        None,
        BackToMenu,
        Quit
    }
    private PauseConfirmationAction _pendingConfirmationAction;
    private bool _confirmationOpen;
    private bool _isOpen;
    private bool _settingsOpen;
    private GameState _previousState;

    private void Awake()
    {
        if (_canvas != null)
            _canvas.sortingOrder = _sortingOrder;

        SetVisible(_rootGroup, false);
        SetVisible(_pauseMenuGroup, true);
        SetVisible(_settingsGroup, false);
    }

    private void OnEnable()
    {
        if (_pauseView != null)
        {
            _pauseView.Resumed += ResumeGame;
            _pauseView.SettingsScreenOpened += OpenSettings;
            _pauseView.BackToMainRequested += ShowBackToMenuConfirmation;
            _pauseView.QuitRequested += ShowQuitConfirmation;
            _pauseView.SectorCenterRescueRequested += MovePlayerToCurrentSectorCenter;
        }

        if (_settingsController != null)
            _settingsController.Closed += CloseSettings;

        if (_confirmationPopup != null)
        {
            _confirmationPopup.ConfirmationResponseAction += HandleConfirmationResponse;
            _confirmationPopup.ClosePopupAction += HideConfirmationPopup;
        }
    }

    private void OnDisable()
    {
        if (_pauseView != null)
        {
            _pauseView.Resumed -= ResumeGame;
            _pauseView.SettingsScreenOpened -= OpenSettings;
            _pauseView.BackToMainRequested -= ShowBackToMenuConfirmation;
            _pauseView.QuitRequested -= ShowQuitConfirmation;   
            _pauseView.SectorCenterRescueRequested -= MovePlayerToCurrentSectorCenter;
        }

        if (_settingsController != null)
            _settingsController.Closed -= CloseSettings;

        if (_confirmationPopup != null)
        {
            _confirmationPopup.ConfirmationResponseAction -= HandleConfirmationResponse;
            _confirmationPopup.ClosePopupAction -= HideConfirmationPopup;
        }
    }

    private void Update()
    {
            


        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;
        if (_confirmationOpen)
        {
            HideConfirmationPopup();
            return;
        }
        if (!_isOpen)
        {
            OpenPause();
            return;
        }

        if (_settingsOpen)
        {
            CloseSettings();
            return;
        }

        ResumeGame();
    }

    private void OpenPause()
    {
        _isOpen = true;
        _settingsOpen = false;

        _previousState = _gameState != null ? _gameState.CurrentGameState : GameState.Gameplay;

        GamePause.On();

        if (_gameState != null)
            _gameState.UpdateGameState(GameState.Pause);

        if (_inputReader != null)
            _inputReader.EnableMenuInput();

        SetVisible(_rootGroup, true);
        SetVisible(_pauseMenuGroup, true);
        SetVisible(_settingsGroup, false);
    }

    private void ResumeGame()
    {
        _isOpen = false;
        _settingsOpen = false;

        SetVisible(_rootGroup, false);
        SetVisible(_settingsGroup, false);
        SetVisible(_pauseMenuGroup, true);

        if (_inputReader != null)
            _inputReader.EnableGameplayInput();

        if (_gameState != null)
            _gameState.UpdateGameState(_previousState);

        GamePause.Off();
    }

    private void OpenSettings()
    {
        _settingsOpen = true;

        SetVisible(_pauseMenuGroup, false);
        SetVisible(_settingsGroup, true);
        if (_settingsController != null)
            _settingsController.OpenSettingsScreen();
    }

    private void CloseSettings()
    {
        _settingsOpen = false;

        SetVisible(_settingsGroup, false);
        SetVisible(_pauseMenuGroup, true);
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        GamePause.Off();

        if (_loadMenuChannel != null && _mainMenuScene != null)
            _loadMenuChannel.RaiseEvent(_mainMenuScene, true, true);
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        GamePause.Off();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void MovePlayerToCurrentSectorCenter()
    {
        if (_sectorCenterRescue != null)
            _sectorCenterRescue.TryMovePlayerToCurrentSectorCenter();
    }

    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void ShowBackToMenuConfirmation()
    {
        ShowConfirmation(PopupType.BackToMenu, PauseConfirmationAction.BackToMenu);
    }

    private void ShowQuitConfirmation()
    {
        ShowConfirmation(PopupType.Quit, PauseConfirmationAction.Quit);
    }

    private void ShowConfirmation(PopupType popupType, PauseConfirmationAction action)
    {
        if (_confirmationPopup == null)
            return;

        _pendingConfirmationAction = action;
        _confirmationOpen = true;

        _confirmationPopup.Show(popupType);
        SetVisible(_confirmationPopupGroup, true);
    }

    private void HandleConfirmationResponse(bool confirmed)
    {
        PauseConfirmationAction action = _pendingConfirmationAction;
        HideConfirmationPopup();

        if (!confirmed)
            return;

        switch (action)
        {
            case PauseConfirmationAction.BackToMenu:
                ReturnToMainMenu();
                break;

            case PauseConfirmationAction.Quit:
                QuitGame();
                break;
        }
    }

    private void HideConfirmationPopup()
    {
        _pendingConfirmationAction = PauseConfirmationAction.None;
        _confirmationOpen = false;

        if (_confirmationPopup != null)
            _confirmationPopup.Hide();

        SetVisible(_confirmationPopupGroup, false);
    }
}
