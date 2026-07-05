using UnityEngine;

[DisallowMultipleComponent]
public class EnemyShooterInfection : ShooterInfection
{
    [Header("Enemy Refs")]
    [SerializeField] private EnemyShooterStatsRuntime _statsRuntime;

    protected override EntityInfectionSettings Rules =>
        Damageable != null && Damageable.StatConfig != null
            ? Damageable.StatConfig.Infection
            : _statsRuntime != null && _statsRuntime.StatConfig != null
                ? _statsRuntime.StatConfig.Infection
                : null;

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