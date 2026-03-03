using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
[Serializable]
public class Save
{
    public float _masterVolume;
    public float _musicVolume;
    public float _sfxVolume;

    public int _resolutionsIndex;
    public int _antiAliasingIndex;
    public float _shadowDistance;
    public bool _isFullscreen;

	public Locale _currentLocale = default;
    public void SaveSettings(SettingsSO settings)
    {
        _masterVolume = settings.MasterVolume;
        _musicVolume  = settings.MusicVolume;
        _sfxVolume    = settings.SfxVolume;

        _resolutionsIndex  = settings.ResolutionsIndex;
        _antiAliasingIndex = settings.AntiAliasingIndex;
        _shadowDistance    = settings.ShadowDistance;
        _isFullscreen      = settings.IsFullscreen;
        _currentLocale = settings.CurrentLocale;

    }

    public string ToJson() => JsonUtility.ToJson(this);

    public void LoadFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
    }
}