using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectileHurtbox : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;

    public Damageable Damageable => _damageable;
    private void Awake()
    {
        if (_damageable == null)
            _damageable = GetComponentInParent<Damageable>();

        Debug.Log($"[EnemyProjectileHurtbox] {name} damageable={(_damageable != null ? _damageable.name : "null")}");
    }

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponentInParent<Damageable>();
    }

    public bool TryGetDamageable(out Damageable damageable)
    {
        damageable = _damageable;

        if (damageable == null)
            damageable = GetComponentInParent<Damageable>();

        return damageable != null;
    }
}
