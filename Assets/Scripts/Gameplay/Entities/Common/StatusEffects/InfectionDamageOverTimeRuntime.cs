/*
 * Role hierarchy: Gameplay > Entities > Common > StatusEffects
 * Primary responsibility: Applies the one refreshed health-DOT owned by Infection Injection ammunition.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Injection hits refresh one poison-style DOT instead of stacking unlimited
/// independent timers. This prevents repeated shots from becoming an
/// unbounded kite strategy while preserving a meaningful delayed damage tool.
/// </summary>
[DisallowMultipleComponent]
public sealed class InfectionDamageOverTimeRuntime : MonoBehaviour
{
    private Damageable _damageable;
    private GameObject _attacker;
    private float _damagePerSecond;
    private float _expiresAt;
    private float _lastAppliedAt;
    private float _nextTickAt;
    private float _tickInterval;

    // Purpose: Applies the requested apply change.
    public void Apply(
        Damageable damageable,
        GameObject attacker,
        float damagePerSecond,
        float durationSeconds,
        float tickInterval,
        StatusEffectDefinitionSO statusEffect)
    {
        _damageable = damageable != null ? damageable : GetComponent<Damageable>();
        _attacker = attacker;
        _damagePerSecond = Mathf.Max(0f, damagePerSecond);
        _tickInterval = Mathf.Max(0.05f, tickInterval);
        _expiresAt = Time.time + Mathf.Max(0.05f, durationSeconds);
        _lastAppliedAt = Time.time;
        _nextTickAt = Time.time + _tickInterval;

        if (_damageable == null || _damagePerSecond <= 0f)
        {
            Destroy(this);
            return;
        }

        if (statusEffect != null)
        {
            EntityStatusEffectRuntime statusRuntime =
                EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(_damageable);

            if (statusRuntime == null)
                statusRuntime = _damageable.gameObject.AddComponent<EntityStatusEffectRuntime>();

            statusRuntime.Apply(statusEffect, durationSeconds);
        }
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        if (_damageable == null || _damageable.IsDead || _damagePerSecond <= 0f)
        {
            Destroy(this);
            return;
        }

        if (Time.time < _nextTickAt && Time.time < _expiresAt)
            return;

        float tickEnd = Mathf.Min(Time.time, _expiresAt);
        float elapsed = Mathf.Max(0f, tickEnd - _lastAppliedAt);

        if (elapsed <= 0f)
        {
            Destroy(this);
            return;
        }

        _damageable.ReceiveInfectionDamageOverTime(
            _damagePerSecond * elapsed,
            _attacker);

        _lastAppliedAt = tickEnd;

        if (Time.time >= _expiresAt)
        {
            Destroy(this);
            return;
        }

        _nextTickAt = Time.time + _tickInterval;
    }
}
