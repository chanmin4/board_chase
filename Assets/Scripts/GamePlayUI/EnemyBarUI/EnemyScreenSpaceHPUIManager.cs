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
    [SerializeField] private EnemyInfectionCastBarWidget _castBarPrefab;


    [Tooltip("Viewport margin for off-screen hiding. 0 means exact screen edge. 0.03 gives a small buffer.")]
    [SerializeField, Min(0f)] private float _viewportHideMargin = 0.03f;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private Camera _worldCamera;

    private sealed class EnemyUIEntry
    {
        public EnemyHealthBarWidget HealthBar;
        public EnemyInfectionCastBarWidget CastBar;
    }

    private readonly Dictionary<EnemyScreenSpaceHPUIAnchor, EnemyUIEntry> _widgets = new();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (_worldCameraReadyChannel != null)
        {
            _worldCameraReadyChannel.OnEventRaised += OnWorldCameraChanged;

            if (_worldCameraReadyChannel.Current != null)
                OnWorldCameraChanged(_worldCameraReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_worldCameraReadyChannel != null)
            _worldCameraReadyChannel.OnEventRaised -= OnWorldCameraChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

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

    private void OnWorldCameraChanged(Camera camera)
    {
        _worldCamera = camera;
    }

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

        if (_castBarPrefab != null)
        {
            entry.CastBar = Instantiate(_castBarPrefab, _widgetRoot);
            entry.CastBar.Bind(anchor);
        }

        _widgets.Add(anchor, entry);
    }

    public void Unregister(EnemyScreenSpaceHPUIAnchor anchor)
    {
        if (anchor == null)
            return;

        if (!_widgets.TryGetValue(anchor, out EnemyUIEntry entry))
            return;

        if (entry.HealthBar != null)
            Destroy(entry.HealthBar.gameObject);

        if (entry.CastBar != null)
            Destroy(entry.CastBar.gameObject);

        _widgets.Remove(anchor);
    }

    private void UpdateWidget(EnemyScreenSpaceHPUIAnchor anchor, EnemyUIEntry entry)
    {
        if (anchor.Enemy != null && !anchor.Enemy.IsSpawnReady)
        {
            SetEntryVisible(entry, false);
            return;
        }

        bool healthVisible = TryGetLocalPoint(
            anchor,
            anchor.GetHealthBarLayout(),
            out Vector2 healthLocalPoint);

        bool castBarVisible = TryGetLocalPoint(
            anchor,
            anchor.GetInfectionCastBarLayout(),
            out Vector2 castBarLocalPoint);

        if (entry.HealthBar != null)
        {
            entry.HealthBar.SetManagerVisible(healthVisible);

            if (healthVisible)
                entry.HealthBar.SetScreenPosition(healthLocalPoint);

            entry.HealthBar.TickVisualState();
        }

        if (entry.CastBar != null)
        {
            entry.CastBar.SetManagerVisible(castBarVisible);

            if (castBarVisible)
                entry.CastBar.SetScreenPosition(castBarLocalPoint);

            entry.CastBar.TickVisualState();
        }
    }

    private static void SetEntryVisible(EnemyUIEntry entry, bool visible)
    {
        if (entry == null)
            return;

        if (entry.HealthBar != null)
            entry.HealthBar.SetManagerVisible(visible);

        if (entry.CastBar != null)
            entry.CastBar.SetManagerVisible(visible);
    }

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
