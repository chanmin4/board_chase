using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SystemMessageUI : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip("전체 시스템 메시지 박스 CanvasGroup입니다. 비워두면 이 오브젝트에서 찾거나 자동 추가합니다.")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Tooltip("메시지가 하나도 없을 때 박스를 숨길지 여부입니다. 채팅창처럼 항상 보이게 하려면 false.")]
    [SerializeField] private bool _hideWhenEmpty = false;

    [Header("Scroll")]
    [Tooltip("시스템 메시지를 표시하는 ScrollRect입니다.")]
    [SerializeField] private ScrollRect _scrollRect;

    [Tooltip("메시지 줄들이 생성될 Content RectTransform입니다.")]
    [SerializeField] private RectTransform _contentRoot;

    [Header("Message Line")]
    [Tooltip("메시지 한 줄 프리팹입니다. TextMeshProUGUI가 붙은 프리팹을 넣으세요.")]
    [SerializeField] private TextMeshProUGUI _messageLinePrefab;

    [Tooltip("최대 보관 메시지 수입니다. 오래된 메시지는 자동 삭제됩니다.")]
    [SerializeField, Min(1)] private int _maxMessageCount = 80;

    [Tooltip("메시지 앞에 붙일 접두사입니다. 예: [System]")]
    [SerializeField] private string _messagePrefix = "[System] ";

    [Tooltip("메시지 수신 후 항상 맨 아래로 자동 스크롤합니다.")]
    [SerializeField] private bool _autoScrollToBottom = true;

    [Header("Listening")]
    [Tooltip("시스템 메시지 이벤트 채널입니다.")]
    [SerializeField] private SystemMessageEventChannelSO _messageChannel;

    private readonly Queue<TextMeshProUGUI> _messageLines = new();
    private Coroutine _scrollCoroutine;

    private void Awake()
    {
        EnsureCanvasGroup();

        if (_hideWhenEmpty)
            SetVisible(false);
        else
            SetVisible(true);

        ScrollToBottomImmediate();
    }

    private void OnEnable()
    {
        if (_messageChannel != null)
            _messageChannel.OnEventRaised += AddMessage;
    }

    private void OnDisable()
    {
        if (_messageChannel != null)
            _messageChannel.OnEventRaised -= AddMessage;
    }

    public void AddMessage(string message)
    {
        AddMessage(new SystemMessageRequest(message, 0f));
    }

    public void ClearMessages()
    {
        while (_messageLines.Count > 0)
        {
            TextMeshProUGUI line = _messageLines.Dequeue();

            if (line != null)
                Destroy(line.gameObject);
        }

        if (_hideWhenEmpty)
            SetVisible(false);

        ScrollToBottomImmediate();
    }

    private void AddMessage(SystemMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.message))
            return;

        if (_messageLinePrefab == null || _contentRoot == null)
        {
            Debug.LogWarning("[SystemMessageUI] Message line prefab or content root is missing.", this);
            return;
        }

        TextMeshProUGUI line = Instantiate(_messageLinePrefab, _contentRoot);
        line.text = $"{_messagePrefix}{request.message}";
        line.raycastTarget = false;

        _messageLines.Enqueue(line);

        TrimOldMessages();

        SetVisible(true);

        if (_autoScrollToBottom)
            RequestScrollToBottom();
    }

    private void TrimOldMessages()
    {
        while (_messageLines.Count > _maxMessageCount)
        {
            TextMeshProUGUI oldLine = _messageLines.Dequeue();

            if (oldLine != null)
                Destroy(oldLine.gameObject);
        }
    }

    private void RequestScrollToBottom()
    {
        if (_scrollCoroutine != null)
            StopCoroutine(_scrollCoroutine);

        _scrollCoroutine = StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        ScrollToBottomImmediate();

        yield return null;

        Canvas.ForceUpdateCanvases();
        ScrollToBottomImmediate();

        _scrollCoroutine = null;
    }

    private void ScrollToBottomImmediate()
    {
        if (_scrollRect == null)
            return;

        _scrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetVisible(bool visible)
    {
        EnsureCanvasGroup();

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null)
            return;

        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
}