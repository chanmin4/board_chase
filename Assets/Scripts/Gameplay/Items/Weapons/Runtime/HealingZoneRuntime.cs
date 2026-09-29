/*
 * Role hierarchy: Gameplay > Items > Weapons > Runtime
 * Primary responsibility: Coordinates the HealingZoneRuntime subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HealingZoneRuntime : SpecialAreaRuntime
{
    private readonly Collider[] _hits = new Collider[64];
    private readonly HashSet<Damageable> _healedThisTick = new();

    private float _healPerTick;
    private LayerMask _targetMask;
    private QueryTriggerInteraction _triggerInteraction;

    // Purpose: Configures this component''s configure.
    public void Configure(
        float radius,
        float durationSeconds,
        float tickInterval,
        float healPerTick,
        LayerMask targetMask,
        QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide)
    {
        _healPerTick = Mathf.Max(0f, healPerTick);
        _targetMask = targetMask;
        _triggerInteraction = triggerInteraction;

        ConfigureArea(
            radius,
            durationSeconds,
            tickInterval,
            SpecialAreaPolarity.Beneficial,
            0f);
    }

    // Purpose: Processes one update step for area.
    protected override void TickArea()
    {
        if (_healPerTick <= 0f)
            return;

        _healedThisTick.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            Radius,
            _hits,
            _targetMask,
            _triggerInteraction);

        for (int i = 0; i < count; i++)
        {
            Collider hit = _hits[i];
            if (hit == null)
                continue;

            Damageable damageable = EntityComponentResolver.Resolve<Damageable>(hit);

            if (damageable == null ||
                damageable.IsDead ||
                !_healedThisTick.Add(damageable))
            {
                continue;
            }

            damageable.Heal(_healPerTick);
        }
    }

    // Purpose: Responds to the draw gizmos selected callback.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, Radius));
    }
}
