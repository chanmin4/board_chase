using UnityEngine;

[DisallowMultipleComponent]
public class EnemyScreenSpaceUIAnchor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private Transform _uiAnchor;
    [SerializeField] private EnemyUIFollowSettingsSO _followSettings;
    [SerializeField] private EnemyHealthBarSettingsSO _healthBarSettings;

    public Damageable Damageable => _damageable;
    public Enemy Enemy => _enemy;
    public Transform UIAnchor => _uiAnchor != null ? _uiAnchor : transform;
    public EnemyUIFollowSettingsSO FollowSettings => _followSettings;
    public EnemyHealthBarSettingsSO HealthBarSettings => _healthBarSettings;

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_enemy == null)
            _enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        EnemyScreenSpaceUIManager.Instance?.Register(this);
    }

    private void Start()
    {
        EnemyScreenSpaceUIManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        EnemyScreenSpaceUIManager.Instance?.Unregister(this);
    }

    public Vector3 GetWorldUIPosition()
    {
        Vector3 anchorPos = UIAnchor.position;

        if (_followSettings != null)
            anchorPos += _followSettings.WorldOffset;

        return anchorPos;
    }
}
