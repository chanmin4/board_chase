/*
 * Role hierarchy: Gameplay > Entities > Common > StatusEffects
 * Primary responsibility: Coordinates the EntityStatusEffectRuntime subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed partial class EntityStatusEffectRuntime : MonoBehaviour
{
    public sealed class ActiveStatusEffect
    {
        public StatusEffectDefinitionSO Definition { get; }
        public float ExpiresAt { get; private set; }
        private int _stacks = 1;
        private float _tickRemaining;
        private readonly InfectionExposureSaveData _accumulation;
        public int Stacks => _accumulation != null ? _accumulation.stacks : _stacks;
        public float Gauge => _accumulation != null ? _accumulation.gauge : 0f;
        public bool UsesAccumulationState(InfectionExposureSaveData state) => ReferenceEquals(_accumulation, state);
        public float TickRemaining { get => _accumulation != null ? _accumulation.nextTick : _tickRemaining;
            set { if (_accumulation != null) _accumulation.nextTick = value; else _tickRemaining = value; } }
        public float RemainingSeconds => Mathf.Max(0f, ExpiresAt - Time.time);

        public ActiveStatusEffect(StatusEffectDefinitionSO definition, float expiresAt, InfectionExposureSaveData accumulation = null)
        {
            Definition = definition;
            ExpiresAt = expiresAt;
            _accumulation = accumulation;
            if (accumulation == null) TickRemaining = definition.DamageTickInterval;
        }

        public void SetStacks(int stacks)
        {
            if (_accumulation != null) _accumulation.stacks = Mathf.Clamp(stacks, 0, Definition.MaximumStacks);
            else _stacks = Mathf.Clamp(stacks, 1, Definition.MaximumStacks);
        }

        // Purpose: Refreshes the displayed or cached refresh.
        public void Refresh(float expiresAt)
        {
            ExpiresAt = expiresAt;
        }
    }

    private readonly List<ActiveStatusEffect> _activeEffects = new();

    public event Action<EntityStatusEffectRuntime> OnChanged;

    public IReadOnlyList<ActiveStatusEffect> ActiveEffects => _activeEffects;
    public bool HasAny => _activeEffects.Count > 0;
    public bool BlocksActions => HasFlag(definition => definition.BlocksActions);
    public bool SuppressesSoundIndicators => HasFlag(definition => definition.SuppressesSoundIndicators);
    public bool SuppressesInjuryApplications => HasFlag(definition => definition.SuppressesInjuryApplications);
    public int ArmorClassDelta => GetArmorClassDelta();

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        // The presenter resolves the existing entity UI anchor, so Player and Enemy
        // prefabs do not need separate status-grid wiring.
        EntityStatusEffectWorldGridUI grid = GetComponent<EntityStatusEffectWorldGridUI>();
        if (grid == null)
            grid = gameObject.AddComponent<EntityStatusEffectWorldGridUI>();

        grid.Bind(this);
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        TickInfectionAggravation(Time.deltaTime);
        bool changed = false;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var active = _activeEffects[i];
            if (active.Definition is InfectionAggravationStatusSO) continue; // Already ticked once above; never deduct HP as well.
            if (active.Definition.PersistentStacks)
            {
                active.TickRemaining -= Time.deltaTime;
                if (active.TickRemaining <= 0f)
                {
                    float interval = active.Definition.DamageTickInterval;
                    int ticks = 1 + Mathf.FloorToInt(-active.TickRemaining / interval);
                    active.TickRemaining += ticks * interval;
                    EntityComponentResolver.Resolve<Damageable>(this)?.ReceiveStatusDamageOverTime(
                        active.Definition.DamagePerStackPerSecond * active.Stacks * interval * ticks);
                    // Death may clear the runtime through lifecycle callbacks.
                    if (_activeEffects.Count <= i) break;
                }
                continue;
            }
            if (_activeEffects[i].ExpiresAt > Time.time)
                continue;

            _activeEffects.RemoveAt(i);
            changed = true;
        }

        if (changed)
            OnChanged?.Invoke(this);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_activeEffects.Count == 0)
            return;

        _activeEffects.Clear();
        OnChanged?.Invoke(this);
    }

    // Purpose: Applies the requested apply change.
    public bool Apply(StatusEffectDefinitionSO definition, float durationOverride = -1f)
    {
        if (definition == null)
            return false;
        if (definition is InfectionAggravationStatusSO aggravation)
        {
            AddInfectionAggravation(aggravation.Settings.baseGaugeCapacity);
            return true;
        }

        float duration = durationOverride > 0f
            ? durationOverride
            : definition.DurationSeconds;
        float expiresAt = definition.PersistentStacks ? float.PositiveInfinity : Time.time + Mathf.Max(0.05f, duration);

        for (int i = 0; i < _activeEffects.Count; i++)
        {
            ActiveStatusEffect active = _activeEffects[i];

            if (active.Definition != definition)
                continue;

            // Same effect never stacks. Contact only refreshes the one timer.
            if (definition.PersistentStacks) active.SetStacks(active.Stacks + 1);
            else active.Refresh(expiresAt);
            OnChanged?.Invoke(this);
            return false;
        }

        _activeEffects.Add(new ActiveStatusEffect(definition, expiresAt));
        OnChanged?.Invoke(this);
        return true;
    }

    /// <summary>
    /// Removes every active status effect. Used by explicit runtime reset and debug actions.
    /// </summary>
    public bool RemoveAll()
    {
        ClearInfectionAggravation();
        if (_activeEffects.Count == 0)
            return false;

        _activeEffects.Clear();
        OnChanged?.Invoke(this);
        return true;
    }

    // Purpose: Removes the requested remove from this component.
    public bool Remove(StatusEffectDefinitionSO definition)
    {
        if (definition == null)
            return false;
        if (definition is InfectionAggravationStatusSO) ClearInfectionAggravation();

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].Definition != definition)
                continue;

            _activeEffects.RemoveAt(i);
            OnChanged?.Invoke(this);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Copies timed effects into the resumable player-run snapshot. Effects
    /// without remaining duration are ignored; their normal Update cleanup is
    /// still the authoritative expiry path during play.
    /// </summary>
    public void CaptureRunState(PlayerRunStateSaveData target)
    {
        if (target == null)
            return;

        target.statusEffects.Clear();

        for (int index = 0; index < _activeEffects.Count; index++)
        {
            ActiveStatusEffect active = _activeEffects[index];
            StatusEffectDefinitionSO definition = active != null ? active.Definition : null;
            if (definition is InfectionAggravationStatusSO) continue; // Saved once in RunData.infectionExposure.
            float remaining = active != null ? active.RemainingSeconds : 0f;

            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || remaining <= 0f)
                continue;

            target.statusEffects.Add(new PlayerRunStatusEffectSaveData
            {
                statusEffectId = definition.Id,
                remainingSeconds = definition.PersistentStacks ? 1f : remaining,
                stacks = active.Stacks,
                tickRemaining = active.TickRemaining
            });
        }
    }

    /// <summary>
    /// Restores the saved remaining durations after the player prefab exists.
    /// Failure is reported before mutation by PlayerRunStatePersistence, so
    /// this method only receives catalog-resolved definitions.
    /// </summary>
    public bool TryRestoreRunState(
        PlayerRunStateSaveData source,
        RuntimeContentCatalogSO catalog,
        out string message)
    {
        message = string.Empty;

        if (source == null || !source.hasSnapshot)
        {
            message = "No resumable player run exists.";
            return false;
        }

        if (catalog == null)
        {
            message = "Runtime content catalog is missing.";
            return false;
        }

        for (int index = 0; index < source.statusEffects.Count; index++)
        {
            PlayerRunStatusEffectSaveData entry = source.statusEffects[index];
            if (entry == null ||
                !catalog.TryResolveStatusEffect(entry.statusEffectId, out _))
            {
                message = $"Saved status effect cannot be resolved. id={entry?.statusEffectId}";
                return false;
            }
        }

        _activeEffects.Clear();

        for (int index = 0; index < source.statusEffects.Count; index++)
        {
            PlayerRunStatusEffectSaveData entry = source.statusEffects[index];
            catalog.TryResolveStatusEffect(entry.statusEffectId, out StatusEffectDefinitionSO definition);
            Apply(definition, entry.remainingSeconds);
            var restored = _activeEffects[_activeEffects.Count - 1];
            restored.SetStacks(entry.stacks <= 0 ? 1 : entry.stacks);
            restored.TickRemaining = Mathf.Clamp(entry.tickRemaining, .001f, definition.DamageTickInterval);
        }

        OnChanged?.Invoke(this);
        message = $"Restored {source.statusEffects.Count} status effect(s).";
        return true;
    }

    // Purpose: Removes the requested effects cleared by friendly paint from this component.
    public void RemoveEffectsClearedByFriendlyPaint()
    {
        bool changed = false;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectDefinitionSO definition = _activeEffects[i].Definition;

            if (definition == null || !definition.RemoveOnFriendlyPaint)
                continue;

            _activeEffects.RemoveAt(i);
            changed = true;
        }

        if (changed)
            OnChanged?.Invoke(this);
    }

    /// <summary>
    /// Removes all currently active effects authored as bullet injuries. This intentionally
    /// does not touch unrelated debuffs such as paint slow or flash stun.
    /// </summary>
    public int RemoveInjuries()
    {
        int removedCount = 0;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectDefinitionSO definition = _activeEffects[i].Definition;

            if (definition == null || !definition.IsInjury)
                continue;

            _activeEffects.RemoveAt(i);
            removedCount++;
        }

        if (removedCount > 0)
            OnChanged?.Invoke(this);

        return removedCount;
    }

    public int BleedingStacks
    {
        get { int total = 0; foreach (var effect in _activeEffects) if (effect.Definition.IsBleeding) total += effect.Stacks; return total; }
    }

    public int TreatBleeding(int amount)
    {
        int remaining = Mathf.Max(0, amount);
        for (int i = _activeEffects.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var effect = _activeEffects[i];
            if (!effect.Definition.IsBleeding) continue;
            int removed = Mathf.Min(remaining, effect.Stacks); remaining -= removed;
            if (removed == effect.Stacks) _activeEffects.RemoveAt(i);
            else effect.SetStacks(effect.Stacks - removed);
        }
        if (remaining != amount) OnChanged?.Invoke(this);
        return Mathf.Max(0, amount) - remaining;
    }

    // Purpose: Checks whether this component has has.
    public bool Has(StatusEffectModifierType type)
    {
        return !Mathf.Approximately(GetMultiplier(type), 1f);
    }

    // Purpose: Returns the current multiplier.
    public float GetMultiplier(StatusEffectModifierType type)
    {
        float result = 1f;

        for (int i = 0; i < _activeEffects.Count; i++)
        {
            StatusEffectDefinitionSO definition = _activeEffects[i].Definition;
            StatusEffectModifier[] modifiers = definition != null ? definition.Modifiers : null;

            if (modifiers == null)
                continue;

            for (int modifierIndex = 0; modifierIndex < modifiers.Length; modifierIndex++)
            {
                if (modifiers[modifierIndex].type == type)
                    result *= Mathf.Max(0f, modifiers[modifierIndex].multiplier);
            }
        }

        return Mathf.Max(0f, result);
    }

    // Purpose: Checks whether this component has flag.
    private bool HasFlag(Func<StatusEffectDefinitionSO, bool> predicate)
    {
        if (predicate == null)
            return false;

        for (int i = 0; i < _activeEffects.Count; i++)
        {
            StatusEffectDefinitionSO definition = _activeEffects[i].Definition;

            if (definition != null && predicate(definition))
                return true;
        }

        return false;
    }

    // Purpose: Returns the current armor class delta.
    private int GetArmorClassDelta()
    {
        int result = 0;

        for (int i = 0; i < _activeEffects.Count; i++)
        {
            StatusEffectDefinitionSO definition = _activeEffects[i].Definition;

            if (definition != null)
                result += definition.ArmorClassDelta;
        }

        return result;
    }
}
