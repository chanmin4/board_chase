/*
 * Role hierarchy: Presentation > UI > Gameplay > GameOver
 * Primary responsibility: Presents GameOverUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("Load Events")]
    [SerializeField] private LoadEventChannelSO _loadGameScene;
    [SerializeField] private LoadEventChannelSO _loadMenu;

    [Header("Scenes")]
    [SerializeField] private GameSceneSO _retryGameScene;
    [SerializeField] private GameSceneSO _mainMenuScene;

    [Header("Texts")]
    [SerializeField] private TMP_Text _gameOverText;

    [Header("Localization")]
    [SerializeField] private string _stringTable = "UI_Popup";
    [SerializeField] private string _deathAtFormatKey = "Popup_DeathAtFormat";

    [Header("Buttons")]
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Options")]
    [SerializeField] private bool _showLoadingScreen = false;
    [SerializeField] private bool _fadeScreen = true;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (_retryButton != null)
            _retryButton.onClick.AddListener(OnRetryClicked);

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

    }
    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshGameOverText();
    }
    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(OnRetryClicked);

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RefreshGameOverText();
    }

    private void RefreshGameOverText()
    {
        if (_gameOverText == null)
            return;

        _gameOverText.text = UIRuntimeLocalization.Format(
            _stringTable,
            _deathAtFormatKey,
            "You died at {0}",
            RunResult.StageDisplayName);
    }

    // Purpose: Responds to the retry clicked callback.
    private void OnRetryClicked()
    {
        if (_loadGameScene == null || _retryGameScene == null)
            return;

        _loadGameScene.RaiseEvent(_retryGameScene, _showLoadingScreen, _fadeScreen);
    }

    // Purpose: Responds to the main menu clicked callback.
    private void OnMainMenuClicked()
    {
        if (_loadMenu == null || _mainMenuScene == null)
            return;

        _loadMenu.RaiseEvent(_mainMenuScene, _showLoadingScreen, _fadeScreen);
    }
}
