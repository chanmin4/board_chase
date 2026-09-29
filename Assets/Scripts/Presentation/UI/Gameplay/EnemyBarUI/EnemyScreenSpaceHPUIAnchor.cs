/*
 * Role hierarchy: Presentation > UI > Gameplay > EnemyBarUI
 * Primary responsibility: Implements the focused EnemyScreenSpaceHPUIAnchor responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
    private EnemyShooterConfigSO _enemyShooterConfig;

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

    // Purpose: Initializes Inspector defaults for this component.
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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        ResolveVisionVisibilityTarget();
        EnemyScreenSpaceHPUIManager.Instance?.Register(this);
    }

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        EnemyScreenSpaceHPUIManager.Instance?.Register(this);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        EnemyScreenSpaceHPUIManager.Instance?.Unregister(this);
    }

    // Purpose: Updates the enemy stat config state.
    public void SetEnemyStatConfig(EnemyStatConfigSO enemyStatConfig)
    {
        _enemyStatConfig = enemyStatConfig;
    }

    // Purpose: Updates the enemy shooter config state.
    public void SetEnemyShooterConfig(EnemyShooterConfigSO enemyShooterConfig)
    {
        _enemyShooterConfig = enemyShooterConfig;
    }

    // Purpose: Updates the vision visibility target state.
    public void SetVisionVisibilityTarget(VisionVisibilityTarget target)
    {
        _visionVisibilityTarget = target;
    }

    // Purpose: Returns the current health bar layout.
    public EnemyScreenSpaceWidgetLayout GetHealthBarLayout()
    {
        if (_enemyShooterConfig != null && _enemyShooterConfig.HealthBarUI != null)
            return _enemyShooterConfig.HealthBarUI;

        if (_enemyStatConfig != null && _enemyStatConfig.HealthBarUI != null)
            return _enemyStatConfig.HealthBarUI;

        return DefaultHealthBarLayout;
    }

    // Purpose: Returns the current infection cast bar layout.
    public EnemyScreenSpaceWidgetLayout GetInfectionCastBarLayout()
    {
        if (_enemyShooterConfig != null && _enemyShooterConfig.InfectionCastBarUI != null)
            return _enemyShooterConfig.InfectionCastBarUI;

        if (_enemyStatConfig != null && _enemyStatConfig.InfectionCastBarUI != null)
            return _enemyStatConfig.InfectionCastBarUI;

        return DefaultInfectionCastBarLayout;
    }

    // Purpose: Returns the current world ui position.
    public Vector3 GetWorldUIPosition(EnemyScreenSpaceWidgetLayout layout)
    {
        Vector3 anchorPos = UIAnchor.position;
        Vector3 offset = layout != null
            ? layout.WorldOffset
            : Vector3.zero;

        return anchorPos + offset;
    }

    // Purpose: Resolves the vision visibility target needed by this component.
    private void ResolveVisionVisibilityTarget()
    {
        if (_visionVisibilityTarget != null)
            return;

        _visionVisibilityTarget =
            GetComponent<VisionVisibilityTarget>() ??
            GetComponentInParent<VisionVisibilityTarget>();
    }
}
