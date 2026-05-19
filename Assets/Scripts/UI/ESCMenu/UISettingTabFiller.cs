using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class UISettingTabFiller : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Button _button;

    [Header("Active Visual")]
    [SerializeField] private CanvasGroup _activeOverlayGroup;

    [Header("Localization")]
    [SerializeField] private TableReference _stringTable = "UI_Settings";
    [SerializeField] private string _titleEntryKey;
    [SerializeField] private TextMeshProUGUI _titleText;

    private SettingsType _settingTab;
    private Coroutine _titleRoutine;

    public SettingsType SettingTab => _settingTab;
    public event UnityAction<SettingsType> Clicked = delegate { };

    private void Awake()
    {
        if (_button == null)
            _button = GetComponentInChildren<Button>(true);

        SetSelected(false);
    }

    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(Click);

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshTitle();
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(Click);

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_titleRoutine != null)
        {
            StopCoroutine(_titleRoutine);
            _titleRoutine = null;
        }
    }

    public void SetTab(SettingsType settingTab, bool isSelected)
    {
        _settingTab = settingTab;
        RefreshTitle();
        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (_activeOverlayGroup == null)
            return;

        _activeOverlayGroup.alpha = isSelected ? 1f : 0f;
        _activeOverlayGroup.interactable = false;
        _activeOverlayGroup.blocksRaycasts = false;
    }

    public void Click()
    {
        Clicked.Invoke(_settingTab);
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RefreshTitle();
    }

    private void RefreshTitle()
    {
        if (_titleText == null || string.IsNullOrWhiteSpace(_titleEntryKey))
            return;

        if (_titleRoutine != null)
            StopCoroutine(_titleRoutine);

        _titleRoutine = StartCoroutine(RefreshTitleRoutine());
    }

    private IEnumerator RefreshTitleRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(_stringTable, _titleEntryKey);

        yield return handle;

        if (_titleText != null)
        {
            string result = handle.Status == AsyncOperationStatus.Succeeded
                ? handle.Result
                : string.Empty;

            _titleText.text = string.IsNullOrWhiteSpace(result)
                ? _titleEntryKey
                : result;
        }

        _titleRoutine = null;
    }
}