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

    public void SaveAudioSettings(float newMusicVolume, float newSfxVolume, float newMasterVolume)
    {
        _masterVolume = Mathf.Clamp01(newMasterVolume);
        _musicVolume = Mathf.Clamp01(newMusicVolume);
        _sfxVolume = Mathf.Clamp01(newSfxVolume);
    }

    public void SaveGraphicsSettings(bool useVSync, int targetFrameRate)
    {
        _useVSync = useVSync;
        _targetFrameRate = Mathf.Max(1, targetFrameRate);
    }

    public void LoadSavedSettings(Save savedFile)
    {
        _masterVolume = savedFile._masterVolume;
        _musicVolume = savedFile._musicVolume;
        _sfxVolume = savedFile._sfxVolume;

        _resolutionsIndex = savedFile._resolutionsIndex;
        _isFullscreen = savedFile._isFullscreen;
        _aimSensitivity = savedFile._aimSensitivity <= 0f ? 1f : savedFile._aimSensitivity;

        _useVSync = savedFile._useVSync;
        _targetFrameRate = savedFile._targetFrameRate <= 0 ? 60 : savedFile._targetFrameRate;

        _antiAliasingIndex = savedFile._antiAliasingIndex;
        _shadowDistance = savedFile._shadowDistance;

        _currentLocale = savedFile._currentLocale;
    }
}
