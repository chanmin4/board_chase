using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]

public class EnemyScreenSpaceUIManager : MonoBehaviour
{
    public static EnemyScreenSpaceUIManager Instance { get; private set; }
    

    [Header("Refs")]
    [Tooltip("enemy health bars will be spawned as children of this transform")]
    [SerializeField] private RectTransform _widgetRoot;
    [SerializeField] private EnemyHealthBarWidget _healthBarPrefab;
    [SerializeField] private EnemyInfectionCastBarWidget _castBarPrefab;

    [Header("Listening To")]
    [SerializeField] private WorldCameraEventChannelSO _worldCameraReadyChannel;

    [Header("Runtime Don't Touch")]
    [ReadOnly] [SerializeField] private Camera _worldCamera;

    private sealed class EnemyUIEntry
    {
        public EnemyHealthBarWidget HealthBar;
        public EnemyInfectionCastBarWidget CastBar;
    }

    private readonly Dictionary<EnemyScreenSpaceUIAnchor, EnemyUIEntry> _widgets = new();
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
            EnemyScreenSpaceUIAnchor anchor = pair.Key;
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

    public void Register(EnemyScreenSpaceUIAnchor anchor)
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

      public void Unregister(EnemyScreenSpaceUIAnchor anchor)
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

     private void UpdateWidget(EnemyScreenSpaceUIAnchor anchor, EnemyUIEntry entry)
    {
        Vector3 worldPos = anchor.GetWorldUIPosition();
        Vector3 screenPos = _worldCamera.WorldToScreenPoint(worldPos);

        EnemyUIFollowSettingsSO follow = anchor.FollowSettings;

        if (follow != null && follow.HideWhenBehindCamera && screenPos.z <= 0f)
        {
            if (entry.HealthBar != null) entry.HealthBar.gameObject.SetActive(false);
            if (entry.CastBar != null) entry.CastBar.gameObject.SetActive(false);
            return;
        }

        bool isOffScreen =
            screenPos.x < 0f || screenPos.x > Screen.width ||
            screenPos.y < 0f || screenPos.y > Screen.height;

        if (follow != null && follow.HideWhenOffScreen && isOffScreen)
        {
            if (entry.HealthBar != null) entry.HealthBar.gameObject.SetActive(false);
            if (entry.CastBar != null) entry.CastBar.gameObject.SetActive(false);
            return;
        }

        Vector2 finalScreen = new Vector2(screenPos.x, screenPos.y);

        if (follow != null && follow.ClampToScreen)
        {
            float pad = follow.ScreenEdgePadding;
            finalScreen.x = Mathf.Clamp(finalScreen.x, pad, Screen.width - pad);
            finalScreen.y = Mathf.Clamp(finalScreen.y, pad, Screen.height - pad);
        }

        if (follow != null)
            finalScreen += follow.ScreenPixelOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _widgetRoot,
            finalScreen,
            null,
            out Vector2 localPoint);

        if (entry.HealthBar != null)
        {
            entry.HealthBar.gameObject.SetActive(true);
            entry.HealthBar.SetScreenPosition(localPoint);
            entry.HealthBar.TickVisualState();
        }

        if (entry.CastBar != null)
        {
            entry.CastBar.gameObject.SetActive(true);
            entry.CastBar.SetScreenPosition(localPoint);
            entry.CastBar.TickVisualState();
        }
    }
}
