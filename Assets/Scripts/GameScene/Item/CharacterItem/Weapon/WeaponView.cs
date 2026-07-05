using UnityEngine;

public class WeaponView : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Required. Projectile spawn origin for this weapon.")]
    [SerializeField] private Transform _fireOrigin;

    [Tooltip("Optional. If empty, Fire Origin forward is used.")]
    [SerializeField] private Transform _fireDirectionReference;

    public Transform FireOrigin => _fireOrigin;
    public bool HasFireOrigin => _fireOrigin != null;

    public Vector3 FireDirection
    {
        get
        {
            Transform directionSource = _fireDirectionReference != null
                ? _fireDirectionReference
                : _fireOrigin;

            return directionSource != null ? directionSource.forward : Vector3.forward;
        }
    }
}
