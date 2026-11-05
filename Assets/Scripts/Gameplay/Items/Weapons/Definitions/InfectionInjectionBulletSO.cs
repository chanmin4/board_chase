/*
 * Role hierarchy: Gameplay > Items > Weapons > Definitions
 * Primary responsibility: Defines the special one-shot Infection Injection ammunition and its refreshed health-DOT.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(
    fileName = "InfectionInjectionBullet",
    menuName = "Game/Bullet Item/Special/Infection Injection")]
public sealed class InfectionInjectionBulletSO : SpecialBulletSO
{
    [Header("Damage Targets")]
    [Tooltip("Damageable layers when this injection is fired by the player.")]
    [SerializeField] private LayerMask _playerDamageableTargetMask;

    [Tooltip("Damageable layers when this injection is fired by an enemy.")]
    [SerializeField] private LayerMask _enemyDamageableTargetMask;

    [Header("Infection Damage Over Time")]
    [Tooltip("How long the refreshed infection DOT lasts after a successful hit.")]
    [SerializeField, Min(0.05f)] private float _durationSeconds = 6f;

    [Tooltip("Raw health damage applied per second. Target Infection DOT resistance is applied at each tick.")]
    [SerializeField, Min(0f)] private float _damagePerSecond = 10f;

    [Tooltip("DOT simulation interval. This does not change total intended damage.")]
    [SerializeField, Min(0.05f)] private float _tickInterval = 0.25f;

    [Tooltip("Optional status definition shown by the existing world status-effect UI while the DOT is active.")]
    [SerializeField] private StatusEffectDefinitionSO _statusEffect;

    public override Color DebugLineColor => new Color(0.42f, 1f, 0.25f, 1f);
    public override int MaxStack => 1;
    public override int MaxOwnedAmount => 1;

    // Purpose: Resolves the damage target mask needed by this component.
    public override LayerMask ResolveDamageTargetMask(ShooterFaction faction)
    {
        return faction != ShooterFaction.Player
            ? _enemyDamageableTargetMask
            : _playerDamageableTargetMask;
    }

    // Purpose: Validates special configuration and reports invalid authored data.
    public override bool ValidateSpecialConfiguration(out string error)
    {
        if (ProjectilePrefab == null)
        {
            error = $"[InfectionInjectionBulletSO] Projectile Prefab is missing: {name}";
            return false;
        }

        if (!ProjectilePrefab.TryGetComponent<BasicBullet>(out _))
        {
            error = $"[InfectionInjectionBulletSO] Projectile Prefab requires BasicBullet: {ProjectilePrefab.name}";
            return false;
        }

        if (_playerDamageableTargetMask.value == 0 || _enemyDamageableTargetMask.value == 0)
        {
            error = $"[InfectionInjectionBulletSO] Assign both player and enemy target masks: {name}";
            return false;
        }

        if (_damagePerSecond <= 0f || _durationSeconds <= 0f)
        {
            error = $"[InfectionInjectionBulletSO] DOT duration and damage per second must be above zero: {name}";
            return false;
        }

        error = null;
        return true;
    }

    // Purpose: Attempts to initialize projectile and reports whether the request succeeded.
    public override bool TryInitializeProjectile(
        GameObject projectileInstance,
        in SpecialBulletLaunchContext context,
        out string error)
    {
        if (projectileInstance == null ||
            !projectileInstance.TryGetComponent<BasicBullet>(out BasicBullet projectile))
        {
            error = $"[InfectionInjectionBulletSO] Spawned projectile requires BasicBullet: {name}";
            return false;
        }

        Vector3 visualDirection = context.VisualTarget - context.VisualStart;
        float maxDistance = Vector3.Distance(context.GameplayStart, context.ImpactTarget);

        projectile.Init(
            this,
            context.GameplayStart,
            context.GameplayDirection,
            visualDirection,
            Mathf.Max(0.1f, maxDistance),
            Speed,
            CastRadius,
            MaxLifetime,
            0f,
            0,
            0f,
            0f,
            0f,
            0f,
            0f,
            ResolveDamageTargetMask(ResolveSourceFaction(context.Source)),
            ProjectileCollisionMask,
            ProjectileTriggerInteraction,
            context.Source,
            context.SoundStimulusEvent,
            null);

        projectile.ConfigureInfectionInjection(
            _damagePerSecond,
            _durationSeconds,
            _tickInterval,
            _statusEffect);

        error = null;
        return true;
    }

    // Purpose: Resolves the source faction needed by this component.
    private static ShooterFaction ResolveSourceFaction(GameObject source)
    {
        Transform root = source != null
            ? EntityComponentResolver.ResolveEntityRoot(source.transform)
            : null;

        if (root != null &&
            (root.GetComponent<PlayerCharacter>() != null ||
             root.GetComponentInChildren<PlayerCharacter>(true) != null))
        {
            return ShooterFaction.Player;
        }

        return ShooterFactionUtility.Resolve(root);
    }
}
