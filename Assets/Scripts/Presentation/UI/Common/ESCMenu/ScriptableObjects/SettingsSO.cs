/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu > ScriptableObjects
 * Primary responsibility: Defines the SettingsSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "Settings", menuName = "Settings/Create new settings SO")]
public class SettingsSO : ScriptableObject
{
    [SerializeField] private float _masterVolume = 1f;
    [SerializeField] private float _musicVolume = 1f;
    [SerializeField] private float _sfxVolume = 1f;

    [SerializeField] private int _resolutionsIndex = 0;
    [SerializeField] private bool _isFullscreen = true;
    [SerializeField] private float _aimSensitivity = 1f;

    [SerializeField] private bool _useVSync = true;
    [SerializeField] private int _targetFrameRate = 60;

    [SerializeField] private int _antiAliasingIndex = 0;
    [SerializeField] private float _shadowDistance = 50f;

    [SerializeField] private Locale _currentLocale = default;

    public float MasterVolume => _masterVolume;
    public float MusicVolume => _musicVolume;
    public float SfxVolume => _sfxVolume;

    public int ResolutionsIndex => _resolutionsIndex;
    public bool IsFullscreen => _isFullscreen;
    public float AimSensitivity => _aimSensitivity;

    public bool UseVSync => _useVSync;
    public int TargetFrameRate => _targetFrameRate;

    public int AntiAliasingIndex => _antiAliasingIndex;
    public float ShadowDistance => _shadowDistance;

    public Locale CurrentLocale => _currentLocale;

    // Purpose: Saves the current general settings state.
    public void SaveGeneralSettings(
        Locale locale,
        int resolutionIndex,
        bool isFullscreen,
        float aimSensitivity)
    {
        _currentLocale = locale;
        _resolutionsIndex = resolutionIndex;
        _isFullscreen = isFullscreen;
        _aimSensitivity = Mathf.Max(0.01f, aimSensitivity);
    }

    // Purpose: Saves the current audio settings state.
    public void SaveAudioSettings(float newMusicVolume, float newSfxVolume, float newMasterVolume)
    {
        _masterVolume = Mathf.Clamp01(newMasterVolume);
        _musicVolume = Mathf.Clamp01(newMusicVolume);
        _sfxVolume = Mathf.Clamp01(newSfxVolume);
    }

    // Purpose: Saves the current graphics settings state.
    public void SaveGraphicsSettings(bool useVSync, int targetFrameRate)
    {
        _useVSync = useVSync;
        _targetFrameRate = Mathf.Max(1, targetFrameRate);
    }

    // Purpose: Loads saved settings into the active runtime state.
    public void LoadSavedSettings(DeviceSettingsSaveData savedFile)
    {
        if (savedFile == null)
            return;

        savedFile.EnsureDefaults();

        _masterVolume = savedFile.masterVolume;
        _musicVolume = savedFile.musicVolume;
        _sfxVolume = savedFile.sfxVolume;

        _resolutionsIndex = savedFile.resolutionIndex;
        _isFullscreen = savedFile.isFullscreen;
        _aimSensitivity = savedFile.aimSensitivity;

        _useVSync = savedFile.useVSync;
        _targetFrameRate = savedFile.targetFrameRate;

        _antiAliasingIndex = savedFile.antiAliasingIndex;
        _shadowDistance = savedFile.shadowDistance;
    }

    // Purpose: Assigns the resolved locale after localization initialization is complete.
    public void SetLocale(Locale locale)
    {
        if (locale != null)
            _currentLocale = locale;
    }
}
