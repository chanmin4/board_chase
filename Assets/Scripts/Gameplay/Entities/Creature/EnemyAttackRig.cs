/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Handles EnemyAttackRig combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class EnemyAttackRig : MonoBehaviour
{
    [SerializeField] private Transform _fireOrigin;
    [SerializeField] private Transform _projectileRoot;

    public Transform FireOrigin => _fireOrigin != null ? _fireOrigin : transform;
    public Transform ProjectileRoot => _projectileRoot;

    // Purpose: Updates the projectile root state.
    public void SetProjectileRoot(Transform projectileRoot)
    {
        _projectileRoot = projectileRoot;
    }
}
