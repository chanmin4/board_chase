using UnityEngine;

public class EnemyAttackRig : MonoBehaviour
{
    [SerializeField] private Transform _fireOrigin;
    [SerializeField] private Transform _projectileRoot;

    public Transform FireOrigin => _fireOrigin != null ? _fireOrigin : transform;
    public Transform ProjectileRoot => _projectileRoot;

    public void SetProjectileRoot(Transform projectileRoot)
    {
        _projectileRoot = projectileRoot;
    }
}
