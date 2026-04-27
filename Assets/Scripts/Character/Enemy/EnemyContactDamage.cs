using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Hitboxes")]
    [SerializeField] private EnemyContactAttackbox[] _hitboxes;

    [Header("Damage")]
    [SerializeField] private float _damage = 5f;
    [SerializeField] private float _hitCooldown = 1f;

    private float _nextHitTime;

    private void Awake()
    {
        RegisterHitboxes();
    }

    private void RegisterHitboxes()
    {
        for (int i = 0; i < _hitboxes.Length; i++)
        {
            if (_hitboxes[i] != null)
                _hitboxes[i].Initialize(this);
        }
    }

    public void TryDamage(Collider other)
    {
        if (Time.time < _nextHitTime)
            return;

        Damageable damageable = other.GetComponentInParent<Damageable>();
        if (damageable == null)
            return;

        if (damageable.gameObject == gameObject)
            return;

        if (!damageable.CanReceiveDamage)
            return;

        damageable.ReceiveAnAttack(_damage);
        _nextHitTime = Time.time + _hitCooldown;
    }
}
