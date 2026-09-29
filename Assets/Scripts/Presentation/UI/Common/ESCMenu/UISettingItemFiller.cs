/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu
 * Primary responsibility: Implements the focused UISettingControlType responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
public enum UISettingControlType
{
    Dropdown,
    Slider
}

public class UISettingItemFiller : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private SettingFieldType _fieldType = default;
    [SerializeField] private UISettingControlType _controlType = UISettingControlType.Dropdown;

    [Header("Localization")]
    [SerializeField] private TableReference _stringTable = "UI_Settings";
    [SerializeField] private string _titleEntryKey;
    [SerializeField] private LocalizeStringEvent _titleLocalizedEvent;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private string _fallbackTitle;
    [SerializeField] private List<string> _dropdownOptionEntryKeys = new();
    [SerializeField] private bool _refreshLocalizedDropdownOnLocaleChanged = true;

    [Header("Dropdown")]
    [SerializeField] private TMP_Dropdown _dropdown;

    [Header("Slider")]
    [SerializeField] private Slider _slider;
    [SerializeField] private TextMeshProUGUI _sliderValueText;
    [SerializeField] private float _sliderDisplayMultiplier = 100f;
    [SerializeField] private string _sliderValueFormat = "0";

    public event UnityAction<int> OnOptionSelected = delegate { };
    public event UnityAction<float> OnSliderValueChanged = delegate { };

    public event UnityAction OnNextOption = delegate { };
    public event UnityAction OnPreviousOption = delegate { };
    private Coroutine _localizedDropdownRoutine;
    private Coroutine _localizedTitleRoutine;
    public SettingFieldType FieldType => _fieldType;
    public UISettingControlType ControlType => _controlType;
    public int CurrentIndex { get; private set; }
    public float CurrentSliderValue { get; private set; }

    private bool _ignoreCallback;
    private bool _usesLocalizedDropdown;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        RefreshTitle();

        if (_dropdown != null)
            _dropdown.onValueChanged.AddListener(HandleDropdownChanged);

        if (_slider != null)
            _slider.onValueChanged.AddListener(HandleSliderChanged);

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_dropdown != null)
            _dropdown.onValueChanged.RemoveListener(HandleDropdownChanged);

        if (_slider != null)
            _slider.onValueChanged.RemoveListener(HandleSliderChanged);
        if (_localizedDropdownRoutine != null)
        {
            StopCoroutine(_localizedDropdownRoutine);
            _localizedDropdownRoutine = null;
        }

        if (_localizedTitleRoutine != null)
        {
            StopCoroutine(_localizedTitleRoutine);
            _localizedTitleRoutine = null;
        }
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    // Purpose: Performs the fill dropdown workflow for this component.
    public void FillDropdown(IReadOnlyList<string> options, int selectedIndex)
    {
        _controlType = UISettingControlType.Dropdown;
        _usesLocalizedDropdown = false;

        if (_dropdown == null)
            return;

        _ignoreCallback = true;

        _dropdown.ClearOptions();

        List<string> optionList = new List<string>();
        if (options != null)
        {
            for (int i = 0; i < options.Count; i++)
                optionList.Add(options[i]);
        }

        if (optionList.Count == 0)
            optionList.Add("-");

        selectedIndex = Mathf.Clamp(selectedIndex, 0, optionList.Count - 1);

        _dropdown.AddOptions(optionList);
        _dropdown.SetValueWithoutNotify(selectedIndex);
        _dropdown.RefreshShownValue();

        CurrentIndex = selectedIndex;
        _ignoreCallback = false;

        RefreshTitle();
    }

    // Purpose: Performs the fill dropdown localized workflow for this component.
    public void FillDropdownLocalized(int selectedIndex)
    {
        _controlType = UISettingControlType.Dropdown;
        _usesLocalizedDropdown = true;

        if (_localizedDropdownRoutine != null)
            StopCoroutine(_localizedDropdownRoutine);

        _localizedDropdownRoutine = StartCoroutine(FillDropdownLocalizedRoutine(selectedIndex));
    }

    // Purpose: Performs the fill slider workflow for this component.
    public void FillSlider(float minValue, float maxValue, float currentValue)
    {
        _controlType = UISettingControlType.Slider;

        if (_slider == null)
            return;

        _ignoreCallback = true;

        _slider.minValue = minValue;
        _slider.maxValue = maxValue;
        _slider.SetValueWithoutNotify(Mathf.Clamp(currentValue, minValue, maxValue));

        CurrentSliderValue = _slider.value;
        RefreshSliderText();

        _ignoreCallback = false;

        RefreshTitle();
    }

    // Purpose: Performs the fill setting field workflow for this component.
    public void FillSettingField(IReadOnlyList<string> options, int selectedIndex)
    {
        FillDropdown(options, selectedIndex);
    }

    // Purpose: Performs the fill setting field workflow for this component.
    public void FillSettingField(int optionCount, int selectedIndex, string selectedOptionText)
    {
        FillDropdown(BuildFallbackOptions(optionCount, selectedIndex, selectedOptionText), selectedIndex);
    }

    // Purpose: Performs the fill setting field_localized workflow for this component.
    public void FillSettingField_Localized(int optionCount, int selectedIndex, string selectedOptionText)
    {
        FillSettingField(optionCount, selectedIndex, selectedOptionText);
    }

    // Purpose: Fills this field with option labels resolved from the assigned string table.
    public void FillLocalizedDropdown(IReadOnlyList<string> optionEntryKeys, int selectedIndex)
    {
        _controlType = UISettingControlType.Dropdown;
        _usesLocalizedDropdown = true;

        _dropdownOptionEntryKeys.Clear();

        if (optionEntryKeys != null)
        {
            for (int i = 0; i < optionEntryKeys.Count; i++)
                _dropdownOptionEntryKeys.Add(optionEntryKeys[i]);
        }

        if (_localizedDropdownRoutine != null)
            StopCoroutine(_localizedDropdownRoutine);

        _localizedDropdownRoutine = StartCoroutine(FillDropdownLocalizedRoutine(selectedIndex));
    }

    // Purpose: Updates the interactable state.
    public void SetInteractable(bool interactable)
    {
        if (_dropdown != null)
            _dropdown.interactable = interactable;

        if (_slider != null)
            _slider.interactable = interactable;
    }

    // Purpose: Selects item for the active workflow.
    public void SelectItem() { }
    // Purpose: Performs the unselect item workflow for this component.
    public void UnselectItem() { }

    // Purpose: Updates the navigation state.
    public void SetNavigation(MultiInputButton buttonToSelectOnDown, MultiInputButton buttonToSelectOnUp)
    {
    }

    // Purpose: Performs the fill dropdown internal workflow for this component.
    private void FillDropdownInternal(IReadOnlyList<string> options, int selectedIndex)
    {
        if (_dropdown == null)
            return;

        _ignoreCallback = true;

        _dropdown.ClearOptions();

        List<string> optionList = new List<string>();
        if (options != null)
        {
            for (int i = 0; i < options.Count; i++)
                optionList.Add(options[i]);
        }

        if (optionList.Count == 0)
            optionList.Add("-");

        selectedIndex = Mathf.Clamp(selectedIndex, 0, optionList.Count - 1);

        _dropdown.AddOptions(optionList);
        _dropdown.SetValueWithoutNotify(selectedIndex);
        _dropdown.RefreshShownValue();

        CurrentIndex = selectedIndex;
        _ignoreCallback = false;

        RefreshTitle();
    }

    // Purpose: Performs the fill dropdown localized routine workflow for this component.
    private IEnumerator FillDropdownLocalizedRoutine(int selectedIndex)
    {
        yield return LocalizationSettings.InitializationOperation;

        List<string> options = new List<string>();

        for (int i = 0; i < _dropdownOptionEntryKeys.Count; i++)
        {
            string key = _dropdownOptionEntryKeys[i];

            if (string.IsNullOrWhiteSpace(key))
            {
                options.Add("-");
                continue;
            }

            AsyncOperationHandle<string> handle =
                LocalizationSettings.StringDatabase.GetLocalizedStringAsync(_stringTable, key);

            yield return handle;

            string text = handle.Status == AsyncOperationStatus.Succeeded
                ? handle.Result
                : string.Empty;

            options.Add(string.IsNullOrWhiteSpace(text) ? key : text);
        }

        FillDropdownInternal(options, selectedIndex);
        _localizedDropdownRoutine = null;
    }
    // Purpose: Handles the incoming dropdown changed request or event.
    private void HandleDropdownChanged(int newIndex)
    {
        if (_ignoreCallback)
            return;

        int previousIndex = CurrentIndex;
        CurrentIndex = newIndex;

        OnOptionSelected.Invoke(newIndex);

        if (newIndex > previousIndex)
        {
            for (int i = previousIndex; i < newIndex; i++)
                OnNextOption.Invoke();
        }
        else if (newIndex < previousIndex)
        {
            for (int i = previousIndex; i > newIndex; i--)
                OnPreviousOption.Invoke();
        }
    }

    // Purpose: Handles the incoming slider changed request or event.
    private void HandleSliderChanged(float value)
    {
        if (_ignoreCallback)
            return;

        CurrentSliderValue = value;
        RefreshSliderText();
        OnSliderValueChanged.Invoke(value);
    }

    // Purpose: Handles the incoming locale changed request or event.
    private void HandleLocaleChanged(Locale locale)
    {
        RefreshTitle();

        if (_usesLocalizedDropdown && _refreshLocalizedDropdownOnLocaleChanged)
            FillDropdownLocalized(CurrentIndex);
    }

    // Purpose: Refreshes the displayed or cached title.
    private void RefreshTitle()
    {
        string titleEntryKey = string.IsNullOrWhiteSpace(_titleEntryKey)
            ? GetDefaultTitleEntryKey(_fieldType)
            : _titleEntryKey;

        if (!string.IsNullOrWhiteSpace(titleEntryKey))
        {
            if (_titleLocalizedEvent != null)
            {
                _titleLocalizedEvent.StringReference.TableReference = _stringTable;
                _titleLocalizedEvent.StringReference.TableEntryReference = titleEntryKey;
                _titleLocalizedEvent.RefreshString();
                return;
            }

            if (_titleText != null)
            {
                if (_localizedTitleRoutine != null)
                    StopCoroutine(_localizedTitleRoutine);

                _localizedTitleRoutine = StartCoroutine(RefreshTitleRoutine(titleEntryKey));
                return;
            }
        }

        if (_titleText != null)
        {
            _titleText.text = string.IsNullOrWhiteSpace(_fallbackTitle)
                ? _fieldType.ToString()
                : _fallbackTitle;
        }
    }
    // Purpose: Refreshes the displayed or cached title routine.
    private IEnumerator RefreshTitleRoutine(string key)
    {
        yield return LocalizationSettings.InitializationOperation;

        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(_stringTable, key);

        yield return handle;

        if (_titleText != null)
        {
            string text = handle.Status == AsyncOperationStatus.Succeeded
                ? handle.Result
                : string.Empty;

            _titleText.text = string.IsNullOrWhiteSpace(text) ? key : text;
        }

        _localizedTitleRoutine = null;
    }

    // Purpose: Refreshes the displayed or cached slider text.
    private void RefreshSliderText()
    {
        if (_sliderValueText == null)
            return;

        float displayValue = CurrentSliderValue * _sliderDisplayMultiplier;
        _sliderValueText.text = displayValue.ToString(_sliderValueFormat);
    }

    // Purpose: Maps unconfigured legacy template fields to their stable Settings table entries.
    private static string GetDefaultTitleEntryKey(SettingFieldType fieldType)
    {
        return fieldType switch
        {
            SettingFieldType.Language => "Settings_Content_LanguageTitle",
            SettingFieldType.Resolution => "Settings_Content_ResolutionTitle",
            SettingFieldType.Displaymode => "Settings_Content_DisplaymodeTitle",
            SettingFieldType.AimSensitivity => "Settings_Content_AimSensivityTitle",
            SettingFieldType.Volume_Master => "Settings_Field_MasterVolume",
            SettingFieldType.Volume_Music => "Settings_Field_MusicVolume",
            SettingFieldType.Volume_SFx => "Settings_Field_SfxVolume",
            SettingFieldType.VSync => "Settings_Field_VSync",
            SettingFieldType.FrameRateLimit => "Settings_Field_FrameRateLimit",
            _ => string.Empty
        };
    }

    // Purpose: Builds the fallback options from the current authored data.
    private static List<string> BuildFallbackOptions(
        int optionCount,
        int selectedIndex,
        string selectedOptionText)
    {
        optionCount = Mathf.Max(1, optionCount);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, optionCount - 1);

        List<string> options = new List<string>(optionCount);

        for (int i = 0; i < optionCount; i++)
            options.Add(i.ToString());

        if (!string.IsNullOrWhiteSpace(selectedOptionText))
            options[selectedIndex] = selectedOptionText;

        return options;
    }
}
