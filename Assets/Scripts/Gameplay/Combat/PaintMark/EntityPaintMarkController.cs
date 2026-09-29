/*
 * Role hierarchy: Gameplay > Combat > PaintMark
 * Primary responsibility: Handles EntityPaintMarkController combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EntityPaintMarkController : MonoBehaviour
{
    private const float NaturalDecayPerSecond = 1f;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private EntityStatConfigSO _statConfigOverride;
    private EntityStatConfigSO _resolvedConfig;

    private PaintMarkFaction _activeFaction = PaintMarkFaction.None;
    private float _vaccineMarkAmount;
    private float _virusMarkAmount;

    public event Action<EntityPaintMarkController> OnChanged;

    public PaintMarkFaction ActiveFaction => _activeFaction;
    public float VaccineMarkAmount => _vaccineMarkAmount;
    public float VirusMarkAmount => _virusMarkAmount;

    public int VaccineStacks => CalculateStacks(PaintMarkFaction.Vaccine, _vaccineMarkAmount);
    public int VirusStacks => CalculateStacks(PaintMarkFaction.Virus, _virusMarkAmount);

    public int ActiveStacks =>
        _activeFaction == PaintMarkFaction.Vaccine ? VaccineStacks :
        _activeFaction == PaintMarkFaction.Virus ? VirusStacks :
        0;

    public float CurrentMarkAmount =>
        _activeFaction == PaintMarkFaction.Vaccine ? _vaccineMarkAmount :
        _activeFaction == PaintMarkFaction.Virus ? _virusMarkAmount :
        Mathf.Max(_vaccineMarkAmount, _virusMarkAmount);

    public float CurrentMaxMarkAmount =>
        _activeFaction == PaintMarkFaction.Virus ? MaxVirusMarkAmount : MaxVaccineMarkAmount;

    public float Normalized =>
        CurrentMaxMarkAmount > 0f
            ? Mathf.Clamp01(CurrentMarkAmount / CurrentMaxMarkAmount)
            : 0f;

    public bool IsMarkEnabled => Settings != null && Settings.UsePaintMark;
    public bool IsActive => _activeFaction != PaintMarkFaction.None;
    public int DisplayStacks => CalculateStacks(DisplayFaction, DisplayMarkAmount);
    public int ArmorClassDelta
    {
        get
        {
            EntityPaintMarkSettings settings = Settings;

            if (settings == null || !settings.UsePaintMark)
                return 0;

            return -ActiveStacks * settings.ArmorClassReductionPerStack;
        }
    }

    public float DisplayNormalized =>
        DisplayMaxMarkAmount > 0f
            ? Mathf.Clamp01(DisplayMarkAmount / DisplayMaxMarkAmount)
            : 0f;

    public PaintMarkFaction DisplayFaction
    {
        get
        {
            if (_activeFaction != PaintMarkFaction.None)
                return _activeFaction;

            if (_vaccineMarkAmount <= 0f && _virusMarkAmount <= 0f)
                return PaintMarkFaction.None;

            return _vaccineMarkAmount >= _virusMarkAmount
                ? PaintMarkFaction.Vaccine
                : PaintMarkFaction.Virus;
        }
    }

    public float DisplayMarkAmount
    {
        get
        {
            return DisplayFaction switch
            {
                PaintMarkFaction.Vaccine => _vaccineMarkAmount,
                PaintMarkFaction.Virus => _virusMarkAmount,
                _ => 0f
            };
        }
    }

    public float DisplayMaxMarkAmount
    {
        get
        {
            return DisplayFaction == PaintMarkFaction.Virus
                ? MaxVirusMarkAmount
                : MaxVaccineMarkAmount;
        }
    }

    private EntityPaintMarkSettings Settings
    {
        get
        {
            EntityStatConfigSO config = ResolveConfig();
            return config != null ? config.PaintMark : null;
        }
    }

    private float MarkAmountPerStack => Settings != null ? Settings.MarkAmountPerStack : 100f;
    private float MaxVaccineMarkAmount => Settings != null ? Settings.MaxVaccineMarkAmount : 300f;
    private float MaxVirusMarkAmount => Settings != null ? Settings.MaxVirusMarkAmount : 300f;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ResolveRefs();
        ResolveConfig();
        ApplyInitialState();
    }

    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        EntityPaintMarkSettings settings = Settings;

        if (settings == null || !settings.UsePaintMark)
            return;

        float previousVaccine = _vaccineMarkAmount;
        float previousVirus = _virusMarkAmount;
        PaintMarkFaction previousActiveFaction = _activeFaction;

        float dt = Time.deltaTime;

        TickDecay(settings, dt);

        if (HasMarkStateChanged(previousVaccine, previousVirus, previousActiveFaction))
            NotifyChanged();
    }

    // Purpose: Updates the stat config state.
    public void SetStatConfig(EntityStatConfigSO statConfig)
    {
        _statConfigOverride = statConfig;
        _resolvedConfig = statConfig;

        ApplyInitialState();
        NotifyChanged();
    }

    // Purpose: Adds the requested mark to this component.
    public void AddMark(PaintMarkFaction faction, float amount)
    {
        EntityPaintMarkSettings settings = Settings;

        if (settings == null ||
            !settings.UsePaintMark ||
            faction == PaintMarkFaction.None ||
            amount <= 0f)
        {
            return;
        }

        float previousVaccine = _vaccineMarkAmount;
        float previousVirus = _virusMarkAmount;
        PaintMarkFaction previousActiveFaction = _activeFaction;

        AddMarkAmount(faction, amount);

        if (HasMarkStateChanged(previousVaccine, previousVirus, previousActiveFaction))
            NotifyChanged();
    }

    /// <summary>
    /// Removes Mark from the currently active faction. This is an explicit
    /// recovery operation, so it may cross stack boundaries immediately.
    /// </summary>
    public float ReduceMark(float amount)
    {
        if (amount <= 0f || _activeFaction == PaintMarkFaction.None)
            return 0f;

        float before = CurrentMarkAmount;
        RemoveActiveAmount(amount);
        float removed = Mathf.Max(0f, before - CurrentMarkAmount);

        if (removed > 0f)
            NotifyChanged();

        return removed;
    }

    // Purpose: Processes one update step for decay.
    private void TickDecay(EntityPaintMarkSettings settings, float dt)
    {
        if (settings == null || dt <= 0f)
            return;

        if (_activeFaction == PaintMarkFaction.Vaccine)
            _vaccineMarkAmount = Mathf.Max(0f, _vaccineMarkAmount - NaturalDecayPerSecond * dt);
        else if (_activeFaction == PaintMarkFaction.Virus)
            _virusMarkAmount = Mathf.Max(0f, _virusMarkAmount - NaturalDecayPerSecond * dt);
        else
        {
            _vaccineMarkAmount = Mathf.Max(0f, _vaccineMarkAmount - NaturalDecayPerSecond * dt);
            _virusMarkAmount = Mathf.Max(0f, _virusMarkAmount - NaturalDecayPerSecond * dt);
        }

        if ((_activeFaction == PaintMarkFaction.Vaccine && _vaccineMarkAmount <= 0f) ||
            (_activeFaction == PaintMarkFaction.Virus && _virusMarkAmount <= 0f))
        {
            ClearActive();
        }
    }

    // Purpose: Adds the requested mark amount to this component.
    private void AddMarkAmount(PaintMarkFaction faction, float amount)
    {
        if (faction == PaintMarkFaction.None || amount <= 0f)
            return;

        if (_activeFaction != PaintMarkFaction.None)
        {
            if (faction == _activeFaction)
                AddActiveAmount(faction, amount);
            else
                RemoveActiveAmount(amount);

            return;
        }

        AddInactiveAmount(faction, amount);
    }

    // Purpose: Adds the requested inactive amount to this component.
    private void AddInactiveAmount(PaintMarkFaction faction, float amount)
    {
        switch (faction)
        {
            case PaintMarkFaction.Vaccine:
                _vaccineMarkAmount = Mathf.Clamp(
                    _vaccineMarkAmount + amount,
                    0f,
                    MaxVaccineMarkAmount);

                _virusMarkAmount = Mathf.Max(0f, _virusMarkAmount - amount);

                if (_vaccineMarkAmount > 0f)
                    _activeFaction = PaintMarkFaction.Vaccine;

                break;

            case PaintMarkFaction.Virus:
                _virusMarkAmount = Mathf.Clamp(
                    _virusMarkAmount + amount,
                    0f,
                    MaxVirusMarkAmount);

                _vaccineMarkAmount = Mathf.Max(0f, _vaccineMarkAmount - amount);

                if (_virusMarkAmount > 0f)
                    _activeFaction = PaintMarkFaction.Virus;

                break;
        }
    }

    // Purpose: Adds the requested active amount to this component.
    private void AddActiveAmount(PaintMarkFaction faction, float amount)
    {
        if (faction == PaintMarkFaction.Vaccine)
        {
            _vaccineMarkAmount = Mathf.Clamp(
                _vaccineMarkAmount + amount,
                0f,
                MaxVaccineMarkAmount);
        }
        else if (faction == PaintMarkFaction.Virus)
        {
            _virusMarkAmount = Mathf.Clamp(
                _virusMarkAmount + amount,
                0f,
                MaxVirusMarkAmount);
        }
    }

    // Purpose: Removes the requested active amount from this component.
    private void RemoveActiveAmount(float amount)
    {
        if (amount <= 0f)
            return;

        if (_activeFaction == PaintMarkFaction.Vaccine)
        {
            _vaccineMarkAmount = Mathf.Max(0f, _vaccineMarkAmount - amount);

            if (_vaccineMarkAmount <= 0f)
                ClearActive();
        }
        else if (_activeFaction == PaintMarkFaction.Virus)
        {
            _virusMarkAmount = Mathf.Max(0f, _virusMarkAmount - amount);

            if (_virusMarkAmount <= 0f)
                ClearActive();
        }
    }

    // Purpose: Clears the current active state.
    private void ClearActive()
    {
        _activeFaction = PaintMarkFaction.None;
        _vaccineMarkAmount = 0f;
        _virusMarkAmount = 0f;
    }

    // Purpose: Applies the requested initial state change.
    private void ApplyInitialState()
    {
        _activeFaction = PaintMarkFaction.None;
        _vaccineMarkAmount = 0f;
        _virusMarkAmount = 0f;

        EntityPaintMarkSettings settings = Settings;

        if (settings == null || !settings.UsePaintMark)
            return;

        if (!settings.StartWithVirusMark)
            return;

        _virusMarkAmount = Mathf.Clamp(
            settings.StartingVirusMarkAmount,
            0f,
            MaxVirusMarkAmount);

        if (_virusMarkAmount > 0f)
            _activeFaction = PaintMarkFaction.Virus;
    }

    // Purpose: Calculates stacks from the current inputs.
    private int CalculateStacks(PaintMarkFaction faction, float amount)
    {
        if (faction == PaintMarkFaction.None || amount <= 0f)
            return 0;

        return Mathf.Clamp(
            Mathf.FloorToInt(amount / MarkAmountPerStack),
            0,
            GetMaxStacks(faction));
    }

    // Purpose: Returns the current max stacks.
    private int GetMaxStacks(PaintMarkFaction faction)
    {
        EntityPaintMarkSettings settings = Settings;

        if (settings == null)
            return 3;

        return faction == PaintMarkFaction.Virus
            ? settings.MaxVirusMarkStacks
            : settings.MaxVaccineMarkStacks;
    }

    // Purpose: Checks whether this component has mark state changed.
    private bool HasMarkStateChanged(
        float previousVaccine,
        float previousVirus,
        PaintMarkFaction previousActiveFaction)
    {
        return !Mathf.Approximately(previousVaccine, _vaccineMarkAmount) ||
               !Mathf.Approximately(previousVirus, _virusMarkAmount) ||
               previousActiveFaction != _activeFaction;
    }

    // Purpose: Resolves the refs needed by this component.
    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);

    }

    // Purpose: Resolves the config needed by this component.
    private EntityStatConfigSO ResolveConfig()
    {
        if (_statConfigOverride != null)
            return _statConfigOverride;

        if (_resolvedConfig != null)
            return _resolvedConfig;

        ResolveRefs();

        if (_damageable != null && _damageable.StatConfig != null)
        {
            _resolvedConfig = _damageable.StatConfig;
            return _resolvedConfig;
        }

        return null;
    }

    // Purpose: Publishes the changed notification.
    private void NotifyChanged()
    {
        OnChanged?.Invoke(this);
    }
}
