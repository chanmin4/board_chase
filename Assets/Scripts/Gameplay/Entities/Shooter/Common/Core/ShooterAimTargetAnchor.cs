/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles ShooterAimTargetAnchor camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShooterAimTargetAnchor : MonoBehaviour
{
    [Tooltip("World-space point that hostile shooters use as the center of this entity. Leave empty to use this Transform.")]
    [SerializeField] private Transform _anchor;

    public Transform Anchor => _anchor != null ? _anchor : transform;
}
