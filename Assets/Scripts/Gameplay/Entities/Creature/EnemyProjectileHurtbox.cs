/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Handles EnemyProjectileHurtbox combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectileHurtbox : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;

    [Header("Hit Colliders")]
    [Tooltip("비워두면 이 오브젝트 또는 자식 Collider를 허용합니다. 특정 콜라이더만 맞게 하고 싶으면 여기에 등록하세요.")]
    [SerializeField] private Collider[] _hurtColliders;

    public Damageable Damageable => _damageable;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_damageable == null)
            _damageable = GetComponentInParent<Damageable>();
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponentInParent<Damageable>();

        if (_hurtColliders == null || _hurtColliders.Length == 0)
            _hurtColliders = GetComponentsInChildren<Collider>(true);
    }

    // Purpose: Performs the accepts collider workflow for this component.
    public bool AcceptsCollider(Collider collider)
    {
        if (collider == null)
            return false;

        if (_hurtColliders != null && _hurtColliders.Length > 0)
        {
            for (int i = 0; i < _hurtColliders.Length; i++)
            {
                if (_hurtColliders[i] == collider)
                    return true;
            }

            return false;
        }

        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    // Purpose: Attempts to get damageable and reports whether the request succeeded.
    public bool TryGetDamageable(out Damageable damageable)
    {
        damageable = _damageable;

        if (damageable == null)
            damageable = GetComponentInParent<Damageable>();

        return damageable != null;
    }
}
