/*
 * Role hierarchy: Presentation > UI > Common > ESCMenu
 * Primary responsibility: Implements the focused UISettingTabFiller responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_button == null)
            _button = GetComponentInChildren<Button>(true);

        SetSelected(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(Click);

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RefreshTitle();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
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

    // Purpose: Updates the tab state.
    public void SetTab(SettingsType settingTab, bool isSelected)
    {
        _settingTab = settingTab;
        RefreshTitle();
        SetSelected(isSelected);
    }

    // Purpose: Updates the selected state.
    public void SetSelected(bool isSelected)
    {
        if (_activeOverlayGroup == null)
            return;

        _activeOverlayGroup.alpha = isSelected ? 1f : 0f;
        _activeOverlayGroup.interactable = false;
        _activeOverlayGroup.blocksRaycasts = false;
    }

    // Purpose: Performs the click workflow for this component.
    public void Click()
    {
        Clicked.Invoke(_settingTab);
    }

    // Purpose: Handles the incoming locale changed request or event.
    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        RefreshTitle();
    }

    // Purpose: Refreshes the displayed or cached title.
    private void RefreshTitle()
    {
        if (_titleText == null || string.IsNullOrWhiteSpace(_titleEntryKey))
            return;

        if (_titleRoutine != null)
            StopCoroutine(_titleRoutine);

        _titleRoutine = StartCoroutine(RefreshTitleRoutine());
    }

    // Purpose: Refreshes the displayed or cached title routine.
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
