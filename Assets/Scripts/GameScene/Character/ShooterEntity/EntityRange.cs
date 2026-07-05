using UnityEngine;

[DisallowMultipleComponent]
public class EntityRange : MonoBehaviour
{
    [Header("Entity Range Refs")]
    [SerializeField] protected EntityWeaponHolder _entityWeaponHolder;
    [SerializeField] protected Transform _entityRangeOrigin;
    [SerializeField] protected ShooterStatsRuntime _entityStatsRuntime;

    public virtual Transform RangeOrigin => _entityRangeOrigin != null ? _entityRangeOrigin : transform;

    public virtual WeaponSO CurrentWeapon =>
        _entityWeaponHolder != null ? _entityWeaponHolder.CurrentWeapon : null;

    public virtual float MaxRange =>
        _entityStatsRuntime != null ? Mathf.Max(0.1f, _entityStatsRuntime.MaxRange) : 0f;

    protected virtual void Reset()
    {
        ResolveEntityRangeRefs();
    }

    protected virtual void Awake()
    {
        ResolveEntityRangeRefs();
    }

    protected virtual void ResolveEntityRangeRefs()
    {
        if (_entityRangeOrigin == null)
            _entityRangeOrigin = transform;

        if (_entityWeaponHolder == null)
            _entityWeaponHolder = GetComponent<EntityWeaponHolder>() ?? GetComponentInParent<EntityWeaponHolder>();

        if (_entityStatsRuntime == null)
            _entityStatsRuntime = GetComponent<ShooterStatsRuntime>() ?? GetComponentInParent<ShooterStatsRuntime>();
    }

    public virtual bool HasValidWeapon()
    {
        return CurrentWeapon != null && MaxRange > 0f;
    }

    public virtual bool IsWithinRange(Vector3 worldPoint)
    {
        if (!HasValidWeapon())
            return false;

        return VSplatterAimUtility.IsWithinFlatRange(
            RangeOrigin.position,
            worldPoint,
            MaxRange);
    }

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
