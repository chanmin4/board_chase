/*
 * Role hierarchy: Presentation > UI > Gameplay > Tool
 * Primary responsibility: Implements the focused CanvasGroupVisibilitySuppressTarget responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class CanvasGroupVisibilitySuppressTarget : MonoBehaviour
{
    private struct CanvasGroupState
    {
        public float alpha;
        public bool interactable;
        public bool blocksRaycasts;
    }

    [Header("Target")]
    [Tooltip("숨김/복구할 CanvasGroup입니다. 비워두면 이 오브젝트에서 찾거나 자동 추가합니다.")]
    [SerializeField] private CanvasGroup _targetGroup;

    [Tooltip("이 ID와 같은 숨김 요청만 받습니다. Source 쪽 Target Id와 같아야 합니다. 예: AimCursor")]
    [SerializeField] private string _targetId = "AimCursor";

    [Header("Listening")]
    [Tooltip("숨김 요청을 받을 이벤트 채널입니다.")]
    [SerializeField] private UIVisibilitySuppressRequestEventChannelSO _requestChannel;

    [Header("Restore")]
    [Tooltip("숨김이 해제되면 Awake/OnEnable 시점의 CanvasGroup 상태로 되돌립니다.")]
    [SerializeField] private bool _restoreInitialStateWhenUnsuppressed = true;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs;

    private CanvasGroupState _initialState;
    private bool _hasInitialState;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _targetGroup = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureTargetGroup();
        CaptureInitialState();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        EnsureTargetGroup();
        CaptureInitialState();

        if (_requestChannel != null)
            _requestChannel.OnEventRaised += HandleSuppressRequest;

        RefreshFromChannel();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_requestChannel != null)
            _requestChannel.OnEventRaised -= HandleSuppressRequest;

        if (_restoreInitialStateWhenUnsuppressed)
            RestoreInitialState();
    }

    // Purpose: Handles the incoming suppress request request or event.
    private void HandleSuppressRequest(UIVisibilitySuppressRequest request)
    {
        if (request.targetId != _targetId)
            return;

        RefreshFromChannel();
    }

    // Purpose: Refreshes the displayed or cached from channel.
    private void RefreshFromChannel()
    {
        bool suppressed = _requestChannel != null &&
                          _requestChannel.IsSuppressed(_targetId);

        ApplySuppressed(suppressed);
    }

    // Purpose: Applies the requested suppressed change.
    private void ApplySuppressed(bool suppressed)
    {
        EnsureTargetGroup();

        if (_targetGroup == null)
            return;

        if (suppressed)
        {
            _targetGroup.alpha = 0f;
            _targetGroup.interactable = false;
            _targetGroup.blocksRaycasts = false;

            if (_debugLogs)
                Debug.Log($"[CanvasGroupVisibilitySuppressTarget] target={_targetId}, suppressed=true", this);

            return;
        }

        if (_restoreInitialStateWhenUnsuppressed)
        {
            RestoreInitialState();
        }
        else
        {
            _targetGroup.alpha = 1f;
            _targetGroup.interactable = true;
            _targetGroup.blocksRaycasts = true;
        }

        if (_debugLogs)
            Debug.Log($"[CanvasGroupVisibilitySuppressTarget] target={_targetId}, suppressed=false", this);
    }

    // Purpose: Ensures target group is ready before the workflow continues.
    private void EnsureTargetGroup()
    {
        if (_targetGroup != null)
            return;

        _targetGroup = GetComponent<CanvasGroup>();

        if (_targetGroup == null)
            _targetGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // Purpose: Performs the capture initial state workflow for this component.
    private void CaptureInitialState()
    {
        if (_hasInitialState || _targetGroup == null)
            return;

        _initialState = new CanvasGroupState
        {
            alpha = _targetGroup.alpha,
            interactable = _targetGroup.interactable,
            blocksRaycasts = _targetGroup.blocksRaycasts
        };

        _hasInitialState = true;
    }

    // Purpose: Restores the saved initial state state.
    private void RestoreInitialState()
    {
        if (!_hasInitialState || _targetGroup == null)
            return;

        _targetGroup.alpha = _initialState.alpha;
        _targetGroup.interactable = _initialState.interactable;
        _targetGroup.blocksRaycasts = _initialState.blocksRaycasts;
    }
}
