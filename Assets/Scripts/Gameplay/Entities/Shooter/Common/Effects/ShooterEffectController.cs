/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Effects
 * Primary responsibility: Coordinates the ShooterEffectController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

public class ShooterEffectController : EntityEffectController
{
    [Header("Shooter Particles")]
    [SerializeField] private ParticleSystem _footTrailParticles;
    [SerializeField] private ParticleSystem _getHitParticles;
    [SerializeField] private ParticleSystem _deathParticles;
    [SerializeField] private ParticleSystem _dashParticles;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [Tooltip("Optional parent for looping status VFX. Defaults to this Shooter transform.")]
    [SerializeField] private Transform _statusVfxRoot;
    [SerializeField] private EntityStatusEffectRuntime _statusEffects;

    [Header("Listening To")]
    [SerializeField] private HitReceivedEventChannelSO _hitReceivedEvent;

    private bool _footTrailPlaying;
    private bool _loggedMissingStatusEffects;
    private readonly Dictionary<StatusEffectDefinitionSO, GameObject> _statusVfxInstances = new();

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();
        StopAllShooterParticles();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_damageable != null)
            _damageable.OnDie += PlayDeathParticles;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised += HandleHitReceived;

        if (_statusEffects != null)
        {
            _statusEffects.OnChanged += HandleStatusEffectsChanged;
            SyncStatusVfx();
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnDie -= PlayDeathParticles;

        if (_hitReceivedEvent != null)
            _hitReceivedEvent.OnEventRaised -= HandleHitReceived;

        if (_statusEffects != null)
            _statusEffects.OnChanged -= HandleStatusEffectsChanged;

        StopFootTrailParticles();
        StopDashParticles();
        ClearStatusVfx();
    }

    // Purpose: Plays the requested dash particles action or effect.
    public virtual void PlayDashParticles()
    {
        PlayIfAssigned(_dashParticles);
    }

    // Purpose: Stops the active dash particles behavior.
    public virtual void StopDashParticles()
    {
        StopIfAssigned(_dashParticles);
    }

    // Purpose: Plays the requested foot trail particles action or effect.
    public virtual void PlayFootTrailParticles()
    {
        _footTrailPlaying = true;
        PlayIfAssigned(_footTrailParticles);
    }

    // Purpose: Stops the active foot trail particles behavior.
    public virtual void StopFootTrailParticles()
    {
        _footTrailPlaying = false;
        StopIfAssigned(_footTrailParticles);
    }

    // Purpose: Updates the foot trail active state.
    public void SetFootTrailActive(bool active)
    {
        if (active)
        {
            if (!_footTrailPlaying)
                PlayFootTrailParticles();

            return;
        }

        if (_footTrailPlaying)
            StopFootTrailParticles();
    }

    // Purpose: Plays the requested get hit particles action or effect.
    public virtual void PlayGetHitParticles()
    {
        PlayIfAssigned(_getHitParticles);
    }

    // Purpose: Plays the requested death particles action or effect.
    public virtual void PlayDeathParticles()
    {
        PlayIfAssigned(_deathParticles);
    }

    // Purpose: Resolves the refs needed by this component.
    protected virtual void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

        if (_statusEffects == null)
            _statusEffects = EntityComponentResolver.Resolve<EntityStatusEffectRuntime>(this);

        if (_statusEffects == null && !_loggedMissingStatusEffects)
        {
            _loggedMissingStatusEffects = true;
            Debug.LogError(
                "[ShooterEffectController] EntityStatusEffectRuntime is missing from the Shooter entity root.",
                this);
        }

        if (_statusVfxRoot == null)
            _statusVfxRoot = transform;
    }

    // Purpose: Stops the active all shooter particles behavior.
    protected virtual void StopAllShooterParticles()
    {
        StopIfAssigned(_footTrailParticles);
        StopIfAssigned(_getHitParticles);
        StopIfAssigned(_deathParticles);
        StopIfAssigned(_dashParticles);
        _footTrailPlaying = false;
    }

    // Purpose: Handles the incoming hit received request or event.
    private void HandleHitReceived(GameObject hitTarget)
    {
        if (hitTarget == null || _damageable == null)
            return;

        Damageable hitDamageable =
            hitTarget.GetComponent<Damageable>() ??
            hitTarget.GetComponentInParent<Damageable>() ??
            hitTarget.GetComponentInChildren<Damageable>(true);

        if (hitDamageable != _damageable)
            return;

        PlayGetHitParticles();
    }

    // Purpose: Handles the incoming status effects changed request or event.
    private void HandleStatusEffectsChanged(EntityStatusEffectRuntime runtime)
    {
        SyncStatusVfx();
    }

    // Purpose: Synchronizes status vfx with the current source state.
    private void SyncStatusVfx()
    {
        if (_statusEffects == null)
        {
            ClearStatusVfx();
            return;
        }

        List<StatusEffectDefinitionSO> stale = new List<StatusEffectDefinitionSO>(_statusVfxInstances.Keys);
        IReadOnlyList<EntityStatusEffectRuntime.ActiveStatusEffect> activeEffects = _statusEffects.ActiveEffects;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            StatusEffectDefinitionSO definition = activeEffects[i].Definition;

            if (definition == null)
                continue;

            stale.Remove(definition);

            if (definition.WorldVfxPrefab == null || _statusVfxInstances.ContainsKey(definition))
                continue;

            GameObject instance = Instantiate(
                definition.WorldVfxPrefab,
                _statusVfxRoot != null ? _statusVfxRoot : transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            _statusVfxInstances.Add(definition, instance);
        }

        for (int i = 0; i < stale.Count; i++)
        {
            StatusEffectDefinitionSO definition = stale[i];

            if (_statusVfxInstances.TryGetValue(definition, out GameObject instance) && instance != null)
                Destroy(instance);

            _statusVfxInstances.Remove(definition);
        }
    }

    // Purpose: Clears the current status vfx state.
    private void ClearStatusVfx()
    {
        foreach (KeyValuePair<StatusEffectDefinitionSO, GameObject> pair in _statusVfxInstances)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        _statusVfxInstances.Clear();
    }
}
