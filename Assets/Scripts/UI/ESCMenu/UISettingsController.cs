using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
public enum SettingFieldType
{
    Language,
    Resolution,
    Displaymode,
    AimSensitivity,

    Volume_Master,
    Volume_Music,
    Volume_SFx,

    VSync,
    FrameRateLimit,

    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Attack,
    Paint,
    Dash,
    Interact,
    UpgradePanel,
    QTEInteract,
    BulletSlot1,
    BulletSlot2
}

public enum SettingsType
{
    General,
    Audio,
    Graphics,
    KeyBindings
}

public class UISettingsController : MonoBehaviour
{
    [SerializeField] private UISettingGeneralComponent _generalComponent;
    [SerializeField] private UISettingGraphicsComponent _graphicsComponent;
    [SerializeField] private UISettingAudioComponent _audioComponent;
    [SerializeField] private UISettingKeyBindingComponent _keyBindingComponent;

    [SerializeField] private UISettingTabsFiller _settingTabFiller = default;
    [SerializeField] private SettingsSO _currentSettings = default;
    [SerializeField] private List<SettingsType> _settingTabsList = new();

    [SerializeField] private InputReader _inputReader = default;
    [SerializeField] private VoidEventChannelSO _saveSettingsEvent = default;
    [Header("Startup")]
    [SerializeField] private SettingsType _defaultTab = SettingsType.General;
    [SerializeField] private bool _rememberLastTabWhileOpen = true;
    public UnityAction Closed = delegate { };
    [Header("Navigation")]
    [SerializeField] private Button _backButton;
    [Header("Title Localization")]
    [SerializeField] private TableReference _settingsStringTable = "UI_Settings";
    [SerializeField] private string _settingsTitleEntryKey = "Settings_Title";
    [SerializeField] private TextMeshProUGUI _settingsTitleText;
    private Coroutine _settingsTitleRoutine;
    private SettingsType _selectedTab = SettingsType.General;
    private bool _hasOpenedSettings;
    private SettingsType _lastSelectedTab = SettingsType.General;
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshSettingsTitle();
        if (_generalComponent != null)
            _generalComponent.SaveRequested += SaveGeneralSettings;

        if (_audioComponent != null)
            _audioComponent._save += SaveAudioSettings;

        if (_graphicsComponent != null)
            _graphicsComponent.SaveRequested += SaveGraphicsSettings;

        if (_keyBindingComponent != null)
            _keyBindingComponent.SaveRequested += SaveKeyBindingSettings;

        if (_inputReader != null)
        {
            _inputReader.MenuCloseEvent += CloseScreen;
            _inputReader.TabSwitched += SwitchTab;
        }

        if (_settingTabFiller != null)
        {
            _settingTabFiller.FillTabs(_settingTabsList);
            _settingTabFiller.ChooseTab += OpenSetting;
        }
        if (_backButton != null)
            _backButton.onClick.AddListener(CloseScreen);

