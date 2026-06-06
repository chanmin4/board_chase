using System;
using UnityEngine;

public enum VSplatterShockwaveEventType
{
    ChargeStarted,
    Released,
    Canceled
}

[Serializable]
public struct VSplatterShockwaveEvent
{
    public VSplatterShockwaveEventType eventType;
    public GameObject sender;
    public Transform origin;
    public Vector3 center;
    public float chargeDuration;
    public float chargeNormalized;
    public float radius;
    public float damage;
    public float knockbackDistance;
    public float knockbackDuration;
    public LayerMask hitMask;
    public QueryTriggerInteraction triggerInteraction;
    public bool applyDamage;
    public bool applyKnockback;

    public VSplatterShockwaveEvent(
        VSplatterShockwaveEventType eventType,
        GameObject sender,
        Transform origin,
        Vector3 center,
        float chargeDuration,
        float chargeNormalized,
        float radius,
        float damage,
        float knockbackDistance,
        float knockbackDuration,
        LayerMask hitMask,
        QueryTriggerInteraction triggerInteraction,
        bool applyDamage,
        bool applyKnockback)
    {
        this.eventType = eventType;
        this.sender = sender;
        this.origin = origin;
        this.center = center;
        this.chargeDuration = chargeDuration;
        this.chargeNormalized = chargeNormalized;
        this.radius = radius;
        this.damage = damage;
        this.knockbackDistance = knockbackDistance;
        this.knockbackDuration = knockbackDuration;
        this.hitMask = hitMask;
        this.triggerInteraction = triggerInteraction;
        this.applyDamage = applyDamage;
        this.applyKnockback = applyKnockback;
    }
}

[CreateAssetMenu(
    fileName = "VSplatterShockwaveEventChannel",
    menuName = "Events/Player/VSplatter Shockwave Event Channel")]
public class VSplatterShockwaveEventChannelSO : ScriptableObject
{
    public event Action<VSplatterShockwaveEvent> OnEventRaised;

    public void RaiseEvent(VSplatterShockwaveEvent shockwaveEvent)
    {
        OnEventRaised?.Invoke(shockwaveEvent);
    }
}
