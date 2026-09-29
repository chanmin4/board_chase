/*
 * Role hierarchy: Gameplay > Entities > Shooter > Player
 * Primary responsibility: Handles PlayerShooterInfection combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerShooterInfection : ShooterInfection
{
    protected override PaintFaction FriendlyPaintFaction => PaintFaction.Vaccine;
    [Header("Difficulty")]
    [SerializeField] private DifficultyRulesSO _difficultyRules;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;

    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;
    [SerializeField] private PlayerInventoryRuntime _inventoryRuntime;
    protected override float InfectionGainMultiplier =>
        ActiveDifficultyRules != null ? ActiveDifficultyRules.PlayerInfectionGainMultiplier : 1f;

    // Player Infection is a run resource: gas masks only reduce incoming gas,
    // while InfectionCureItemSO is the only standard way to lower this value.
    protected override bool AllowsNaturalRecovery => false;

    private float _biochemicalGasTickElapsed;

    private DifficultyRulesSO ActiveDifficultyRules =>
        _difficultyRules != null ? _difficultyRules : DifficultyRuntime.CurrentRules;

    /// <summary>
    /// The current room-gas Infection rate after the equipped gas mask has
    /// absorbed its share. This intentionally excludes natural recovery: HUD
    /// feedback should answer "what is this room doing to me right now?".
    /// </summary>
    public float CurrentBiochemicalGasInfectionPerSecond
    {
        get
        {
            float rawRate = SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasInfectionPerSecond;

            if (rawRate <= 0f)
                return 0f;

            if (_inventoryRuntime == null)
                _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);

            return _inventoryRuntime != null
                ? _inventoryRuntime.PreviewBiochemicalGasExposure(rawRate)
                : rawRate;
        }
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected override void OnEnable()
    {
        base.OnEnable();

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.RaiseEvent(this);

    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected override void OnDisable()
    {
        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.Clear(this);
 
        base.OnDisable();
    }

    // Purpose: Processes this component's per-frame runtime work.
    protected override void Update()
    {
        base.Update();

        // Room biochemical gas is deliberately player-only. Enemy shooters do
        // not read SectorRoomTraitRuntime's player rate and never enter this path.
        float infectionPerSecond =
            SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasInfectionPerSecond;

        if (infectionPerSecond <= 0f || IsDead)
        {
            _biochemicalGasTickElapsed = 0f;
            return;
        }

        float tickInterval =
            SectorRoomTraitRuntime.CurrentPlayerBiochemicalGasTickInterval;
        _biochemicalGasTickElapsed += Time.deltaTime;

        while (_biochemicalGasTickElapsed >= tickInterval)
        {
            _biochemicalGasTickElapsed -= tickInterval;
            float rawExposure = infectionPerSecond * tickInterval;
            if (_inventoryRuntime == null)
                _inventoryRuntime = EntityComponentResolver.Resolve<PlayerInventoryRuntime>(this);

            float appliedExposure = _inventoryRuntime != null
                ? _inventoryRuntime.ResolveBiochemicalGasExposure(rawExposure, out _)
                : rawExposure;

            AddInfection(appliedExposure);

            if (IsDead)
                break;
        }
    }

    // Purpose: Publishes the current snapshot notification.
    public override void PublishCurrentSnapshot()
    {
        base.PublishCurrentSnapshot();

        if (_playerHealthChanged == null)
            return;

        _playerHealthChanged.RaiseEvent(new PlayerHealthSnapshot(
            MaxHealth,
            CurrentHealth,
            CurrentInfection,
            NaturalRecoveryPerSecond,
            IsDead));
    }

    // Purpose: Performs the recover on sector captured workflow for this component.
    public override void RecoverOnSectorCaptured()
    {
    }

    // Purpose: Performs the recover on named killed workflow for this component.
    public override void RecoverOnNamedKilled()
    {
    }

    // Purpose: Performs the recover on boss killed workflow for this component.
    public override void RecoverOnBossKilled()
    {
    }
}
