using UnityEngine;
using UnityEngine.Localization.Settings;

public class SettingsSystem : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO SaveSettingsEvent = default;

    [SerializeField] private SettingsSO _currentSettings = default;
    [SerializeField] private SaveSystem _saveSystem = default;

    [SerializeField] private FloatEventChannelSO _changeMasterVolumeEventChannel = default;
    [SerializeField] private FloatEventChannelSO _changeSFXVolumeEventChannel = default;
    [SerializeField] private FloatEventChannelSO _changeMusicVolumeEventChannel = default;

    private void Awake()
    {
        _saveSystem.LoadSaveDataFromDisk();
        _currentSettings.LoadSavedSettings(_saveSystem.saveData);
        SetCurrentSettings();
    }

    private void OnEnable()
    {
        if (SaveSettingsEvent != null)
            SaveSettingsEvent.OnEventRaised += SaveSettings;
    }

    private void OnDisable()
    {
        if (SaveSettingsEvent != null)
            SaveSettingsEvent.OnEventRaised -= SaveSettings;
    }

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

    private void SaveSettings()
    {
        _saveSystem.SaveDataToDisk();
    }
}
