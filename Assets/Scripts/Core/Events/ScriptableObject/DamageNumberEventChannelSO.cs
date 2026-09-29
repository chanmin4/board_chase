/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Handles DamageNumberKind combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum DamageNumberKind
{
    Health = 0,
    Armor = 1,
    PaintMark = 2,
    Infection = 3,
    Heal = 4
}

public readonly struct DamageNumberRequest
{
    public readonly GameObject Target;
    public readonly Vector3 WorldPosition;
    public readonly float Amount;
    public readonly DamageNumberKind Kind;
    public readonly bool UseWorldPosition;
    public readonly bool IsCritical;

    public bool IsValid => Amount > 0f && (Target != null || UseWorldPosition);

    public DamageNumberRequest(
        GameObject target,
        Vector3 worldPosition,
        float amount,
        DamageNumberKind kind,
        bool useWorldPosition,
        bool isCritical)
    {
        Target = target;
        WorldPosition = worldPosition;
        Amount = amount;
        Kind = kind;
        UseWorldPosition = useWorldPosition;
        IsCritical = isCritical;
    }
}

[CreateAssetMenu(
    fileName = "DamageNumberEventChannel",
    menuName = "Events/UI/Damage Number Event Channel")]
public class DamageNumberEventChannelSO : DescriptionBaseSO
{
    public event Action<DamageNumberRequest> OnEventRaised;

    // Purpose: Publishes the health damage notification.
    public void RaiseHealthDamage(
        GameObject target,
        float amount,
        bool isCritical = false)
    {
        RaiseDamage(
            target,
            amount,
            DamageNumberKind.Health,
            isCritical);
    }

    // Purpose: Publishes the infection damage notification.
    public void RaiseInfectionDamage(
        GameObject target,
        float amount,
        bool isCritical = false)
    {
        RaiseDamage(
            target,
            amount,
            DamageNumberKind.Infection,
            isCritical);
    }

    // Purpose: Publishes the heal notification.
    public void RaiseHeal(
        GameObject target,
        float amount,
        bool isCritical = false)
    {
        RaiseDamage(
            target,
            amount,
            DamageNumberKind.Heal,
            isCritical);
    }

    // Purpose: Publishes the damage notification.
    public void RaiseDamage(
        GameObject target,
        float amount,
        DamageNumberKind kind = DamageNumberKind.Health,
        bool isCritical = false)
    {
        RaiseEvent(new DamageNumberRequest(
            target,
            default,
            amount,
            kind,
            false,
            isCritical));
    }

    // Purpose: Publishes the damage at notification.
    public void RaiseDamageAt(
        Vector3 worldPosition,
        float amount,
        DamageNumberKind kind = DamageNumberKind.Health,
        bool isCritical = false)
    {
        RaiseEvent(new DamageNumberRequest(
            null,
            worldPosition,
            amount,
            kind,
            true,
            isCritical));
    }

    // Purpose: Publishes the health damage at notification.
    public void RaiseHealthDamageAt(
        Vector3 worldPosition,
        float amount,
        bool isCritical = false)
    {
        RaiseDamageAt(
            worldPosition,
            amount,
            DamageNumberKind.Health,
            isCritical);
    }

    // Purpose: Publishes the infection damage at notification.
    public void RaiseInfectionDamageAt(
        Vector3 worldPosition,
        float amount,
        bool isCritical = false)
    {
        RaiseDamageAt(
            worldPosition,
            amount,
            DamageNumberKind.Infection,
            isCritical);
    }

    // Purpose: Publishes the heal at notification.
    public void RaiseHealAt(
        Vector3 worldPosition,
        float amount,
        bool isCritical = false)
    {
        RaiseDamageAt(
            worldPosition,
            amount,
            DamageNumberKind.Heal,
            isCritical);
    }

    // Purpose: Publishes the event notification.
    public void RaiseEvent(DamageNumberRequest request)
    {
        if (!request.IsValid)
            return;

        OnEventRaised?.Invoke(request);
    }
}