        OpenInitialSettingTab();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_settingsTitleRoutine != null)
        {
            StopCoroutine(_settingsTitleRoutine);
            _settingsTitleRoutine = null;
        }
        if (_generalComponent != null)
            _generalComponent.SaveRequested -= SaveGeneralSettings;

        if (_audioComponent != null)
            _audioComponent._save -= SaveAudioSettings;

        if (_graphicsComponent != null)
            _graphicsComponent.SaveRequested -= SaveGraphicsSettings;

        if (_keyBindingComponent != null)
            _keyBindingComponent.SaveRequested -= SaveKeyBindingSettings;

        if (_inputReader != null)
        {
            _inputReader.MenuCloseEvent -= CloseScreen;
            _inputReader.TabSwitched -= SwitchTab;
        }

        if (_settingTabFiller != null)
            _settingTabFiller.ChooseTab -= OpenSetting;
        if (_backButton != null)
            _backButton.onClick.RemoveListener(CloseScreen);
    }

    public void CloseScreen()
    {
        Closed.Invoke();
    }

    private void OpenSetting(SettingsType settingType)
    {
        _selectedTab = settingType;
        if (_rememberLastTabWhileOpen)
        {
            _lastSelectedTab = settingType;
            _hasOpenedSettings = true;
        }

        if (settingType == SettingsType.General && _generalComponent != null)
            _generalComponent.Setup(_currentSettings);

        if (settingType == SettingsType.Audio && _audioComponent != null)
            _audioComponent.Setup(
                _currentSettings.MusicVolume,
                _currentSettings.SfxVolume,
                _currentSettings.MasterVolume);

        if (settingType == SettingsType.Graphics && _graphicsComponent != null)
            _graphicsComponent.Setup(_currentSettings);

        if (settingType == SettingsType.KeyBindings && _keyBindingComponent != null)
            _keyBindingComponent.Setup();

        SetActive(_generalComponent, settingType == SettingsType.General);
        SetActive(_audioComponent, settingType == SettingsType.Audio);
        SetActive(_graphicsComponent, settingType == SettingsType.Graphics);
        SetActive(_keyBindingComponent, settingType == SettingsType.KeyBindings);

        if (_settingTabFiller != null)
            _settingTabFiller.SelectTab(settingType);
    }

    private void SwitchTab(float orientation)
    {
        if (orientation == 0f || _settingTabsList == null || _settingTabsList.Count == 0)
            return;

        int currentIndex = _settingTabsList.FindIndex(o => o == _selectedTab);
        if (currentIndex < 0)
            return;

        currentIndex += orientation < 0f ? -1 : 1;
        currentIndex = Mathf.Clamp(currentIndex, 0, _settingTabsList.Count - 1);

        OpenSetting(_settingTabsList[currentIndex]);
    }

    private void SaveGeneralSettings(
        UnityEngine.Localization.Locale locale,
        int resolutionIndex,
        bool isFullscreen,
        float aimSensitivity)
    {
        _currentSettings.SaveGeneralSettings(
            locale,
            resolutionIndex,
            isFullscreen,
            aimSensitivity);

        _saveSettingsEvent.RaiseEvent();
    }

    private void SaveAudioSettings(float musicVolume, float sfxVolume, float masterVolume)
    {
        _currentSettings.SaveAudioSettings(musicVolume, sfxVolume, masterVolume);
        _saveSettingsEvent.RaiseEvent();
    }

    private void SaveGraphicsSettings(bool useVSync, int targetFrameRate)
    {
        _currentSettings.SaveGraphicsSettings(useVSync, targetFrameRate);
        _saveSettingsEvent.RaiseEvent();
    }

    private void SaveKeyBindingSettings()
    {
        _saveSettingsEvent.RaiseEvent();
    }

    private static void SetActive(MonoBehaviour component, bool visible)
    {
        if (component == null)
            return;

        CanvasGroup group = component.GetComponent<CanvasGroup>();

        if (group == null)
        {
            Debug.LogError($"[UISettingsController] CanvasGroup missing on {component.name}.", component);
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
    public void OpenSettingsScreen()
    {
        SettingsType tabToOpen = _rememberLastTabWhileOpen && _hasOpenedSettings
            ? _lastSelectedTab
            : _defaultTab;

        OpenSetting(tabToOpen);
    }

    private void OpenInitialSettingTab()
    {
        _lastSelectedTab = _defaultTab;
        _hasOpenedSettings = false;

        OpenSetting(_defaultTab);
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RefreshSettingsTitle();
    }

    private void RefreshSettingsTitle()
    {
        if (_settingsTitleText == null || string.IsNullOrWhiteSpace(_settingsTitleEntryKey))
            return;

        if (_settingsTitleRoutine != null)
            StopCoroutine(_settingsTitleRoutine);

        _settingsTitleRoutine = StartCoroutine(RefreshSettingsTitleRoutine());
    }

    private IEnumerator RefreshSettingsTitleRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                _settingsStringTable,
                _settingsTitleEntryKey);

        yield return handle;

        if (_settingsTitleText != null)
        {
            string result = handle.Status == AsyncOperationStatus.Succeeded
                ? handle.Result
                : string.Empty;

            _settingsTitleText.text = string.IsNullOrWhiteSpace(result)
                ? _settingsTitleEntryKey
                : result;
        }

        _settingsTitleRoutine = null;
    }
}
