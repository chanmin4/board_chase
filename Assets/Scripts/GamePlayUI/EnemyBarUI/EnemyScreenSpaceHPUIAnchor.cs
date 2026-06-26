using UnityEngine;

[DisallowMultipleComponent]
public class EnemyScreenSpaceHPUIAnchor : MonoBehaviour
{
    private static readonly EnemyScreenSpaceWidgetLayout DefaultHealthBarLayout =
        new EnemyScreenSpaceWidgetLayout(new Vector2(0f, 50f));

    private static readonly EnemyScreenSpaceWidgetLayout DefaultInfectionCastBarLayout =
        new EnemyScreenSpaceWidgetLayout(new Vector2(0f, 40f));

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private Transform _uiAnchor;
    [SerializeField] private EnemyHealthBarSettingsSO _healthBarSettings;
    private VisionVisibilityTarget _visionVisibilityTarget;

    private EnemyStatConfigSO _enemyStatConfig;

    public Damageable Damageable => _damageable;
    public Enemy Enemy => _enemy;
    public Transform UIAnchor => _uiAnchor != null ? _uiAnchor : transform;
    public EnemyHealthBarSettingsSO HealthBarSettings => _healthBarSettings;
    public bool IsVisionVisible
    {
        get
        {
            ResolveVisionVisibilityTarget();
            return _visionVisibilityTarget == null || _visionVisibilityTarget.IsVisible;
        }
    }

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_enemy == null)
            _enemy = GetComponent<Enemy>();

        if (_visionVisibilityTarget == null)
            _visionVisibilityTarget =
                GetComponent<VisionVisibilityTarget>() ??
                GetComponentInParent<VisionVisibilityTarget>();
    }

    private void OnEnable()
    {
        ResolveVisionVisibilityTarget();
        EnemyScreenSpaceHPUIManager.Instance?.Register(this);
    }

    private void Start()
    {
        EnemyScreenSpaceHPUIManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        EnemyScreenSpaceHPUIManager.Instance?.Unregister(this);
    }

    public void SetEnemyStatConfig(EnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }

    public void SetVisionVisibilityTarget(VisionVisibilityTarget target)
    {
        _visionVisibilityTarget = target;
    }

    public EnemyScreenSpaceWidgetLayout GetHealthBarLayout()
    {
        if (_enemyStatConfig != null && _enemyStatConfig.HealthBarUI != null)
            return _enemyStatConfig.HealthBarUI;

        return DefaultHealthBarLayout;
    }

    public EnemyScreenSpaceWidgetLayout GetInfectionCastBarLayout()
    {
        if (_enemyStatConfig != null && _enemyStatConfig.InfectionCastBarUI != null)
            return _enemyStatConfig.InfectionCastBarUI;

        return DefaultInfectionCastBarLayout;
    }

    public Vector3 GetWorldUIPosition(EnemyScreenSpaceWidgetLayout layout)
    {
        Vector3 anchorPos = UIAnchor.position;
        Vector3 offset = layout != null
            ? layout.WorldOffset
            : Vector3.zero;

        return anchorPos + offset;
    }

    private void ResolveVisionVisibilityTarget()
    {
        if (_visionVisibilityTarget != null)
            return;

        _visionVisibilityTarget =
            GetComponent<VisionVisibilityTarget>() ??
            GetComponentInParent<VisionVisibilityTarget>();
    }
}
