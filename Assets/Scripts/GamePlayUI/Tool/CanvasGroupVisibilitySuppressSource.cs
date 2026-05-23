using UnityEngine;

[DisallowMultipleComponent]
public class CanvasGroupVisibilitySuppressSource : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("이 CanvasGroup의 표시 상태를 기준으로 숨김 요청을 보냅니다. 예: PlayerPanelHub CanvasGroup")]
    [SerializeField] private CanvasGroup _sourceGroup;

    [Tooltip("켜져 있으면 Source Group이 보일 때 Target을 숨깁니다.")]
    [SerializeField] private bool _suppressWhenSourceVisible = true;

    [Tooltip("Source Group alpha가 이 값보다 크면 visible로 판단합니다.")]
    [SerializeField, Range(0f, 1f)] private float _visibleAlphaThreshold = 0.01f;

    [Tooltip("켜져 있으면 Source Group의 Blocks Raycasts도 true여야 visible로 판단합니다.")]
    [SerializeField] private bool _requireSourceBlocksRaycasts = true;

    [Header("Broadcasting")]
    [Tooltip("숨김 요청을 보낼 이벤트 채널입니다.")]
    [SerializeField] private UIVisibilitySuppressRequestEventChannelSO _requestChannel;

    [Tooltip("숨길 대상 ID입니다. Target 쪽 Target Id와 같아야 합니다. 예: AimCursor")]
    [SerializeField] private string _targetId = "AimCursor";

    [Tooltip("이 Source를 구분하는 ID입니다. 비워두면 씬/오브젝트/인스턴스 ID로 자동 생성합니다.")]
    [SerializeField] private string _sourceId;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private string _runtimeSourceId;
    private bool _hasLastState;
    private bool _lastSuppressState;

    private void Reset()
    {
        _sourceGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (_sourceGroup == null)
            _sourceGroup = GetComponent<CanvasGroup>();

        _runtimeSourceId = string.IsNullOrWhiteSpace(_sourceId)
            ? $"{gameObject.scene.name}:{gameObject.name}:{GetInstanceID()}"
            : _sourceId.Trim();
    }

    private void OnEnable()
    {
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh(force: false);
    }

    private void OnDisable()
    {
        RaiseSuppress(false);
        _hasLastState = false;
    }

    public void RefreshNow()
    {
        Refresh(force: true);
    }

    private void Refresh(bool force)
    {
        bool sourceVisible = IsSourceVisible();
        bool shouldSuppress = _suppressWhenSourceVisible
            ? sourceVisible
            : !sourceVisible;

        if (!force && _hasLastState && _lastSuppressState == shouldSuppress)
            return;

        _hasLastState = true;
        _lastSuppressState = shouldSuppress;

        RaiseSuppress(shouldSuppress);
    }

    private bool IsSourceVisible()
    {
        if (_sourceGroup == null)
            return false;

        bool visible = _sourceGroup.alpha > _visibleAlphaThreshold;

        if (_requireSourceBlocksRaycasts)
            visible &= _sourceGroup.blocksRaycasts;

        return visible;
    }

    private void RaiseSuppress(bool suppress)
    {
        if (_requestChannel == null)
            return;

        _requestChannel.RaiseEvent(new UIVisibilitySuppressRequest(
            _targetId,
            _runtimeSourceId,
            suppress));

        if (_debugLogs)
        {
            Debug.Log(
                $"[CanvasGroupVisibilitySuppressSource] target={_targetId}, source={_runtimeSourceId}, suppress={suppress}",
                this);
        }
    }
}