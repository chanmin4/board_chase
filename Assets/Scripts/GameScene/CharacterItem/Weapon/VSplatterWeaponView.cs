using UnityEngine;

public class VSplatterWeaponView : MonoBehaviour
{
    [SerializeField] private Transform _fireOrigin;
    [Tooltip("")]
    [SerializeField] private Transform _fireDirectionReference;

    public Transform FireOrigin => _fireOrigin != null ? _fireOrigin : transform;

    public Vector3 FireDirection
    {
        get
        {
            Transform directionSource = _fireDirectionReference != null
                ? _fireDirectionReference
                : _fireOrigin != null
                    ? _fireOrigin
                    : transform;

            return directionSource.forward;
        }
    }
}