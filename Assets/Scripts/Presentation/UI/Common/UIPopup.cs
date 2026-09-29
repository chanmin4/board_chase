/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Implements the focused PopupType responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum PopupType
{
    Quit,
    NewGame,
    BackToMenu,
}

public class UIPopup : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private CanvasGroup _rootGroup;

    [Header("Localization")]
    [SerializeField] private TableReference _popupStringTable = "UI_Popup";
    [SerializeField] private string _titleKey = "Popup_Title";
    [SerializeField] private string _quitDescriptionKey = "Popup_QuitDescription";
    [SerializeField] private string _newGameDescriptionKey = "Popup_NewGameDescription";
    [SerializeField] private string _saveDescriptionKey = "Popup_SaveDescription";
    [SerializeField] private string _okButtonKey = "Popup_Ok";
    [SerializeField] private string _cancelButtonKey = "Popup_Cancel";

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _button1Text;
    [SerializeField] private TextMeshProUGUI _button2Text;

    [Header("Buttons")]
    [SerializeField] private UIGenericButton _popupButton1;
    [SerializeField] private UIGenericButton _popupButton2;

    private Coroutine _refreshTextRoutine;

    public event UnityAction<bool> ConfirmationResponseAction = delegate { };
    public event UnityAction ClosePopupAction = delegate { };

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_rootGroup == null)
            _rootGroup = GetComponent<CanvasGroup>();

        Hide();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        UnbindButtons();

        if (_refreshTextRoutine != null)
        {
            StopCoroutine(_refreshTextRoutine);
            _refreshTextRoutine = null;
        }
    }

    // Purpose: Shows the show UI or visual state.
    public void Show(PopupType popupType)
    {
        SetPopup(popupType);
        SetVisible(true);
    }

    // Purpose: Hides the hide UI or visual state.
    public void Hide()
    {
        SetVisible(false);
    }

    // Purpose: Updates the popup state.
    public void SetPopup(PopupType popupType)
    {
        UnbindButtons();

        RefreshTexts(popupType);

        if (_popupButton1 != null)
        {
            _popupButton1.SetButton(true);
            _popupButton1.Clicked += ConfirmButtonClicked;
        }

        if (_popupButton2 != null)
            _popupButton2.Clicked += CancelButtonClicked;
    }

    // Purpose: Closes the active popup button clicked UI or workflow.
    public void ClosePopupButtonClicked()
    {
        ClosePopupAction.Invoke();
    }

    // Purpose: Handles the incoming locale changed request or event.
    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        // 현재 팝업 타입 재저장은 안 하므로, 열려 있는 동안 언어 바꾸는 경우가 필요하면 타입 캐시를 추가하면 됨.
    }

    // Purpose: Refreshes the displayed or cached texts.
    private void RefreshTexts(PopupType popupType)
    {
        if (_refreshTextRoutine != null)
            StopCoroutine(_refreshTextRoutine);

        _refreshTextRoutine = StartCoroutine(RefreshTextsRoutine(popupType));
    }

    // Purpose: Refreshes the displayed or cached texts routine.
    private IEnumerator RefreshTextsRoutine(PopupType popupType)
    {
        yield return LocalizationSettings.InitializationOperation;

        yield return SetLocalizedText(_titleText, _titleKey);
        yield return SetLocalizedText(_descriptionText, ResolveDescriptionKey(popupType));
        yield return SetLocalizedText(_button1Text, _okButtonKey);
        yield return SetLocalizedText(_button2Text, _cancelButtonKey);

        _refreshTextRoutine = null;
    }

    // Purpose: Resolves the description key needed by this component.
    private string ResolveDescriptionKey(PopupType popupType)
    {
        switch (popupType)
        {
            case PopupType.NewGame:
                return _newGameDescriptionKey;

            case PopupType.BackToMenu:
            case PopupType.Quit:
                return _saveDescriptionKey;

            default:
                return _quitDescriptionKey;
        }
    }

    // Purpose: Updates the localized text state.
    private IEnumerator SetLocalizedText(TextMeshProUGUI target, string key)
    {
        if (target == null || string.IsNullOrWhiteSpace(key))
            yield break;

        AsyncOperationHandle<string> handle =
            LocalizationSettings.StringDatabase.GetLocalizedStringAsync(_popupStringTable, key);

        yield return handle;

        string result = handle.Status == AsyncOperationStatus.Succeeded
            ? handle.Result
            : string.Empty;

        target.text = string.IsNullOrWhiteSpace(result) ? key : result;
    }

    // Purpose: Commits the current button clicked selection or change.
    private void ConfirmButtonClicked()
    {
        ConfirmationResponseAction.Invoke(true);
    }

    // Purpose: Checks whether cancel button clicked is currently permitted.
    private void CancelButtonClicked()
    {
        ConfirmationResponseAction.Invoke(false);
    }

    // Purpose: Removes the buttons binding.
    private void UnbindButtons()
    {
        if (_popupButton1 != null)
            _popupButton1.Clicked -= ConfirmButtonClicked;

        if (_popupButton2 != null)
            _popupButton2.Clicked -= CancelButtonClicked;
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_rootGroup == null)
            return;

        _rootGroup.alpha = visible ? 1f : 0f;
        _rootGroup.interactable = visible;
        _rootGroup.blocksRaycasts = visible;
    }
}
