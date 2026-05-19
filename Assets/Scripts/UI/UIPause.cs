using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UIPause : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private UIGenericButton _resumeButton;
    [SerializeField] private UIGenericButton _settingsButton;
    [SerializeField] private UIGenericButton _backToMenuButton;
    [SerializeField] private UIGenericButton _quitButton;
    [SerializeField] private UIGenericButton _sectorCenterRescueButton;

    [Header("Button Texts")]
    [SerializeField] private TextMeshProUGUI _resumeText;
    [SerializeField] private TextMeshProUGUI _settingsText;
    [SerializeField] private TextMeshProUGUI _backToMenuText;
    [SerializeField] private TextMeshProUGUI _quitText;
    [SerializeField] private TextMeshProUGUI _sectorCenterRescueText;

    [Header("Localization")]
    [SerializeField] private TableReference _stringTable = "UI_Settings";
    [SerializeField] private string _resumeKey = "Pause_Resume";
    [SerializeField] private string _settingsKey = "Pause_Settings";
    [SerializeField] private string _mainMenuKey = "Pause_MainMenu";
    [SerializeField] private string _quitKey = "Pause_Quit";
    [SerializeField] private string _sectorCenterRescueKey = "Pause_SectorCenterRescue";

    [Header("Broadcasting On")]
    [SerializeField] private BoolEventChannelSO _onPauseOpened;

    public event UnityAction Resumed = delegate { };
    public event UnityAction SettingsScreenOpened = delegate { };
    public event UnityAction BackToMainRequested = delegate { };
    public event UnityAction QuitRequested = delegate { };
    public event UnityAction SectorCenterRescueRequested = delegate { };

    private Coroutine _refreshTextRoutine;

    private void OnEnable()
    {
        if (_onPauseOpened != null)
            _onPauseOpened.RaiseEvent(true);

        BindButtons();

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshTexts();
    }

    private void OnDisable()
    {
        if (_onPauseOpened != null)
            _onPauseOpened.RaiseEvent(false);

        UnbindButtons();

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_refreshTextRoutine != null)
        {
            StopCoroutine(_refreshTextRoutine);
            _refreshTextRoutine = null;
        }
    }

    private void BindButtons()
    {
        if (_resumeButton != null)
        {
            _resumeButton.SetButton(true);
            _resumeButton.Clicked += Resume;
        }

        if (_settingsButton != null)
            _settingsButton.Clicked += OpenSettingsScreen;

        if (_backToMenuButton != null)
            _backToMenuButton.Clicked += BackToMainMenuConfirmation;

        if (_quitButton != null)
            _quitButton.Clicked += RequestQuit;

        if (_sectorCenterRescueButton != null)
            _sectorCenterRescueButton.Clicked += RequestSectorCenterRescue;
    }

    private void UnbindButtons()
    {
        if (_resumeButton != null)
            _resumeButton.Clicked -= Resume;

        if (_settingsButton != null)
            _settingsButton.Clicked -= OpenSettingsScreen;

        if (_backToMenuButton != null)
            _backToMenuButton.Clicked -= BackToMainMenuConfirmation;

        if (_quitButton != null)
            _quitButton.Clicked -= RequestQuit;

        if (_sectorCenterRescueButton != null)
            _sectorCenterRescueButton.Clicked -= RequestSectorCenterRescue;
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (_refreshTextRoutine != null)
            StopCoroutine(_refreshTextRoutine);

        _refreshTextRoutine = StartCoroutine(RefreshTextsRoutine());
    }

    private IEnumerator RefreshTextsRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        yield return SetLocalizedText(_resumeText, _resumeKey);
        yield return SetLocalizedText(_settingsText, _settingsKey);
        yield return SetLocalizedText(_backToMenuText, _mainMenuKey);
        yield return SetLocalizedText(_quitText, _quitKey);
        yield return SetLocalizedText(_sectorCenterRescueText, _sectorCenterRescueKey);

        _refreshTextRoutine = null;
    }

    private IEnumerator SetLocalizedText(TextMeshProUGUI target, string key)
    {
        if (target == null || string.IsNullOrWhiteSpace(key))
            yield break;

        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(_stringTable, key);

        yield return handle;

        string result = handle.Status == AsyncOperationStatus.Succeeded
            ? handle.Result
            : string.Empty;

        target.text = string.IsNullOrWhiteSpace(result) ? key : result;
    }

    private void Resume()
    {
        Resumed.Invoke();
    }

    private void OpenSettingsScreen()
    {
        SettingsScreenOpened.Invoke();
    }

    private void BackToMainMenuConfirmation()
    {
        BackToMainRequested.Invoke();
    }

    private void RequestQuit()
    {
        QuitRequested.Invoke();
    }

    private void RequestSectorCenterRescue()
    {
        SectorCenterRescueRequested.Invoke();
    }

    public void CloseScreen()
    {
        Resumed.Invoke();
    }
}