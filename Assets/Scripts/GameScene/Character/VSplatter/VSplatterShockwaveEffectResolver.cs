using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class VSplatterShockwaveEffectResolver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VSplatterShockwaveController _controller;
    [SerializeField] private VSplatterShockwaveEventChannelSO _shockwaveEventChannel;

    private void Awake()
    {
        if (_controller == null)
            TryGetComponent(out _controller);

        if (_shockwaveEventChannel == null && _controller != null)
            _shockwaveEventChannel = _controller.ShockwaveEventChannel;
    }

    private void OnEnable()
    {
        if (_shockwaveEventChannel != null)
        {
            _shockwaveEventChannel.OnEventRaised += HandleShockwaveEvent;
            return;
        }

        if (_controller != null)
            _controller.ShockwaveEventRaised += HandleShockwaveEvent;
    }

    private void OnDisable()
    {
        if (_shockwaveEventChannel != null)
        {
            _shockwaveEventChannel.OnEventRaised -= HandleShockwaveEvent;
            return;
        }

        if (_controller != null)
            _controller.ShockwaveEventRaised -= HandleShockwaveEvent;
    }

    private void HandleShockwaveEvent(VSplatterShockwaveEvent shockwaveEvent)
    {
        if (shockwaveEvent.eventType != VSplatterShockwaveEventType.Released)
            return;

        Collider[] hits = Physics.OverlapSphere(
            shockwaveEvent.center,
            shockwaveEvent.radius,
            shockwaveEvent.hitMask,
            shockwaveEvent.triggerInteraction);

        if (hits == null || hits.Length == 0)
            return;

        Transform selfRoot = shockwaveEvent.sender != null
            ? shockwaveEvent.sender.transform.root
            : transform.root;

        HashSet<Damageable> damagedTargets = shockwaveEvent.applyDamage ? new HashSet<Damageable>() : null;
        HashSet<KnockbackReceiver> knockedTargets = shockwaveEvent.applyKnockback ? new HashSet<KnockbackReceiver>() : null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            Damageable damageable = shockwaveEvent.applyDamage ? hit.GetComponentInParent<Damageable>() : null;
            KnockbackReceiver knockbackReceiver = shockwaveEvent.applyKnockback ? hit.GetComponentInParent<KnockbackReceiver>() : null;

            if (damageable != null && damageable.transform.root == selfRoot)
                damageable = null;

            if (knockbackReceiver != null && knockbackReceiver.transform.root == selfRoot)
                knockbackReceiver = null;

            if (damageable != null && damagedTargets.Add(damageable))
                damageable.ReceiveAnAttack(shockwaveEvent.damage);

            if (knockbackReceiver != null && knockedTargets.Add(knockbackReceiver))
            {
                KnockbackRequest request = KnockbackRequest.FromSource(
                    shockwaveEvent.center,
                    knockbackReceiver.transform.position,
                    shockwaveEvent.knockbackDistance,
                    shockwaveEvent.knockbackDuration);

                knockbackReceiver.RequestKnockback(request);
            }
        }
    }
}
