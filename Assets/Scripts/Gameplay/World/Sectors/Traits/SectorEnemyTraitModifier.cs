/*
 * Role hierarchy: Gameplay > World > Sectors > Traits
 * Primary responsibility: Applies the selected sector's temporary enemy-side
 * stat package without mutating the shared EnemyShooterConfigSO.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public sealed class SectorEnemyTraitModifier : MonoBehaviour
{
    [ReadOnly] [SerializeField] private bool _armed;
    [ReadOnly] [SerializeField] private bool _damageAnomaly;
    [ReadOnly] [SerializeField] private float _attackDamageMultiplier = 1f;
    [ReadOnly] [SerializeField] private float _maxHealthMultiplier = 1f;
    [ReadOnly] [SerializeField] private float _moveSpeedMultiplier = 1f;
    [ReadOnly] [SerializeField] private int _armorClassBonus;

    private Damageable _damageable;
    private float _baseMaxHealth = -1f;

    public float AttackDamageMultiplier => Mathf.Max(1f, _attackDamageMultiplier);
    public float MaxHealthMultiplier => Mathf.Max(1f, _maxHealthMultiplier);
    public float MoveSpeedMultiplier => Mathf.Max(1f, _moveSpeedMultiplier);
    public int ArmorClassBonus => Mathf.Max(0, _armorClassBonus);
    // Firearms already snapshot AttackDamageMultiplier through EnemyShooterStatsRuntime.
    // Non-firearm attacks use this helper so the V3 anomaly is applied once, not twice.
    public static float NonFirearmAnomalyMultiplier(Component owner)
    {
        var modifier = owner != null ? EntityComponentResolver.Resolve<SectorEnemyTraitModifier>(owner) : null;
        return modifier != null && modifier._damageAnomaly ? modifier.AttackDamageMultiplier : 1f;
    }

    // Purpose: Configures this component''s configure.
    public void Configure(SectorRoomTraitTableSO table, bool armed, bool damageAnomaly = false)
    {
        _armed = armed && table != null;
        _damageAnomaly = damageAnomaly && !_armed;
        _attackDamageMultiplier = _armed ? table.ArmedEnemyAttackDamageMultiplier : damageAnomaly ? 1.25f : 1f;
        _maxHealthMultiplier = _armed ? table.ArmedEnemyMaxHealthMultiplier : 1f;
        _moveSpeedMultiplier = _armed ? table.ArmedEnemyMoveSpeedMultiplier : 1f;
        _armorClassBonus = _armed ? table.ArmedEnemyArmorClassBonus : 0;
        ApplyHealthMultiplier();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);
    }

    // Purpose: Applies the requested health multiplier change.
    private void ApplyHealthMultiplier()
    {
        ResolveRefs();

        if (_damageable == null)
            return;

        if (_baseMaxHealth <= 0f)
            _baseMaxHealth = Mathf.Max(1f, _damageable.MaxHealth);

        float healthNormalized = _damageable.HealthNormalized;

        _damageable.SetMaxHealth(
            _baseMaxHealth * MaxHealthMultiplier,
            healToFull: false);

        // Switching the debug room trait should preserve the current health
        // percentage instead of silently healing or damaging a live enemy.
        _damageable.SetCurrentHealth(
            _damageable.MaxHealth * Mathf.Clamp01(healthNormalized));
    }
}
