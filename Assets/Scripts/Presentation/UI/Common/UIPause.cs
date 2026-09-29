/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Implements the focused UIPause responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
    [SerializeField] private TableReference _stringTable = "UI_Pause";
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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_onPauseOpened != null)
            _onPauseOpened.RaiseEvent(true);

        BindButtons();

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshTexts();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
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

    // Purpose: Binds buttons to the active runtime or UI target.
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

    // Purpose: Removes the buttons binding.
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

    // Purpose: Handles the incoming locale changed request or event.
    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RefreshTexts();
    }

    // Purpose: Refreshes the displayed or cached texts.
    private void RefreshTexts()
    {
        if (_refreshTextRoutine != null)
            StopCoroutine(_refreshTextRoutine);

        _refreshTextRoutine = StartCoroutine(RefreshTextsRoutine());
    }

    // Purpose: Refreshes the displayed or cached texts routine.
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

    // Purpose: Updates the localized text state.
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

    // Purpose: Resumes the paused resume workflow.
    private void Resume()
    {
        Resumed.Invoke();
    }

    // Purpose: Opens the settings screen UI or workflow.
    private void OpenSettingsScreen()
    {
        SettingsScreenOpened.Invoke();
    }

    // Purpose: Performs the back to main menu confirmation workflow for this component.
    private void BackToMainMenuConfirmation()
    {
        BackToMainRequested.Invoke();
    }

    // Purpose: Requests quit from the owning service.
    private void RequestQuit()
    {
        QuitRequested.Invoke();
    }

    // Purpose: Requests sector center rescue from the owning service.
    private void RequestSectorCenterRescue()
    {
        Debug.Log("[UIPause] Sector center rescue button clicked.", this);
        SectorCenterRescueRequested.Invoke();
    }

    // Purpose: Closes the active screen UI or workflow.
    public void CloseScreen()
    {
        Resumed.Invoke();
    }
}
