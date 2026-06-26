using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CreditPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup _panelGroup;
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

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        ApplyCreditText();
    }

    private void OnEnable()
    {
        if (_backButton != null)
            _backButton.onClick.AddListener(CloseScreen);

        if (_inputReader != null)
            _inputReader.MenuCloseEvent += CloseScreen;

        ApplyCreditText();
    }

    private void OnDisable()
    {
        if (_backButton != null)
            _backButton.onClick.RemoveListener(CloseScreen);

        if (_inputReader != null)
            _inputReader.MenuCloseEvent -= CloseScreen;
    }

    public void OpenCreditPanel()
    {
        ApplyCreditText();

        if (_resetScrollToTopOnOpen)
            ResetScrollToTop();
    }

    public void CloseScreen()
    {
        Closed.Invoke();
    }

    private void ResolveRefs()
    {
        if (_panelGroup == null)
            _panelGroup = GetComponent<CanvasGroup>();

        if (_scrollRect == null)
            _scrollRect = GetComponentInChildren<ScrollRect>(true);

        if (_creditText == null)
            _creditText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (_inputReader == null)
            _inputReader = FindAnyObjectByType<InputReader>();
    }

    private void ApplyCreditText()
    {
        if (_creditText == null)
            return;

        string text = _creditTextAsset != null
            ? _creditTextAsset.text
            : _inlineCreditText;

        _creditText.text = text ?? string.Empty;
    }

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
