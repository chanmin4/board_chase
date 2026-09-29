/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Presents CreditPanel state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CreditPanel : PanelUI
{
    [Header("Refs")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private TextMeshProUGUI _creditText;
    [SerializeField] private Button _backButton;
    [SerializeField] private InputReader _inputReader;

    [Header("Credit Text")]
    [Tooltip("Optional text asset for long credits. If assigned, it overrides Inline Credit Text.")]
    [SerializeField] private TextAsset _creditTextAsset;

    [TextArea(8, 30)]
    [SerializeField] private string _inlineCreditText;

    [Header("Open")]
    [SerializeField] private bool _resetScrollToTopOnOpen = true;

    public event UnityAction Closed = delegate { };

    // Purpose: Initializes Inspector defaults for this component.
    protected override void Reset()
    {
        base.Reset();
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ResolveRefs();
        ApplyCreditText();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(CloseScreen);

        if (_inputReader != null)
            _inputReader.MenuCloseEvent += CloseScreen;

        ApplyCreditText();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(CloseScreen);

        if (_inputReader != null)
            _inputReader.MenuCloseEvent -= CloseScreen;
    }

    // Purpose: Opens the credit panel UI or workflow.
    public void OpenCreditPanel()
    {
        ApplyCreditText();

        if (_resetScrollToTopOnOpen)
            ResetScrollToTop();
    }

    // Purpose: Closes the active screen UI or workflow.
    public void CloseScreen()
    {
        Closed.Invoke();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (_creditText == null)
            _creditText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();
    }

    // Purpose: Applies the requested credit text change.
    private void ApplyCreditText()
    {
        if (_creditText == null)
            return;

        string text = _creditTextAsset != null
            ? _creditTextAsset.text
            : _inlineCreditText;

        _creditText.text = text ?? string.Empty;
    }

    // Purpose: Performs the reset scroll to top workflow for this component.
    private void ResetScrollToTop()
    {
        if (_scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 1f;
        _scrollRect.horizontalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}
