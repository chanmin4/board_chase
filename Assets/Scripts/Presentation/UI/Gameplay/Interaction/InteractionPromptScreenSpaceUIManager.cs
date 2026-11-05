/*
 * Role hierarchy: Presentation > UI > Gameplay > Interaction
 * Primary responsibility: Handles InteractionPromptScreenSpaceUIManager interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionPromptScreenSpaceUIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _widgetRoot;
    [SerializeField] private InteractionPromptWidget _promptPrefab;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;
    [SerializeField] private InteractionPromptEventChannelSO _promptChannel;

    [Header("Follow")]
    [SerializeField] private Vector2 _screenPixelOffset = new Vector2(0f, 36f);
    [SerializeField] private bool _hideWhenBehindCamera = true;
    [SerializeField] private bool _hideWhenOffScreen = true;
    [SerializeField] private bool _clampToScreen = true;
    [SerializeField] private float _screenEdgePadding = 32f;

    [Header("Nearby Interaction Dots")]
    [Tooltip("실제 E 상호작용 거리와 별개로, 플레이어의 주변 원형 시야 안에 있는 상호작용 대상에 점을 표시합니다. 벽 뒤 대상은 표시하지 않습니다.")]
    [SerializeField] private bool _showNearbyDots = true;
    [SerializeField, Min(.05f)] private float _nearbyDotScanInterval = .15f;
    [SerializeField, Min(2f)] private float _nearbyDotSize = 10f;
    [SerializeField] private Vector2 _nearbyDotPixelOffset = new Vector2(0f, 8f);
    [SerializeField] private Color _nearbyDotColor = new Color(1f, 1f, 1f, .9f);

    private Camera _worldCamera;
    private Canvas _dotCanvas;
    private InteractionPromptWidget _widget;
    private InteractionPromptSnapshot _current;
    private bool _hasPrompt;
    private EntityVisionController _dotViewer;
    private InteractionManager _interactionManager;
    private Collider[] _nearbyColliders = new Collider[64];
    private readonly HashSet<Transform> _nearbyAnchors = new();
    private readonly Dictionary<Transform, InteractionNearbyDotGraphic> _nearbyDots = new();
    private readonly Queue<InteractionNearbyDotGraphic> _dotPool = new();
    private readonly List<Transform> _obsoleteAnchors = new();
    private float _nextDotScan;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureWidget();

        if (_widget != null)
            _widget.SetVisible(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += OnWorldCameraChanged;

            if (_worldCameraReadyChannel.Current != null)
                OnWorldCameraChanged(_worldCameraReadyChannel.Current);
        }

        if (_promptChannel != null)
            _promptChannel.OnEventRaised += HandlePromptChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        ClearNearbyDots();
        _hasPrompt = false;
        if (_widget != null) _widget.SetVisible(false);
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraChanged;

        if (_promptChannel != null)
            _promptChannel.OnEventRaised -= HandlePromptChanged;
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        UpdateNearbyDots();
        if (!_hasPrompt || _current.Anchor == null || _worldCamera == null || _widgetRoot == null || _widget == null)
            return;

        Vector3 screenPos = _worldCamera.WorldToScreenPoint(_current.Anchor.position);

        if (_hideWhenBehindCamera && screenPos.z <= 0f)
        {
            _widget.SetVisible(false);
            return;
        }

        bool isOffScreen =
            screenPos.x < 0f || screenPos.x > Screen.width ||
            screenPos.y < 0f || screenPos.y > Screen.height;

        if (_hideWhenOffScreen && isOffScreen)
        {
            _widget.SetVisible(false);
            return;
        }

        Vector2 finalScreen = new Vector2(screenPos.x, screenPos.y);

        if (_clampToScreen)
        {
            finalScreen.x = Mathf.Clamp(finalScreen.x, _screenEdgePadding, Screen.width - _screenEdgePadding);
            finalScreen.y = Mathf.Clamp(finalScreen.y, _screenEdgePadding, Screen.height - _screenEdgePadding);
        }

        finalScreen += _screenPixelOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _widgetRoot,
            finalScreen,
            null,
            out Vector2 localPoint);

        _widget.SetScreenPosition(localPoint);
        _widget.SetVisible(true);
    }

    private void UpdateNearbyDots()
    {
        if (!_showNearbyDots || _worldCamera == null || _widgetRoot == null)
        {
            ClearNearbyDots();
            return;
        }

        if (Time.unscaledTime >= _nextDotScan)
        {
            _nextDotScan = Time.unscaledTime + Mathf.Max(.05f, _nearbyDotScanInterval);
            ScanNearbyDots();
        }

        // Only project the small nearby set each frame; no scene scan or physics
        // queries here. Dots remain attached while camera/player moves.
        foreach (var entry in _nearbyDots)
        {
            InteractionNearbyDotGraphic dot = entry.Value;
            if (entry.Key == null) { dot.gameObject.SetActive(false); continue; }
            Vector3 screen = _worldCamera.WorldToScreenPoint(entry.Key.position);
            bool visible = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width &&
                           screen.y >= 0f && screen.y <= Screen.height;
            dot.gameObject.SetActive(visible);
            if (!visible) continue;
            if (_dotCanvas == null) _dotCanvas = _widgetRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = _dotCanvas != null && _dotCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _dotCanvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_widgetRoot,
                new Vector2(screen.x, screen.y) + _nearbyDotPixelOffset, uiCamera, out Vector2 local);
            dot.rectTransform.anchoredPosition = local;
        }
    }

    private void ScanNearbyDots()
    {
        EntityVisionController viewer = VisionVisibilitySystem.Instance != null
            ? VisionVisibilitySystem.Instance.Viewer : null;
        if (_dotViewer != viewer || _interactionManager == null)
        {
            _dotViewer = viewer;
            _interactionManager = viewer != null
                ? EntityComponentResolver.Resolve<InteractionManager>(viewer) : null;
        }
        _nearbyAnchors.Clear();
        if (viewer == null || _interactionManager == null || !_interactionManager.isActiveAndEnabled)
        {
            ClearNearbyDots();
            return;
        }
        float radius = viewer.CloseVisionRadius;
        Vector3 origin = viewer.VisualOrigin.position;
        int count = 0;
        if (radius > 0f)
        {
            do
            {
                count = Physics.OverlapSphereNonAlloc(origin, radius, _nearbyColliders,
                    Physics.AllLayers, QueryTriggerInteraction.Collide);
                if (count < _nearbyColliders.Length) break;
                Array.Resize(ref _nearbyColliders, _nearbyColliders.Length * 2);
            } while (true);
        }
        for (int i = 0; i < count; i++)
        {
            Collider collider = _nearbyColliders[i];
            if (collider == null || !_interactionManager.TryResolveNearbyPrompt(collider.gameObject, out Transform anchor) ||
                _nearbyAnchors.Contains(anchor)) continue;
            Vector3 delta = anchor.position - origin;
            delta.y = 0f;
            // Close circle only, not the long forward cone. LOS prevents dots
            // from advertising unopened rooms or undiscovered objects behind walls.
            if (delta.sqrMagnitude > radius * radius || !viewer.CanRevealVisualPoint(anchor.position, 0f)) continue;
            _nearbyAnchors.Add(anchor);
            if (_nearbyDots.ContainsKey(anchor)) continue;
            InteractionNearbyDotGraphic dot = CreateOrRentDot();
            _nearbyDots.Add(anchor, dot);
        }

        _obsoleteAnchors.Clear();
        foreach (var entry in _nearbyDots)
            if (entry.Key == null || !_nearbyAnchors.Contains(entry.Key)) _obsoleteAnchors.Add(entry.Key);
        foreach (Transform anchor in _obsoleteAnchors)
        {
            ReturnDot(_nearbyDots[anchor]);
            _nearbyDots.Remove(anchor);
        }
    }

    private InteractionNearbyDotGraphic CreateOrRentDot()
    {
        InteractionNearbyDotGraphic dot;
        if (_dotPool.Count > 0) dot = _dotPool.Dequeue();
        else
        {
            var obj = new GameObject("Nearby Interaction Dot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(InteractionNearbyDotGraphic));
            obj.transform.SetParent(_widgetRoot, false);
            dot = obj.GetComponent<InteractionNearbyDotGraphic>();
            dot.raycastTarget = false;
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(.5f, .5f);
            dot.rectTransform.pivot = new Vector2(.5f, .5f);
        }
        dot.rectTransform.sizeDelta = Vector2.one * _nearbyDotSize;
        dot.color = _nearbyDotColor;
        dot.gameObject.SetActive(true);
        return dot;
    }

    private void ReturnDot(InteractionNearbyDotGraphic dot)
    {
        if (dot == null) return;
        dot.gameObject.SetActive(false);
        _dotPool.Enqueue(dot);
    }

    private void ClearNearbyDots()
    {
        foreach (var entry in _nearbyDots) ReturnDot(entry.Value);
        _nearbyDots.Clear();
        _nearbyAnchors.Clear();
    }

    // Purpose: Handles the incoming prompt changed request or event.
    private void HandlePromptChanged(InteractionPromptSnapshot snapshot)
    {
        EnsureWidget();

        _current = snapshot;
        _hasPrompt = snapshot.Visible && snapshot.Anchor != null;

        if (_widget == null)
            return;

        if (!_hasPrompt)
        {
            _widget.SetVisible(false);
            return;
        }

        _widget.Bind(snapshot);
        _widget.SetVisible(true);
    }

    // Purpose: Responds to the world camera changed callback.
    private void OnWorldCameraChanged(Camera camera)
    {
        _worldCamera = camera;
    }

    // Purpose: Ensures widget is ready before the workflow continues.
    private void EnsureWidget()
    {
        if (_widget != null || _promptPrefab == null || _widgetRoot == null)
            return;

        _widget = Instantiate(_promptPrefab, _widgetRoot);
    }
}
