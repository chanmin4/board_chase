/*
 * Role hierarchy: Gameplay > Entities > Shooter > Enemy > Common
 * Primary responsibility: Handles EnemyShooterInfection combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyShooterInfection : ShooterInfection
{
    protected override PaintFaction FriendlyPaintFaction => PaintFaction.Virus;
    [Header("Enemy Refs")]
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;

    [Header("Enemy Infection Debug")]
    [Tooltip("Logs enemy Infection gain, health threshold, and death resolution. Disable after the enemy Infection UI/death issue is verified.")]
    [SerializeField] private bool _debugInfectionLifecycle;

    protected override bool DebugInfectionLifecycle => _debugInfectionLifecycle;

    protected override EntityInfectionSettings Rules =>
        Damageable != null && Damageable.StatConfig != null
            ? Damageable.StatConfig.Infection
            : _statsRuntime != null && _statsRuntime.StatConfig != null
                ? _statsRuntime.StatConfig.Infection
                : null;

    // Purpose: Resolves the refs needed by this component.
    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_statsRuntime == null)
        {
            _statsRuntime =
                GetComponent<EnemyShooterStatsRuntime>() ??
                GetComponentInParent<EnemyShooterStatsRuntime>() ??
                GetComponentInChildren<EnemyShooterStatsRuntime>(true);
        }
    }
}
