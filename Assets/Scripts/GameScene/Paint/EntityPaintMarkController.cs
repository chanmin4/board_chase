using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EntityPaintMarkController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private EntityStatConfigSO _statConfigOverride;
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    private EntityStatConfigSO _resolvedConfig;
    private MaskRenderManager _maskRenderManager;

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

    private void Awake()
    {
        ResolveRefs();
        ResolveConfig();
        ApplyInitialState();
    }

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }

        ApplyDamageTakenAdditive();
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;

        if (_damageable != null)
            _damageable.ResetPaintMarkDamageTakenAdditive();
    }

    private void Update()
    {
        EntityPaintMarkSettings settings = Settings;

        if (settings == null || !settings.UsePaintMark)
            return;

        float previousVaccine = _vaccineMarkAmount;
        float previousVirus = _virusMarkAmount;
        PaintMarkFaction previousActiveFaction = _activeFaction;

        float dt = Time.deltaTime;

        TickSurfaceMark(settings, dt);
        TickDecay(settings, dt);
        ApplyDamageTakenAdditive();

        if (HasMarkStateChanged(previousVaccine, previousVirus, previousActiveFaction))
            NotifyChanged();
    }

    public void SetStatConfig(EntityStatConfigSO statConfig)
    {
        _statConfigOverride = statConfig;
        _resolvedConfig = statConfig;

        ApplyInitialState();
        ApplyDamageTakenAdditive();
        NotifyChanged();
    }

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
        ApplyDamageTakenAdditive();

        if (HasMarkStateChanged(previousVaccine, previousVirus, previousActiveFaction))
            NotifyChanged();
    }

    public void AddVirusMarkFromDamagingPlayer()
    {
        EntityPaintMarkSettings settings = Settings;

        if (settings == null || !settings.UsePaintMark)
            return;

        AddMark(PaintMarkFaction.Virus, settings.VirusMarkAmountOnDamagingPlayer);
    }

    private void TickSurfaceMark(EntityPaintMarkSettings settings, float dt)
    {
        MaskRenderManager manager = ResolveMaskRenderManager();

        if (manager == null)
            return;

        if (!manager.TryGetStateAtWorld(transform.position, out PaintSurfaceState state, true))
            return;

        PaintMarkFaction surfaceFaction = PaintTypeUtility.ToMarkFaction(state);

        if (surfaceFaction == PaintMarkFaction.None)
            return;

        float tickScale = CalculateTickScale(settings, dt);

        if (tickScale <= 0f)
            return;

        if (_activeFaction != PaintMarkFaction.None &&
            surfaceFaction != _activeFaction)
        {
            RemoveActiveAmount(settings.OppositePaintFastDecayAmountPerTick * tickScale);
            return;
        }

        float amount =
            surfaceFaction == PaintMarkFaction.Vaccine
                ? settings.VaccinePaintMarkAmountPerTick * tickScale
                : settings.VirusPaintMarkAmountPerTick * tickScale;

        AddMarkAmount(surfaceFaction, amount);
    }

    private void TickDecay(EntityPaintMarkSettings settings, float dt)
    {
        float tickScale = CalculateTickScale(settings, dt);

        if (tickScale <= 0f)
            return;

        if (_activeFaction == PaintMarkFaction.None)
        {
            float decay = settings.InactiveSlowDecayAmountPerTick * tickScale;

            if (decay <= 0f)
                return;

            _vaccineMarkAmount = Mathf.Max(0f, _vaccineMarkAmount - decay);
            _virusMarkAmount = Mathf.Max(0f, _virusMarkAmount - decay);
            return;
        }

        RemoveActiveAmount(settings.ActiveSlowDecayAmountPerTick * tickScale);
    }

    private float CalculateTickScale(EntityPaintMarkSettings settings, float dt)
    {
        if (settings == null || dt <= 0f)
            return 0f;

        return dt / settings.PaintMarkTickInterval;
    }

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

                if (_vaccineMarkAmount >= MaxVaccineMarkAmount)
                    Activate(PaintMarkFaction.Vaccine);

                break;

            case PaintMarkFaction.Virus:
                _virusMarkAmount = Mathf.Clamp(
                    _virusMarkAmount + amount,
                    0f,
                    MaxVirusMarkAmount);

                _vaccineMarkAmount = Mathf.Max(0f, _vaccineMarkAmount - amount);

                if (_virusMarkAmount >= MaxVirusMarkAmount)
                    Activate(PaintMarkFaction.Virus);

                break;
        }
    }

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

    private void Activate(PaintMarkFaction faction)
    {
        _activeFaction = faction;

        if (faction == PaintMarkFaction.Vaccine)
        {
            _vaccineMarkAmount = MaxVaccineMarkAmount;
            _virusMarkAmount = 0f;
        }
        else if (faction == PaintMarkFaction.Virus)
        {
            _virusMarkAmount = MaxVirusMarkAmount;
            _vaccineMarkAmount = 0f;
        }
    }

    private void ClearActive()
    {
        _activeFaction = PaintMarkFaction.None;
        _vaccineMarkAmount = 0f;
        _virusMarkAmount = 0f;
    }

    private void ApplyDamageTakenAdditive()
    {
        if (_damageable == null)
            return;

        EntityPaintMarkSettings settings = Settings;
        float additive = 0f;

        if (settings != null && settings.UsePaintMark)
        {
            int stacks = ActiveStacks;

            if (_activeFaction == PaintMarkFaction.Vaccine)
                additive = stacks * settings.VaccineDamageTakenAddPerStack;
            else if (_activeFaction == PaintMarkFaction.Virus)
                additive = stacks * settings.VirusDamageTakenAddPerStack;
        }

        _damageable.SetPaintMarkDamageTakenAdditive(additive);
    }

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

        if (_virusMarkAmount >= MaxVirusMarkAmount)
            Activate(PaintMarkFaction.Virus);
    }

    private int CalculateStacks(PaintMarkFaction faction, float amount)
    {
        if (faction == PaintMarkFaction.None || amount <= 0f)
            return 0;

        return Mathf.Clamp(
            Mathf.FloorToInt(amount / MarkAmountPerStack),
            0,
            GetMaxStacks(faction));
    }

    private int GetMaxStacks(PaintMarkFaction faction)
    {
        EntityPaintMarkSettings settings = Settings;

        if (settings == null)
            return 3;

        return faction == PaintMarkFaction.Virus
            ? settings.MaxVirusMarkStacks
            : settings.MaxVaccineMarkStacks;
    }

    private bool HasMarkStateChanged(
        float previousVaccine,
        float previousVirus,
        PaintMarkFaction previousActiveFaction)
    {
        return !Mathf.Approximately(previousVaccine, _vaccineMarkAmount) ||
               !Mathf.Approximately(previousVirus, _virusMarkAmount) ||
               previousActiveFaction != _activeFaction;
    }

    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ??
                          GetComponentInParent<Damageable>();

    }

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

    private MaskRenderManager ResolveMaskRenderManager()
    {
        if (_maskRenderManager != null)
            return _maskRenderManager;

        if (_maskRenderManagerReadyChannel != null &&
            _maskRenderManagerReadyChannel.Current != null)
        {
            _maskRenderManager = _maskRenderManagerReadyChannel.Current;
        }

        if (_maskRenderManager == null)
            _maskRenderManager = FindAnyObjectByType<MaskRenderManager>();

        return _maskRenderManager;
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    private void NotifyChanged()
    {
        OnChanged?.Invoke(this);
    }
}