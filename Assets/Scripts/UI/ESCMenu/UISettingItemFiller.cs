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

    private void OnEnable()
    {
        RefreshTitle();

        if (_dropdown != null)
            _dropdown.onValueChanged.AddListener(HandleDropdownChanged);

        if (_slider != null)
            _slider.onValueChanged.AddListener(HandleSliderChanged);

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

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

    public void FillDropdownLocalized(int selectedIndex)
    {
        _controlType = UISettingControlType.Dropdown;
        _usesLocalizedDropdown = true;

        if (_localizedDropdownRoutine != null)
            StopCoroutine(_localizedDropdownRoutine);

        _localizedDropdownRoutine = StartCoroutine(FillDropdownLocalizedRoutine(selectedIndex));
    }

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

    public void FillSettingField(IReadOnlyList<string> options, int selectedIndex)
    {
        FillDropdown(options, selectedIndex);
    }

    public void FillSettingField(int optionCount, int selectedIndex, string selectedOptionText)
    {
        FillDropdown(BuildFallbackOptions(optionCount, selectedIndex, selectedOptionText), selectedIndex);
    }

    public void FillSettingField_Localized(int optionCount, int selectedIndex, string selectedOptionText)
    {
        FillSettingField(optionCount, selectedIndex, selectedOptionText);
    }

    public void SetInteractable(bool interactable)
    {
        if (_dropdown != null)
            _dropdown.interactable = interactable;

        if (_slider != null)
            _slider.interactable = interactable;
    }

    public void SelectItem() { }
    public void UnselectItem() { }

    public void SetNavigation(MultiInputButton buttonToSelectOnDown, MultiInputButton buttonToSelectOnUp)
    {
    }

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

    private void HandleSliderChanged(float value)
    {
        if (_ignoreCallback)
            return;

        CurrentSliderValue = value;
        RefreshSliderText();
        OnSliderValueChanged.Invoke(value);
    }

    private void HandleLocaleChanged(Locale locale)
    {
        RefreshTitle();

        if (_usesLocalizedDropdown && _refreshLocalizedDropdownOnLocaleChanged)
            FillDropdownLocalized(CurrentIndex);
    }

    private void RefreshTitle()
    {
        if (!string.IsNullOrWhiteSpace(_titleEntryKey))
        {
            if (_titleLocalizedEvent != null)
            {
                _titleLocalizedEvent.StringReference.TableReference = _stringTable;
                _titleLocalizedEvent.StringReference.TableEntryReference = _titleEntryKey;
                _titleLocalizedEvent.RefreshString();
                return;
            }

            if (_titleText != null)
            {
                if (_localizedTitleRoutine != null)
                    StopCoroutine(_localizedTitleRoutine);

                _localizedTitleRoutine = StartCoroutine(RefreshTitleRoutine(_titleEntryKey));
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

    private void RefreshSliderText()
    {
        if (_sliderValueText == null)
            return;

        float displayValue = CurrentSliderValue * _sliderDisplayMultiplier;
        _sliderValueText.text = displayValue.ToString(_sliderValueFormat);
    }

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