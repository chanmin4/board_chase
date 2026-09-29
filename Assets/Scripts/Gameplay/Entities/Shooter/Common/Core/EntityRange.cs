/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles EntityRange camera, vision, range, or aiming calculations and presentation.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class EntityRange : MonoBehaviour
{
    [Header("Entity Range Refs")]
    [FormerlySerializedAs("_entityWeaponHolder")]
    [SerializeField] protected ShooterEquipmentController _entityEquipmentController;
    [SerializeField] protected Transform _entityRangeOrigin;
    [SerializeField] protected ShooterStatsRuntime _entityStatsRuntime;

    public virtual Transform RangeOrigin => _entityRangeOrigin != null ? _entityRangeOrigin : transform;

    public virtual WeaponSO CurrentWeapon =>
        _entityEquipmentController != null ? _entityEquipmentController.CurrentWeapon : null;

    public virtual float MaxRange =>
        _entityStatsRuntime != null ? Mathf.Max(0.1f, _entityStatsRuntime.MaxRange) : 0f;

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveEntityRangeRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveEntityRangeRefs();
    }

    // Purpose: Resolves the entity range refs needed by this component.
    protected virtual void ResolveEntityRangeRefs()
    {
        if (_entityRangeOrigin == null)
            _entityRangeOrigin = transform;

        if (_entityEquipmentController == null)
            _entityEquipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_entityStatsRuntime == null)
            _entityStatsRuntime = EntityComponentResolver.Resolve<ShooterStatsRuntime>(this);
    }

    // Purpose: Checks whether this component has valid weapon.
    public virtual bool HasValidWeapon()
    {
        return CurrentWeapon != null && MaxRange > 0f;
    }

    // Purpose: Checks whether within range is currently true.
    public virtual bool IsWithinRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return false;

        return VSplatterAimUtility.IsWithinFlatRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }

    // Purpose: Performs the clamp to range workflow for this component.
    public virtual Vector3 ClampToRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return worldPoint;

        return VSplatterAimUtility.ClampFlatPointToRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }
}
