#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>Executed BasicBullet flight sweeps. Prediction queries are intentionally excluded.</summary>
public static class CombatPhysicsTrace
{
    public struct Sweep
    {
        public Vector3 Origin, End, HitPoint;
        public float Radius, Time;
        public int SourceId, ProjectileId, SectorId;
        public string SourceName, HitName;
        public bool Hit, DamageHit;
    }
    public static bool Enabled { get; set; }
    private static readonly List<Sweep> Sweeps = new();
    public static IReadOnlyList<Sweep> Recent => Sweeps;
    public static void Clear() => Sweeps.Clear();
    public static void Record(BasicBullet projectile, GameObject source, Vector3 origin, Vector3 direction,
        float radius, float distance, bool hit, RaycastHit contact, bool damageHit)
    {
        if (!Enabled) return;
        var sector = source != null ? source.GetComponentInParent<SectorRuntime>() : null;
        Sweeps.Add(new Sweep
        {
            Origin = origin, End = origin + direction * distance, Radius = radius, Time = UnityEngine.Time.time,
            SourceId = source != null ? source.GetInstanceID() : 0,
            SourceName = source != null ? source.name : "없음", ProjectileId = projectile.gameObject.GetInstanceID(),
            SectorId = sector != null ? sector.GetInstanceID() : 0,
            Hit = hit, HitPoint = contact.point, DamageHit = damageHit,
            HitName = hit && contact.collider != null ? contact.collider.name : null
        });
        if (Sweeps.Count > 512) Sweeps.RemoveAt(0);
    }
}
#endif
