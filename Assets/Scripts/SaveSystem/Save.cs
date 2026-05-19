using System;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class Save
{
    public float _masterVolume = 1f;
    public float _musicVolume = 1f;
    public float _sfxVolume = 1f;

    public int _resolutionsIndex;
    public bool _isFullscreen = true;
    public float _aimSensitivity = 1f;

    public bool _useVSync = true;
    public int _targetFrameRate = 60;

    public int _antiAliasingIndex;
    public float _shadowDistance;

    public Locale _currentLocale = default;

    public void SaveSettings(SettingsSO settings)
    {
        _masterVolume = settings.MasterVolume;
        _musicVolume = settings.MusicVolume;
        _sfxVolume = settings.SfxVolume;

        _resolutionsIndex = settings.ResolutionsIndex;
        _isFullscreen = settings.IsFullscreen;
        _aimSensitivity = settings.AimSensitivity;

        _useVSync = settings.UseVSync;
        _targetFrameRate = settings.TargetFrameRate;

        _antiAliasingIndex = settings.AntiAliasingIndex;
        _shadowDistance = settings.ShadowDistance;

        _currentLocale = settings.CurrentLocale;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public void LoadFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
    }
}
