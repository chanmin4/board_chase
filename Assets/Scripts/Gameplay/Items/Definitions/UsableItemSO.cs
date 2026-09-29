/*
 * Role hierarchy: Gameplay > Items > Definitions
 * Primary responsibility: Defines the UsableItemSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Inventory item that can be bound to one of the player's five quick slots.
/// The quick slot is only a binding; the actual stack remains in the inventory.
/// </summary>
public abstract class UsableItemSO : ItemSO
{
    [Header("Usable Identity")]
    [FormerlySerializedAs("_weaponId")]
    [SerializeField] private string _usableId = "usable";

    [FormerlySerializedAs("_displayName")]
    [SerializeField] private string _usableDisplayName = "Usable Item";

    [Header("Hand Socket Visual")]
    [FormerlySerializedAs("_weaponViewPrefab")]
    [Tooltip("Optional hand-only override. Leave empty on ordinary consumables to use World Visual Prefab with its saved transform. Throwables still require their dedicated WeaponView / ThrowableProjectile prefab.")]
    [SerializeField] private WeaponView _heldViewPrefab;

    [FormerlySerializedAs("_weaponViewLocalPosition")]
    [SerializeField] private Vector3 _heldViewLocalPosition;

    [FormerlySerializedAs("_weaponViewLocalEulerAngles")]
    [SerializeField] private Vector3 _heldViewLocalEulerAngles;

    [FormerlySerializedAs("_weaponViewLocalScale")]
    [SerializeField] private Vector3 _heldViewLocalScale = Vector3.one;

    public string UsableId => _usableId;
    public string UsableDisplayName =>
        !string.IsNullOrWhiteSpace(_usableDisplayName)
            ? _usableDisplayName
            : _usableId;
    public WeaponView HeldViewPrefab => _heldViewPrefab;
    public bool UsesWorldVisualInHand => _heldViewPrefab == null && !(this is ThrowableUsableItemSO);
    public GameObject HandVisualPrefab => _heldViewPrefab != null
        ? _heldViewPrefab.gameObject
        : UsesWorldVisualInHand ? WorldVisualPrefab : null;
    public override ItemKind Kind => ItemKind.Usable;

    // Purpose: Applies the requested held view transform change.
    public void ApplyHeldViewTransform(Transform viewTransform)
    {
        if (viewTransform == null)
            return;

        viewTransform.localPosition = _heldViewLocalPosition;
        viewTransform.localRotation = Quaternion.Euler(_heldViewLocalEulerAngles);
        viewTransform.localScale = SanitizeScale(_heldViewLocalScale);
    }

    // Purpose: Resolves the held view scale needed by this component.
    public Vector3 ResolveHeldViewScale()
    {
        return SanitizeScale(_heldViewLocalScale);
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
