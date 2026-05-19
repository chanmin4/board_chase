using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class UISettingGeneralComponent : MonoBehaviour
{
    [Header("Fields")]
    [SerializeField] private UISettingItemFiller _languageField;
    [SerializeField] private UISettingItemFiller _resolutionField;
    [SerializeField] private UISettingItemFiller _fullscreenField;
    [SerializeField] private UISettingItemFiller _aimSensitivityField;

    [Header("Language")]
    [SerializeField] private string[] _languageLocaleCodes = { "en", "ko" };

    [Header("Aim")]
    [SerializeField, Min(1)] private int _aimSensitivitySteps = 20;
    [SerializeField] private float _minAimSensitivity = 0.2f;
    [SerializeField] private float _maxAimSensitivity = 3f;

    public event UnityAction<Locale, int, bool, float> SaveRequested = delegate { };

    private readonly List<Locale> _languageLocales = new();
    private List<Resolution> _resolutionsList = new();

    private AsyncOperationHandle _initializeOperation;

    private int _currentLanguageIndex;
    private int _currentResolutionIndex;
    private bool _isFullscreen;
    private int _aimSensitivityIndex;

    private void OnEnable()
    {
        if (_languageField != null)
            _languageField.OnOptionSelected += SelectLanguage;

        if (_resolutionField != null)
        {
            _resolutionField.OnNextOption += NextResolution;
            _resolutionField.OnPreviousOption += PreviousResolution;
        }

        if (_fullscreenField != null)
        {
            _fullscreenField.OnNextOption += EnableFullscreen;
            _fullscreenField.OnPreviousOption += DisableFullscreen;
        }

        if (_aimSensitivityField != null)
        {
            _aimSensitivityField.OnNextOption += IncreaseAimSensitivity;
            _aimSensitivityField.OnPreviousOption += DecreaseAimSensitivity;
        }

        _initializeOperation = LocalizationSettings.SelectedLocaleAsync;
        if (_initializeOperation.IsDone)
            InitializeLanguage(_initializeOperation);
        else
            _initializeOperation.Completed += InitializeLanguage;
    }

    private void OnDisable()
    {
        if (_languageField != null)
            _languageField.OnOptionSelected -= SelectLanguage;

        if (_resolutionField != null)
        {
            _resolutionField.OnNextOption -= NextResolution;
            _resolutionField.OnPreviousOption -= PreviousResolution;
        }

        if (_fullscreenField != null)
        {
            _fullscreenField.OnNextOption -= EnableFullscreen;
            _fullscreenField.OnPreviousOption -= DisableFullscreen;
        }

        if (_aimSensitivityField != null)
        {
            _aimSensitivityField.OnNextOption -= IncreaseAimSensitivity;
            _aimSensitivityField.OnPreviousOption -= DecreaseAimSensitivity;
        }

        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    public void Setup(SettingsSO settings)
    {
        if (settings == null)
            return;

        _resolutionsList = GetResolutionsList();

        _currentResolutionIndex = Mathf.Clamp(settings.ResolutionsIndex, 0, _resolutionsList.Count - 1);
        _isFullscreen = settings.IsFullscreen;
        _aimSensitivityIndex = AimSensitivityToIndex(settings.AimSensitivity);

        RefreshResolutionField();
        RefreshFullscreenField();
        RefreshAimSensitivityField();
    }

    private void InitializeLanguage(AsyncOperationHandle handle)
    {
        _initializeOperation.Completed -= InitializeLanguage;

        _languageLocales.Clear();

        for (int i = 0; i < _languageLocaleCodes.Length; i++)
        {
            Locale locale = FindLocale(_languageLocaleCodes[i]);
            if (locale != null)
                _languageLocales.Add(locale);
        }

        _currentLanguageIndex = ResolveLanguageIndex(LocalizationSettings.SelectedLocale);

        RefreshLanguageField();
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private Locale FindLocale(string localeCode)
    {
        List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;

        for (int i = 0; i < locales.Count; i++)
        {
            Locale locale = locales[i];

            if (locale != null && locale.Identifier.Code == localeCode)
                return locale;
        }

        return null;
    }

    private int ResolveLanguageIndex(Locale locale)
    {
        int index = _languageLocales.IndexOf(locale);
        return index >= 0 ? index : 0;
    }

    private void SelectLanguage(int index)
    {
        if (_languageLocales.Count == 0)
            return;

        _currentLanguageIndex = Mathf.Clamp(index, 0, _languageLocales.Count - 1);
        ApplyLanguage();
        SaveCurrentSettings();
    }

    private void ApplyLanguage()
    {
        if (_currentLanguageIndex < 0 || _currentLanguageIndex >= _languageLocales.Count)
            return;

        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        LocalizationSettings.SelectedLocale = _languageLocales[_currentLanguageIndex];
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

        RefreshLanguageField();
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        int index = _languageLocales.IndexOf(locale);
        if (index < 0)
            return;

        _currentLanguageIndex = index;
        RefreshLanguageField();
    }

    private void NextResolution()
    {
        _currentResolutionIndex = Mathf.Clamp(_currentResolutionIndex + 1, 0, _resolutionsList.Count - 1);
        ApplyResolution();
        SaveCurrentSettings();
    }

    private void PreviousResolution()
    {
        _currentResolutionIndex = Mathf.Clamp(_currentResolutionIndex - 1, 0, _resolutionsList.Count - 1);
        ApplyResolution();
        SaveCurrentSettings();
    }

    private void ApplyResolution()
    {
        if (_resolutionsList == null || _resolutionsList.Count == 0)
            return;

        Resolution resolution = _resolutionsList[_currentResolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, _isFullscreen);
        RefreshResolutionField();
    }

    private void EnableFullscreen()
    {
        _isFullscreen = true;
        ApplyFullscreen();
        SaveCurrentSettings();
    }

    private void DisableFullscreen()
    {
        _isFullscreen = false;
        ApplyFullscreen();
        SaveCurrentSettings();
    }

    private void ApplyFullscreen()
    {
        Screen.fullScreen = _isFullscreen;
        ApplyResolution();
        RefreshFullscreenField();
    }

    private void IncreaseAimSensitivity()
    {
        _aimSensitivityIndex = Mathf.Clamp(_aimSensitivityIndex + 1, 0, _aimSensitivitySteps);
        RefreshAimSensitivityField();
        SaveCurrentSettings();
    }

    private void DecreaseAimSensitivity()
    {
        _aimSensitivityIndex = Mathf.Clamp(_aimSensitivityIndex - 1, 0, _aimSensitivitySteps);
        RefreshAimSensitivityField();
        SaveCurrentSettings();
    }

    private void SaveCurrentSettings()
    {
        if (_currentLanguageIndex < 0 || _currentLanguageIndex >= _languageLocales.Count)
            return;

        SaveRequested.Invoke(
            _languageLocales[_currentLanguageIndex],
            _currentResolutionIndex,
            _isFullscreen,
            IndexToAimSensitivity(_aimSensitivityIndex));
    }

    private void RefreshLanguageField()
    {
        if (_languageField == null || _languageLocales.Count == 0)
            return;

        _languageField.FillDropdownLocalized(_currentLanguageIndex);
    }

    private void RefreshResolutionField()
    {
        if (_resolutionField == null || _resolutionsList == null || _resolutionsList.Count == 0)
            return;

        Resolution resolution = _resolutionsList[_currentResolutionIndex];
        _resolutionField.FillSettingField(
            _resolutionsList.Count,
            _currentResolutionIndex,
            $"{resolution.width} x {resolution.height}");
    }

    private void RefreshFullscreenField()
    {
        if (_fullscreenField == null)
            return;

        _fullscreenField.FillSettingField_Localized(
            2,
            _isFullscreen ? 1 : 0,
            _isFullscreen ? "On" : "Off");
    }

    private void RefreshAimSensitivityField()
    {
        if (_aimSensitivityField == null)
            return;

        float sensitivity = IndexToAimSensitivity(_aimSensitivityIndex);
        _aimSensitivityField.FillSettingField(
            _aimSensitivitySteps + 1,
            _aimSensitivityIndex,
            sensitivity.ToString("0.00"));
    }

    private List<Resolution> GetResolutionsList()
    {
        List<Resolution> options = new();

        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
            options.Add(resolutions[i]);

        if (options.Count == 0)
            options.Add(Screen.currentResolution);

        return options;
    }

    private int AimSensitivityToIndex(float sensitivity)
    {
        float t = Mathf.InverseLerp(_minAimSensitivity, _maxAimSensitivity, sensitivity);
        return Mathf.RoundToInt(t * _aimSensitivitySteps);
    }

    private float IndexToAimSensitivity(int index)
    {
        float t = _aimSensitivitySteps > 0 ? index / (float)_aimSensitivitySteps : 0f;
        return Mathf.Lerp(_minAimSensitivity, _maxAimSensitivity, t);
    }
}