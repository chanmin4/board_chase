/*
 * Role hierarchy: Presentation > UI > Gameplay > DamageNumber
 * Primary responsibility: Handles DamageNumberUIManager combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageNumberUIManager : MonoBehaviour
{
    private sealed class ActivePopup
    {
        public DamageNumberPopupUI popup;
        public GameObject target;
        public Vector3 worldPosition;
        public DamageNumberKind kind;
        public Vector2 jitter;
        public float createdTime;
        public bool paired;
    }

    [Header("Events")]
    [SerializeField] private DamageNumberEventChannelSO _damageNumberEventChannel;
    [Tooltip("Published by the active RUN world camera. This keeps damage-number placement valid across additive Home/RUN scene transitions.")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Header("UI")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _popupRoot;
    [SerializeField] private DamageNumberPopupUI _popupPrefab;

    [Header("Popup Placement")]
    [Tooltip("Random UI offset applied once when each popup is created so repeated hits do not fully overlap.")]
    [SerializeField] private Vector2 _spawnJitter = new(18f, 10f);

    [Tooltip("Older popups on the same target are shifted by this amount. Newest popup stays closest to the anchor.")]
    [SerializeField] private Vector2 _stackSpacing = new(0f, 18f);

    [Tooltip("Horizontal spacing used when health and infection damage from the same hit are shown together. Health goes left, infection goes right.")]
    [SerializeField] private float _pairedDamageSpacing = 42f;

    [Tooltip("Maximum visible popups per target. Older popups beyond this are hidden early.")]
    [SerializeField, Min(1)] private int _maxVisiblePopupsPerTarget = 4;

    [Tooltip("Alpha multiplier applied per older stack slot. 1 keeps every popup equally bright; lower values emphasize the latest hit.")]
    [SerializeField, Range(0.1f, 1f)] private float _olderPopupAlphaMultiplier = 0.82f;

    [Header("Pairing")]
    [Tooltip("Very short window used only to place health and infection from the same hit side by side. Same-kind damage is not merged.")]
    [SerializeField, Min(0f)] private float _healthInfectionPairWindowSeconds = 0.04f;

    [Tooltip("World distance fallback for pairing position-based requests with no target.")]
    [SerializeField, Min(0f)] private float _pairWorldDistance = 0.5f;

    [Header("Animation")]
    [Tooltip("Total popup lifetime in unscaled seconds.")]
    [SerializeField, Min(0.01f)] private float _popupLifetime = 0.9f;

    [Tooltip("Normalized time where fade starts. 0.75 means the popup stays fully visible for 75% of its lifetime.")]
    [SerializeField, Range(0f, 0.99f)] private float _fadeStartNormalized = 0.75f;

    [Tooltip("UI-space movement applied during lifetime. Usually slightly upward.")]
    [SerializeField] private Vector2 _floatOffset = new(0f, 28f);

    [Tooltip("Initial scale used for the quick pop effect.")]
    [SerializeField, Min(1f)] private float _popScale = 1.22f;

    [Tooltip("Seconds spent returning from Pop Scale to normal scale.")]
    [SerializeField, Min(0.001f)] private float _popDuration = 0.08f;

    [Header("Number Format")]
    [SerializeField] private string _numberFormat = "{0:0.0}";

    [Header("Icons")]
    [Tooltip("Shared icon for all damage kinds, including health and infection.")]
    [SerializeField] private Sprite _damageIcon;

    [Tooltip("Icon used only for healing numbers.")]
    [SerializeField] private Sprite _healIcon;

    [Header("Colors")]
    [SerializeField] private Color _healthDamageTextColor = new(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color _infectionDamageTextColor = new(0.78f, 0.25f, 1f, 1f);
    [SerializeField] private Color _healTextColor = new(0.35f, 1f, 0.45f, 1f);
    [SerializeField] private Color _iconColor = Color.white;

    private readonly List<DamageNumberPopupUI> _pool = new();
    private readonly List<ActivePopup> _activePopups = new();
    private Camera _worldCamera;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _canvas = GetComponentInParent<Canvas>();
        _popupRoot = transform as RectTransform;
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureRefs();

        if (_popupPrefab != null)
            _popupPrefab.gameObject.SetActive(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_damageNumberEventChannel != null)
            _damageNumberEventChannel.OnEventRaised += HandleDamageNumberRequested;

        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += HandleWorldCameraChanged;
            HandleWorldCameraChanged(_worldCameraReadyChannel.Current);
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_damageNumberEventChannel != null)
            _damageNumberEventChannel.OnEventRaised -= HandleDamageNumberRequested;

        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= HandleWorldCameraChanged;

        _worldCamera = null;
    }

    // Purpose: Handles the incoming world camera changed request or event.
    private void HandleWorldCameraChanged(Camera worldCamera)
    {
        _worldCamera = worldCamera;
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        CleanupInactivePopups();
        UpdateActivePopupPositions();
    }

    // Purpose: Handles the incoming damage number requested request or event.
    private void HandleDamageNumberRequested(DamageNumberRequest request)
    {
        if (!request.IsValid || _popupPrefab == null)
            return;

        Vector3 worldPosition = ResolveWorldPosition(request);
        if (!TryWorldToCanvasPosition(worldPosition, out Vector2 anchoredPosition))
            return;

        DamageNumberPopupUI popup = GetPopup();
        if (popup == null)
            return;

        popup.Play(
            _floatOffset,
            _popupLifetime,
            _fadeStartNormalized,
            _popScale,
            _popDuration);

        ActivePopup active = new ActivePopup
        {
            popup = popup,
            target = request.Target,
            worldPosition = worldPosition,
            kind = request.Kind,
            jitter = new Vector2(
                Random.Range(-_spawnJitter.x, _spawnJitter.x),
                Random.Range(-_spawnJitter.y, _spawnJitter.y)),
            createdTime = Time.unscaledTime
        };

        ActivePopup pair = FindDamagePair(active);
        if (pair != null)
        {
            active.paired = true;
            pair.paired = true;
            pair.popup.SetPairOffset(ResolvePairOffset(pair.kind));
            active.popup.SetPairOffset(ResolvePairOffset(active.kind));
        }

        active.popup.SetBasePosition(anchoredPosition + active.jitter);

        active.popup.SetAmount(
            request.Amount,
            _numberFormat,
            ResolveIcon(request.Kind),
            ResolveTextColor(request.Kind),
            _iconColor);

        _activePopups.Add(active);
    }

    // Purpose: Finds the requested damage pair.
    private ActivePopup FindDamagePair(ActivePopup current)
    {
        if (!IsHealthOrInfection(current.kind))
            return null;

        float now = Time.unscaledTime;

        for (int i = _activePopups.Count - 1; i >= 0; i--)
        {
            ActivePopup active = _activePopups[i];

            if (active == null || active.popup == null || !active.popup.IsVisible)
            {
                _activePopups.RemoveAt(i);
                continue;
            }

            if (!IsHealthOrInfection(active.kind) || active.kind == current.kind)
                continue;

            if (now - active.createdTime > _healthInfectionPairWindowSeconds)
                continue;

            if (current.target != null && active.target == current.target)
                return active;

            float sqrDistance = (active.worldPosition - current.worldPosition).sqrMagnitude;
            if (sqrDistance <= _pairWorldDistance * _pairWorldDistance)
                return active;
        }

        return null;
    }

    // Purpose: Updates the active popup positions from the current runtime state.
    private void UpdateActivePopupPositions()
    {
        for (int i = 0; i < _activePopups.Count; i++)
        {
            ActivePopup active = _activePopups[i];
            if (active == null || active.popup == null || !active.popup.IsVisible)
                continue;

            Vector3 worldPosition = ResolveWorldPosition(active);
            if (!TryWorldToCanvasPosition(worldPosition, out Vector2 anchoredPosition))
                continue;

            int stackIndex = CountNewerPopupsForSameTarget(active);
            if (stackIndex >= _maxVisiblePopupsPerTarget)
            {
                active.popup.gameObject.SetActive(false);
                continue;
            }

            float alphaMultiplier = Mathf.Pow(_olderPopupAlphaMultiplier, stackIndex);
            active.popup.SetBasePosition(anchoredPosition + active.jitter);
            active.popup.SetStackOffset(_stackSpacing * stackIndex, alphaMultiplier);
        }
    }

    // Purpose: Counts the available newer popups for same target.
    private int CountNewerPopupsForSameTarget(ActivePopup popup)
    {
        int count = 0;

        for (int i = 0; i < _activePopups.Count; i++)
        {
            ActivePopup other = _activePopups[i];
            if (other == null || other == popup || other.popup == null || !other.popup.IsVisible)
                continue;

            if (other.createdTime <= popup.createdTime)
                continue;

            if (popup.target != null && other.target == popup.target)
            {
                count++;
                continue;
            }

            if (popup.target == null && other.target == null)
            {
                float sqrDistance = (other.worldPosition - popup.worldPosition).sqrMagnitude;
                if (sqrDistance <= _pairWorldDistance * _pairWorldDistance)
                    count++;
            }
        }

        return count;
    }

    // Purpose: Performs the cleanup inactive popups workflow for this component.
    private void CleanupInactivePopups()
    {
        for (int i = _activePopups.Count - 1; i >= 0; i--)
        {
            ActivePopup active = _activePopups[i];

            if (active == null || active.popup == null || !active.popup.IsVisible)
                _activePopups.RemoveAt(i);
        }
    }

    // Purpose: Resolves the world position needed by this component.
    private Vector3 ResolveWorldPosition(DamageNumberRequest request)
    {
        if (request.Target != null)
        {
            DamageNumberAnchor anchor =
                request.Target.GetComponentInChildren<DamageNumberAnchor>(true);

            if (anchor != null)
                return anchor.ResolveWorldPosition();

            Collider collider =
                request.Target.GetComponent<Collider>() ??
                request.Target.GetComponentInChildren<Collider>();

            if (collider != null)
                return collider.bounds.center;

            Renderer renderer = request.Target.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return renderer.bounds.center;

            return request.Target.transform.position;
        }

        return request.WorldPosition;
    }

    // Purpose: Resolves the world position needed by this component.
    private Vector3 ResolveWorldPosition(ActivePopup active)
    {
        if (active.target != null)
            return ResolveTargetWorldPosition(active.target);

        return active.worldPosition;
    }

    // Purpose: Resolves the target world position needed by this component.
    private Vector3 ResolveTargetWorldPosition(GameObject target)
    {
        DamageNumberAnchor anchor = target.GetComponentInChildren<DamageNumberAnchor>(true);
        if (anchor != null)
            return anchor.ResolveWorldPosition();

        Collider collider =
            target.GetComponent<Collider>() ??
            target.GetComponentInChildren<Collider>();

        if (collider != null)
            return collider.bounds.center;

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds.center;

        return target.transform.position;
    }

    // Purpose: Attempts to world to canvas position and reports whether the request succeeded.
    private bool TryWorldToCanvasPosition(Vector3 worldPosition, out Vector2 anchoredPosition)
    {
        EnsureRefs();
        anchoredPosition = default;

        if (_canvas == null || _popupRoot == null)
            return false;

        Camera worldCamera = ResolveWorldCamera();
        if (worldCamera == null)
            return false;

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z < 0f)
            return false;

        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _popupRoot,
            screenPosition,
            uiCamera,
            out anchoredPosition);
    }

    // Purpose: Checks whether health or infection is currently true.
    private static bool IsHealthOrInfection(DamageNumberKind kind)
    {
        return kind == DamageNumberKind.Health ||
            kind == DamageNumberKind.Infection;
    }

    // Purpose: Resolves the pair offset needed by this component.
    private Vector2 ResolvePairOffset(DamageNumberKind kind)
    {
        float halfSpacing = Mathf.Max(0f, _pairedDamageSpacing) * 0.5f;

        return kind == DamageNumberKind.Health
            ? new Vector2(-halfSpacing, 0f)
            : new Vector2(halfSpacing, 0f);
    }

    // Purpose: Resolves the icon needed by this component.
    private Sprite ResolveIcon(DamageNumberKind kind)
    {
        return kind == DamageNumberKind.Heal
            ? _healIcon
            : _damageIcon;
    }

    // Purpose: Resolves the text color needed by this component.
    private Color ResolveTextColor(DamageNumberKind kind)
    {
        return kind switch
        {
            DamageNumberKind.Infection => _infectionDamageTextColor,
            DamageNumberKind.Heal => _healTextColor,
            _ => _healthDamageTextColor
        };
    }

    // Purpose: Returns the current popup.
    private DamageNumberPopupUI GetPopup()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i] != null && !_pool[i].gameObject.activeSelf)
                return _pool[i];
        }

        if (_popupPrefab == null || _popupRoot == null)
            return null;

        DamageNumberPopupUI popup = Instantiate(_popupPrefab, _popupRoot);
        _pool.Add(popup);
        return popup;
    }

    // Purpose: Ensures refs is ready before the workflow continues.
    private void EnsureRefs()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (_popupRoot == null)
            _popupRoot = transform as RectTransform;
    }

    /// <summary>
    /// Uses the explicit active-world-camera contract first. Camera.main remains
    /// a backwards-compatible fallback for older scenes that have not yet been
    /// wired to the channel.
    /// </summary>
    private Camera ResolveWorldCamera()
    {
        if (_worldCamera != null && _worldCamera.isActiveAndEnabled)
            return _worldCamera;

        if (_worldCameraReadyChannel != null &&
            _worldCameraReadyChannel.Current != null &&
            _worldCameraReadyChannel.Current.isActiveAndEnabled)
        {
            _worldCamera = _worldCameraReadyChannel.Current;
            return _worldCamera;
        }

        _worldCamera = Camera.main;
        return _worldCamera;
    }
}
