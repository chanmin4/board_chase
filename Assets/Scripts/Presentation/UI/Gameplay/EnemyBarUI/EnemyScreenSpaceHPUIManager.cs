/*
 * Role hierarchy: Presentation > UI > Gameplay > EnemyBarUI
 * Primary responsibility: Coordinates the EnemyScreenSpaceHPUIManager subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyScreenSpaceHPUIManager : MonoBehaviour
{
    public static EnemyScreenSpaceHPUIManager Instance { get; private set; }

    [Header("Refs")]
    [Tooltip("Enemy health bars will be spawned as children of this transform.")]
    [SerializeField] private RectTransform _widgetRoot;
    [SerializeField] private EnemyHealthBarWidget _healthBarPrefab;

    [Tooltip("Viewport margin for off-screen hiding. 0 means exact screen edge. 0.03 gives a small buffer.")]
    [SerializeField, Min(0f)] private float _viewportHideMargin = 0.03f;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private Camera _worldCamera;

    private sealed class EnemyUIEntry
    {
        public EnemyHealthBarWidget HealthBar;
    }

    private readonly Dictionary<EnemyScreenSpaceHPUIAnchor, EnemyUIEntry> _widgets = new();

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        Instance = this;
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
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraChanged;
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Purpose: Processes frame work that must run after the regular Update pass.
    private void LateUpdate()
    {
        if (_worldCamera == null || _widgetRoot == null)
            return;

        foreach (var pair in _widgets)
        {
            EnemyScreenSpaceHPUIAnchor anchor = pair.Key;
            EnemyUIEntry entry = pair.Value;

            if (anchor == null || entry == null)
                continue;

            UpdateWidget(anchor, entry);
        }
    }

    // Purpose: Responds to the world camera changed callback.
    private void OnWorldCameraChanged(Camera camera)
    {
        _worldCamera = camera;
    }

    // Purpose: Registers register with its owning runtime service.
    public void Register(EnemyScreenSpaceHPUIAnchor anchor)
    {
        if (anchor == null || _widgetRoot == null)
            return;

        if (_widgets.ContainsKey(anchor))
            return;

        EnemyUIEntry entry = new EnemyUIEntry();

        if (_healthBarPrefab != null)
        {
            entry.HealthBar = Instantiate(_healthBarPrefab, _widgetRoot);
            entry.HealthBar.Bind(anchor);
        }

        _widgets.Add(anchor, entry);
    }

    // Purpose: Unregisters unregister from its owning runtime service.
    public void Unregister(EnemyScreenSpaceHPUIAnchor anchor)
    {
        if (anchor == null)
            return;

        if (!_widgets.TryGetValue(anchor, out EnemyUIEntry entry))
            return;

        if (entry.HealthBar != null)
            Destroy(entry.HealthBar.gameObject);

        _widgets.Remove(anchor);
    }

    // Purpose: Updates the widget from the current runtime state.
    private void UpdateWidget(EnemyScreenSpaceHPUIAnchor anchor, EnemyUIEntry entry)
    {
        if (anchor.Enemy != null && !anchor.Enemy.IsSpawnReady)
        {
            SetEntryVisible(entry, false);
            return;
        }

        if (!anchor.IsVisionVisible)
        {
            SetEntryVisible(entry, false);
            return;
        }

        bool healthVisible = TryGetLocalPoint(
            anchor,
            anchor.GetHealthBarLayout(),
            out Vector2 healthLocalPoint);

        if (entry.HealthBar != null)
        {
            entry.HealthBar.SetManagerVisible(healthVisible);

            if (healthVisible)
                entry.HealthBar.SetScreenPosition(healthLocalPoint);

            entry.HealthBar.TickVisualState();
        }
    }

    // Purpose: Updates the entry visible state.
    private static void SetEntryVisible(EnemyUIEntry entry, bool visible)
    {
        if (entry == null)
            return;

        if (entry.HealthBar != null)
            entry.HealthBar.SetManagerVisible(visible);

    }

    // Purpose: Attempts to get local point and reports whether the request succeeded.
    private bool TryGetLocalPoint(
        EnemyScreenSpaceHPUIAnchor anchor,
        EnemyScreenSpaceWidgetLayout layout,
        out Vector2 localPoint)
    {
        localPoint = default;

        if (anchor == null || layout == null)
            return false;

        Vector3 worldPos = anchor.GetWorldUIPosition(layout);
        Vector3 viewportPos = _worldCamera.WorldToViewportPoint(worldPos);

        if (layout.HideWhenBehindCamera && viewportPos.z <= 0f)
            return false;

        bool isOffScreen =
            viewportPos.x < -_viewportHideMargin ||
            viewportPos.x > 1f + _viewportHideMargin ||
            viewportPos.y < -_viewportHideMargin ||
            viewportPos.y > 1f + _viewportHideMargin;

        if (layout.HideWhenOffScreen && isOffScreen)
            return false;

        Vector3 screenPos = _worldCamera.WorldToScreenPoint(worldPos);
        Vector2 finalScreen = new Vector2(screenPos.x, screenPos.y);

        if (layout.ClampToScreen)
        {
            float padding = layout.ScreenEdgePadding;
            finalScreen.x = Mathf.Clamp(finalScreen.x, padding, Screen.width - padding);
            finalScreen.y = Mathf.Clamp(finalScreen.y, padding, Screen.height - padding);
        }

        finalScreen += layout.ScreenPixelOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _widgetRoot,
            finalScreen,
            null,
            out localPoint);

        return true;
    }
}
