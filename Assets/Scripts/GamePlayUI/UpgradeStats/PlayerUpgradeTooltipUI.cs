using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUpgradeTooltipUI : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("CanvasGroup on the tooltip prefab root. Used to show or hide the tooltip without destroying it.")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Tooltip("Background image for the speech bubble panel.")]
    [SerializeField] private Image _backgroundImage;

    [Tooltip("Text field used for the upgrade title, for example '1단계 바이러스 제거탄 강화 I'.")]
    [SerializeField] private TextMeshProUGUI _titleText;

    [Tooltip("Text field used for the upgrade description from the catalog.")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Tooltip("Text field used for the point cost, for example '1pt 소비'.")]
    [SerializeField] private TextMeshProUGUI _costText;

    [Tooltip("Text field used for the current node state, for example '상태: 활성화 가능'.")]
    [SerializeField] private TextMeshProUGUI _stateText;

    [Header("Position")]
    [Tooltip("Screen-space offset from the mouse cursor.")]
    [SerializeField] private Vector2 _screenOffset = new Vector2(24f, -12f);

    [Tooltip("Keeps the tooltip inside the screen after the layout size is rebuilt.")]
    [SerializeField] private bool _clampToScreen = true;

    private RectTransform _rectTransform;

    private void Reset()
    {
        CacheRefs();
    }

    private void Awake()
    {
        CacheRefs();
        Hide();
    }

    public void Show(PlayerUpgradeNodeViewData data, Vector2 screenPosition)
    {
        CacheRefs();

        if (_titleText != null)
            _titleText.text = $"{data.level}단계 {data.displayName}";

        if (_descriptionText != null)
            _descriptionText.text = data.description;

        if (_costText != null)
            _costText.text = data.isPurchased ? "소비 완료" : $"{data.cost}pt 소비";

        if (_stateText != null)
            _stateText.text = ResolveStateText(data.state);

        gameObject.SetActive(true);
        RebuildFlexibleSize();
        SetPosition(screenPosition);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    public void Hide()
    {
        CacheRefs();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    private void CacheRefs()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_backgroundImage == null)
            _backgroundImage = GetComponent<Image>();
    }

    private void RebuildFlexibleSize()
    {
        if (_rectTransform == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
    }

    private void SetPosition(Vector2 screenPosition)
    {
        if (_rectTransform == null)
            return;

        Vector2 targetPosition = screenPosition + _screenOffset;

        if (_clampToScreen)
            targetPosition = ClampToScreen(targetPosition);

        _rectTransform.position = targetPosition;
    }

    private Vector2 ClampToScreen(Vector2 screenPosition)
    {
        if (_rectTransform == null)
            return screenPosition;

        Vector2 size = _rectTransform.rect.size;
        Vector2 pivot = _rectTransform.pivot;

        float minX = size.x * pivot.x;
        float maxX = Screen.width - size.x * (1f - pivot.x);
        float minY = size.y * pivot.y;
        float maxY = Screen.height - size.y * (1f - pivot.y);

        screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);

        return screenPosition;
    }

    private string ResolveStateText(PlayerUpgradeNodeState state)
    {
        return state switch
        {
            PlayerUpgradeNodeState.Purchased => "상태: 활성화됨",
            PlayerUpgradeNodeState.Available => "상태: 활성화 가능",
            PlayerUpgradeNodeState.LockedByPoints => "상태: 포인트 부족",
            PlayerUpgradeNodeState.LockedByPreviousLevel => "상태: 이전 단계를 먼저 활성화해야 함",
            PlayerUpgradeNodeState.LockedInBeta => "상태: Beta 버전 잠금",
            _ => "상태: 비활성화"
        };
    }
}
