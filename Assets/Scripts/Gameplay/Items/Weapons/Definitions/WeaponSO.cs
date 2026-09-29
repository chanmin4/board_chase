/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the WeaponSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Common weapon item data. Combat behavior belongs to a concrete weapon type.
/// Existing firearm assets are migrated to RangedWeaponSO, while melee and
/// throwable weapons use this shared inventory/equipment surface.
/// </summary>
public abstract class WeaponSO : ItemSO
{
    [Header("Identity")]
    [SerializeField] private string _weaponId = "weapon";
    [SerializeField] private string _displayName = "Weapon";


    [Header("View")]
    [FormerlySerializedAs("weaponPrefab")]
    [Tooltip("Prefab used when this weapon is equipped in hand. Keep this separate from the world pickup prefab.")]
    [SerializeField] private WeaponView _weaponViewPrefab;
    [SerializeField] private Vector3 _weaponViewLocalPosition;
    [SerializeField] private Vector3 _weaponViewLocalEulerAngles;
    [SerializeField] private Vector3 _weaponViewLocalScale = Vector3.one;

    [Header("Audio")]
    [Tooltip("Optional authored weapon audio handoff. When a cue is assigned here, it overrides the matching legacy cue field on the concrete weapon asset.")]
    [SerializeField] private WeaponAudioProfileSO _audioProfile;

    [Header("Stat Modifiers")]
    [SerializeField] private PlayerStatModifier[] _statModifiers;

    public string WeaponId => _weaponId;
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(_displayName)
            ? _displayName
            : _weaponId;

    public WeaponView WeaponViewPrefab => _weaponViewPrefab;
    public WeaponAudioProfileSO AudioProfile => _audioProfile;

    public PlayerStatModifier[] StatModifiers => _statModifiers;
    public override ItemKind Kind => ItemKind.Weapon;

    // Purpose: Applies the requested weapon view transform change.
    public void ApplyWeaponViewTransform(Transform viewTransform)
    {
        if (viewTransform == null)
            return;

        viewTransform.localPosition = _weaponViewLocalPosition;
        viewTransform.localRotation = Quaternion.Euler(_weaponViewLocalEulerAngles);
        viewTransform.localScale = SanitizeScale(_weaponViewLocalScale);
    }

    // Purpose: Normalizes scale to a safe authored value.
    private static Vector3 SanitizeScale(Vector3 scale)
    {
        const float min = 0.0001f;

        return new Vector3(
            Mathf.Abs(scale.x) < min ? 1f : scale.x,
            Mathf.Abs(scale.y) < min ? 1f : scale.y,
            Mathf.Abs(scale.z) < min ? 1f : scale.z);
    }
}
