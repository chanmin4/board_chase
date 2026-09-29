/*
 * Role hierarchy: Gameplay > Items > Weapons > Runtime
 * Primary responsibility: Presents MeleeWeaponView state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class MeleeWeaponView : WeaponView
{
    [Header("Swing Effect")]
    [Tooltip("World position and rotation used when spawning the MeleeWeaponSO swing particle prefab.")]
    [SerializeField] private Transform _swingEffectOrigin;

    public Transform SwingEffectOrigin => _swingEffectOrigin;
}
