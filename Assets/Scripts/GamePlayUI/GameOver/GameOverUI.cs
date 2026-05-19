using TMPro;
using UnityEngine;
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

    [Header("Buttons")]
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Options")]
    [SerializeField] private bool _showLoadingScreen = false;
    [SerializeField] private bool _fadeScreen = true;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (_retryButton != null)
            _retryButton.onClick.AddListener(OnRetryClicked);

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

    }
    private void Start()
    {
        if (_gameOverText != null)
            _gameOverText.text = $"You died at {RunResult.StageDisplayName}";
    }
    private void OnDestroy()
    {
        if (_retryButton != null)
            _retryButton.onClick.RemoveListener(OnRetryClicked);

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }

    private void OnRetryClicked()
    {
        if (_loadGameScene == null || _retryGameScene == null)
            return;

        _loadGameScene.RaiseEvent(_retryGameScene, _showLoadingScreen, _fadeScreen);
    }

    private void OnMainMenuClicked()
    {
        if (_loadMenu == null || _mainMenuScene == null)
            return;

        _loadMenu.RaiseEvent(_mainMenuScene, _showLoadingScreen, _fadeScreen);
    }
}
