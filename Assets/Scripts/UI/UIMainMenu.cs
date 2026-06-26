using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

public class UIMainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _playerUpgradeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _achievementButton;
    [SerializeField] private Button _creditButton;
    [SerializeField] private Button _quitButton;

    [Header("Text Localization")]
    [Tooltip("Main menu button texts string table. Usually UI_MainMenu.")]
    [SerializeField] private TableReference _mainMenuStringTable = "UI_MainMenu";

    [SerializeField] private LocalizeStringEvent _continueText;
    [SerializeField] private LocalizeStringEvent _newGameText;
    [SerializeField] private LocalizeStringEvent _playerUpgradeText;
    [SerializeField] private LocalizeStringEvent _settingsText;
    [SerializeField] private LocalizeStringEvent _achievementText;
    [SerializeField] private LocalizeStringEvent _creditText;
    [SerializeField] private LocalizeStringEvent _quitText;

    [Header("Localization Keys")]
    [SerializeField] private string _continueKey = "MainMenu_Continue";
    [SerializeField] private string _newGameKey = "MainMenu_NewGame";
    [SerializeField] private string _playerUpgradeKey = "MainMenu_PlayerUpgrade";
    [SerializeField] private string _settingsKey = "MainMenu_Settings";
    [SerializeField] private string _achievementKey = "MainMenu_Achievement";
    [SerializeField] private string _creditKey = "MainMenu_Credit";
    [SerializeField] private string _quitKey = "MainMenu_Quit";

    public event UnityAction NewGameButtonAction = delegate { };
    public event UnityAction ContinueButtonAction = delegate { };
    public event UnityAction PlayerUpgradeButtonAction = delegate { };
    public event UnityAction SettingsButtonAction = delegate { };
    public event UnityAction AchievementButtonAction = delegate { };
    public event UnityAction CreditButtonAction = delegate { };
    public event UnityAction QuitButtonAction = delegate { };

    private void Awake()
    {
        ApplyTextKeys();
    }

    private void OnEnable()
    {
        if (_newGameButton != null)
            _newGameButton.onClick.AddListener(HandleNewGameClicked);

        if (_continueButton != null)
            _continueButton.onClick.AddListener(HandleContinueClicked);

        if (_playerUpgradeButton != null)
            _playerUpgradeButton.onClick.AddListener(HandlePlayerUpgradeClicked);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(HandleSettingsClicked);

        if (_achievementButton != null)
            _achievementButton.onClick.AddListener(HandleAchievementClicked);

        if (_creditButton != null)
            _creditButton.onClick.AddListener(HandleCreditClicked);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(HandleQuitClicked);

        ApplyTextKeys();
    }

    private void OnDisable()
    {
        if (_newGameButton != null)
            _newGameButton.onClick.RemoveListener(HandleNewGameClicked);

        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(HandleContinueClicked);

        if (_playerUpgradeButton != null)
            _playerUpgradeButton.onClick.RemoveListener(HandlePlayerUpgradeClicked);

        if (_settingsButton != null)
            _settingsButton.onClick.RemoveListener(HandleSettingsClicked);

        if (_achievementButton != null)
            _achievementButton.onClick.RemoveListener(HandleAchievementClicked);

        if (_creditButton != null)
            _creditButton.onClick.RemoveListener(HandleCreditClicked);

        if (_quitButton != null)
            _quitButton.onClick.RemoveListener(HandleQuitClicked);
    }

    public void SetMenuScreen(bool hasSaveData)
    {
        if (_continueButton != null)
            _continueButton.interactable = hasSaveData;

        if (hasSaveData && _continueButton != null)
            _continueButton.Select();
        else if (_newGameButton != null)
            _newGameButton.Select();
    }

    private void ApplyTextKeys()
    {
        ApplyTextKey(_continueText, _continueKey);
        ApplyTextKey(_newGameText, _newGameKey);
        ApplyTextKey(_playerUpgradeText, _playerUpgradeKey);
        ApplyTextKey(_settingsText, _settingsKey);
        ApplyTextKey(_achievementText, _achievementKey);
        ApplyTextKey(_creditText, _creditKey);
        ApplyTextKey(_quitText, _quitKey);
    }

    private void ApplyTextKey(LocalizeStringEvent localizeEvent, string key)
    {
        if (localizeEvent == null || string.IsNullOrWhiteSpace(key))
            return;

        localizeEvent.StringReference.TableReference = _mainMenuStringTable;
        localizeEvent.StringReference.TableEntryReference = key;
        localizeEvent.RefreshString();
    }

    private void HandleNewGameClicked()
    {
        NewGameButtonAction.Invoke();
    }

    private void HandleContinueClicked()
    {
        ContinueButtonAction.Invoke();
    }

    private void HandlePlayerUpgradeClicked()
    {
        PlayerUpgradeButtonAction.Invoke();
    }

    private void HandleSettingsClicked()
    {
        SettingsButtonAction.Invoke();
    }

    private void HandleAchievementClicked()
    {
        AchievementButtonAction.Invoke();
    }

    private void HandleCreditClicked()
    {
        Debug.Log("[UIMainMenu] Credit clicked", this);
        CreditButtonAction.Invoke();
    }

    private void HandleQuitClicked()
    {
        QuitButtonAction.Invoke();
    }
}
