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

    [Header("Persistent Progress")]
    public int _roguelikeCurrency;
    public MetaUpgradeSaveData _metaUpgrades = new MetaUpgradeSaveData();

    public MetaUpgradeSaveData MetaUpgrades
    {
        get
        {
            if (_metaUpgrades == null)
                _metaUpgrades = new MetaUpgradeSaveData();

            return _metaUpgrades;
        }
    }

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
        EnsureRuntimeDefaults();
        return JsonUtility.ToJson(this);
    }

    public void LoadFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        EnsureRuntimeDefaults();
    }

    public void EnsureRuntimeDefaults()
    {
        _roguelikeCurrency = Mathf.Max(0, _roguelikeCurrency);
        MetaUpgrades.ClampNonNegative();
    }
}

[Serializable]
public class MetaUpgradeSaveData
{
    public int maxHealthLevel;
    public int attackDamageLevel;
    public int magazineSizeLevel;
    public int moveSpeedLevel;
    public int reloadDurationLevel;
    public int attackSpeedLevel;
    public int paintSpeedLevel;

    public int GetLevel(MetaUpgradeId id)
    {
        return id switch
        {
            MetaUpgradeId.MaxHealth => maxHealthLevel,
            MetaUpgradeId.AttackDamage => attackDamageLevel,
            MetaUpgradeId.MagazineSize => magazineSizeLevel,
            MetaUpgradeId.MoveSpeed => moveSpeedLevel,
            MetaUpgradeId.ReloadDuration => reloadDurationLevel,
            MetaUpgradeId.AttackSpeed => attackSpeedLevel,
            MetaUpgradeId.PaintSpeed => paintSpeedLevel,
            _ => 0
        };
    }

    public void SetLevel(MetaUpgradeId id, int level)
    {
        level = Mathf.Max(0, level);

        switch (id)
        {
            case MetaUpgradeId.MaxHealth:
                maxHealthLevel = level;
                break;

            case MetaUpgradeId.AttackDamage:
                attackDamageLevel = level;
                break;

            case MetaUpgradeId.MagazineSize:
                magazineSizeLevel = level;
                break;

            case MetaUpgradeId.MoveSpeed:
                moveSpeedLevel = level;
                break;

            case MetaUpgradeId.ReloadDuration:
                reloadDurationLevel = level;
                break;

            case MetaUpgradeId.AttackSpeed:
                attackSpeedLevel = level;
                break;

            case MetaUpgradeId.PaintSpeed:
                paintSpeedLevel = level;
                break;
        }
    }

    public void IncreaseLevel(MetaUpgradeId id)
    {
        SetLevel(id, GetLevel(id) + 1);
    }

    public void ClampNonNegative()
    {
        maxHealthLevel = Mathf.Max(0, maxHealthLevel);
        attackDamageLevel = Mathf.Max(0, attackDamageLevel);
        magazineSizeLevel = Mathf.Max(0, magazineSizeLevel);
        moveSpeedLevel = Mathf.Max(0, moveSpeedLevel);
        reloadDurationLevel = Mathf.Max(0, reloadDurationLevel);
        attackSpeedLevel = Mathf.Max(0, attackSpeedLevel);
        paintSpeedLevel = Mathf.Max(0, paintSpeedLevel);
    }
}
