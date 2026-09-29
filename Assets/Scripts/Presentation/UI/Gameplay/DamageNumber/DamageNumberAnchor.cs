/*
 * Role hierarchy: Presentation > UI > Gameplay > DamageNumber
 * Primary responsibility: Handles DamageNumberAnchor combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageNumberAnchor : MonoBehaviour
{
    [Header("Anchor")]
    [Tooltip("Optional point used as the popup origin. If empty, this transform is used.")]
    [SerializeField] private Transform _anchor;

    [Tooltip("World-space offset added after resolving the anchor point.")]
    [SerializeField] private Vector3 _worldOffset = new(0f, 1.4f, 0f);

    // Purpose: Resolves the world position needed by this component.
    public Vector3 ResolveWorldPosition()
    {
        Transform anchor = _anchor != null ? _anchor : transform;
        return anchor.position + _worldOffset;
    }
}
