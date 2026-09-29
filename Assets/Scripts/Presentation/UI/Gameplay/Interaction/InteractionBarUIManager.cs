/*
 * Role hierarchy: Presentation > UI > Gameplay > Interaction
 * Primary responsibility: Handles InteractionBarUIManager interaction, pickup, trade, loot, or storage flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class InteractionBarUIManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _widgetRoot;
    [SerializeField] private InteractionCastBarWidget _castBarPrefab;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;
    [SerializeField] private InteractionCastBarEventChannelSO _castBarChannel;

    [Header("Follow")]
    [SerializeField] private Vector2 _screenPixelOffset = new Vector2(0f, 56f);
    [SerializeField] private bool _hideWhenBehindCamera = true;
    [SerializeField] private bool _hideWhenOffScreen = true;
    [SerializeField] private bool _clampToScreen = true;
    [SerializeField] private float _screenEdgePadding = 32f;

    private Camera _worldCamera;
    private InteractionCastBarWidget _widget;
    private InteractionCastBarSnapshot _current;
    private bool _hasCast;

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

        if (_castBarChannel != null)
            _castBarChannel.OnEventRaised += HandleCastChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraChanged;

        if (_castBarChannel != null)
            _castBarChannel.OnEventRaised -= HandleCastChanged;
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (!_hasCast || _worldCamera == null || _widgetRoot == null || _widget == null)
            return;

        Transform followTarget = _current.Anchor != null
            ? _current.Anchor
            : _current.Actor;

        if (followTarget == null)
        {
            _widget.SetVisible(false);
            return;
        }

        Vector3 screenPos = _worldCamera.WorldToScreenPoint(followTarget.position);

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
        _widget.SetProgress(_current.Progress01);
        _widget.SetVisible(true);
    }

    // Purpose: Handles the incoming cast changed request or event.
    private void HandleCastChanged(InteractionCastBarSnapshot snapshot)
    {
        EnsureWidget();

        _current = snapshot;
        _hasCast = snapshot.Visible;

        if (_widget == null)
            return;

        if (!_hasCast)
        {
            _widget.SetProgress(0f);
            _widget.SetVisible(false);
            return;
        }

        _widget.SetProgress(snapshot.Progress01);
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
        if (_widget != null || _castBarPrefab == null || _widgetRoot == null)
            return;

        _widget = Instantiate(_castBarPrefab, _widgetRoot);
    }
}
