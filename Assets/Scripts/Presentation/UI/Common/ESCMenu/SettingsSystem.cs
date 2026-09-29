/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu
 * Primary responsibility: Coordinates the SettingsSystem subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;

public class SettingsSystem : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO SaveSettingsEvent = default;

    [SerializeField] private SettingsSO _currentSettings = default;
    [SerializeField] private SaveSystem _saveSystem = default;

    [SerializeField] private FloatEventChannelSO _changeMasterVolumeEventChannel = default;
    [SerializeField] private FloatEventChannelSO _changeSFXVolumeEventChannel = default;
    [SerializeField] private FloatEventChannelSO _changeMusicVolumeEventChannel = default;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        _saveSystem.LoadSaveDataFromDisk();
        _currentSettings.LoadSavedSettings(_saveSystem.DeviceSettings);
        SetCurrentSettings();
        _saveSystem.MarkRuntimeSettingsInitialized();
        StartCoroutine(ApplySavedLocaleAfterInitialization());
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (SaveSettingsEvent != null)
            SaveSettingsEvent.OnEventRaised += SaveSettings;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (SaveSettingsEvent != null)
            SaveSettingsEvent.OnEventRaised -= SaveSettings;
    }

    // Purpose: Updates the current settings state.
    private void SetCurrentSettings()
    {
        if (_changeMusicVolumeEventChannel != null)
            _changeMusicVolumeEventChannel.RaiseEvent(_currentSettings.MusicVolume);

        if (_changeSFXVolumeEventChannel != null)
            _changeSFXVolumeEventChannel.RaiseEvent(_currentSettings.SfxVolume);

        if (_changeMasterVolumeEventChannel != null)
            _changeMasterVolumeEventChannel.RaiseEvent(_currentSettings.MasterVolume);

        Resolution currentResolution = Screen.currentResolution;

        if (_currentSettings.ResolutionsIndex >= 0 &&
            _currentSettings.ResolutionsIndex < Screen.resolutions.Length)
        {
            currentResolution = Screen.resolutions[_currentSettings.ResolutionsIndex];
        }

        Screen.SetResolution(
            currentResolution.width,
            currentResolution.height,
            _currentSettings.IsFullscreen);

        QualitySettings.vSyncCount = _currentSettings.UseVSync ? 1 : 0;
        Application.targetFrameRate = _currentSettings.UseVSync
            ? -1
            : _currentSettings.TargetFrameRate;

        if (_currentSettings.CurrentLocale != null)
            LocalizationSettings.SelectedLocale = _currentSettings.CurrentLocale;
    }

    // Purpose: Saves the current settings state.
    private void SaveSettings()
    {
        _saveSystem.RequestSettingsSave();
    }

    // Purpose: Resolves the persisted locale code after Unity Localization has populated its locale list.
    private IEnumerator ApplySavedLocaleAfterInitialization()
    {
        yield return LocalizationSettings.InitializationOperation;

        if (_saveSystem == null || _currentSettings == null)
            yield break;

        string localeCode = _saveSystem.DeviceSettings.localeCode;
        if (string.IsNullOrWhiteSpace(localeCode))
            yield break;

        List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
        for (int index = 0; index < locales.Count; index++)
        {
            Locale locale = locales[index];
            if (locale == null || locale.Identifier.Code != localeCode)
                continue;

            _currentSettings.SetLocale(locale);
            LocalizationSettings.SelectedLocale = locale;
            yield break;
        }
    }
}
