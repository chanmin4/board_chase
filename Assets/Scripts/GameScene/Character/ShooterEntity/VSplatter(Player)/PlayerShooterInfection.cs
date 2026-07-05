using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerShooterInfection : ShooterInfection
{
    [Header("Difficulty")]
    [SerializeField] private DifficultyRulesSO _difficultyRules;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerHealthEventChannelSO _playerHealthChanged;

    [FormerlySerializedAs("_deathEvent")]
    [SerializeField] private VoidEventChannelSO _gameOverEvent;

    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;
    protected override float InfectionGainMultiplier =>
        ActiveDifficultyRules != null ? ActiveDifficultyRules.PlayerInfectionGainMultiplier : 1f;

    protected override float InfectionRecoverMultiplier =>
        ActiveDifficultyRules != null ? ActiveDifficultyRules.PlayerInfectionRecoverMultiplier : 1f;

    private DifficultyRulesSO ActiveDifficultyRules =>
        _difficultyRules != null ? _difficultyRules : DifficultyRuntime.CurrentRules;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.RaiseEvent(this);

    }

    protected override void OnDisable()
    {
        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.Clear(this);
 
        base.OnDisable();
    }

    public override void PublishCurrentSnapshot()
    {
        base.PublishCurrentSnapshot();

        if (_playerHealthChanged == null)
            return;

        _playerHealthChanged.RaiseEvent(new PlayerHealthSnapshot(
            MaxHealth,
            CurrentHealth,
            CurrentInfection,
            IsDead));
    }

    protected override void OnBecameDead(bool killedByInfection)
    {
        if (_gameOverEvent != null)
            _gameOverEvent.RaiseEvent();
    }

    public override void RecoverOnSectorCaptured()
    {
    }

    public override void RecoverOnNamedKilled()
    {
    }

    public override void RecoverOnBossKilled()
    {
    }
}