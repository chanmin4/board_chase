using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class PlayerCurrencyRuntime : MonoBehaviour
{
    [Header("Player Stats Config")]
    [FormerlySerializedAs("_baseStatsConfig")]
    [SerializeField] private PlayerStatsConfigSO _playerStatsConfig;

    [Header("Need Ref - Events")]
    [SerializeField] private PlayerCurrencyChangedEventChannelSO _currencyChangedChannel;
    [SerializeField] private PlayerCurrencyRuntimeReadyEventChannelSO _currencyRuntimeReadyChannel;
    [SerializeField] private VoidEventChannelSO _requestCurrencySnapshotChannel;

    [Header("Optional - Save Hook")]
    [Tooltip("Used only for loading/saving roguelike currency. Run currency is not persisted.")]
    [SerializeField] private SaveSystem _saveSystem;

    [Tooltip("If true, reads roguelike currency from SaveSystem on enable.")]
    [SerializeField] private bool _loadRoguelikeCurrencyFromSaveOnEnable = true;

    [Header("Runtime")]
    [SerializeField] private int _roguelikeCurrency;
    [SerializeField] private int _runCurrency;

    public int RoguelikeCurrency => _roguelikeCurrency;
    public int RunCurrency => _runCurrency;

    private void Awake()
    {
        InitializeFromStatsConfig();
    }

    private void OnEnable()
    {
        if (_requestCurrencySnapshotChannel != null)
            _requestCurrencySnapshotChannel.OnEventRaised += PublishSnapshot;

        if (_loadRoguelikeCurrencyFromSaveOnEnable)
            LoadRoguelikeCurrencyFromSaveSystem();

        PublishSnapshot();

        if (_currencyRuntimeReadyChannel != null)
            _currencyRuntimeReadyChannel.RaiseEvent(this);
    }

    private void OnDisable()
    {
        if (_requestCurrencySnapshotChannel != null)
            _requestCurrencySnapshotChannel.OnEventRaised -= PublishSnapshot;

        if (_currencyRuntimeReadyChannel != null)
            _currencyRuntimeReadyChannel.Clear(this);
    }

    public void ResetRunCurrencyForNewRun()
    {
        _runCurrency = _playerStatsConfig != null
            ? _playerStatsConfig.StartingRunCurrency
            : 0;

        PublishSnapshot();
    }

    public void LoadRoguelikeCurrencyFromSaveSystem()
    {
        if (_saveSystem == null || _saveSystem.saveData == null)
            return;

        _roguelikeCurrency = Mathf.Max(0, _saveSystem.saveData._roguelikeCurrency);
        PublishSnapshot();
    }

    public void WriteRoguelikeCurrencyToSaveSystem(bool saveToDisk)
    {
        if (_saveSystem == null || _saveSystem.saveData == null)
            return;

        _saveSystem.saveData._roguelikeCurrency = Mathf.Max(0, _roguelikeCurrency);

        if (saveToDisk)
            _saveSystem.SaveDataToDisk();
    }

    public bool AddCurrency(PlayerCurrencyType type, int amount)
    {
        if (amount <= 0)
            return false;

        switch (type)
        {
            case PlayerCurrencyType.Roguelike:
                _roguelikeCurrency += amount;
                break;

            case PlayerCurrencyType.Run:
                _runCurrency += amount;
                break;
        }

        PublishSnapshot();
        return true;
    }

    public bool CanSpend(PlayerCurrencyType type, int amount)
    {
        if (amount <= 0)
            return true;

        return GetCurrency(type) >= amount;
    }

    public bool TrySpendCurrency(PlayerCurrencyType type, int amount)
    {
        if (amount <= 0)
            return true;

        if (!CanSpend(type, amount))
            return false;

        switch (type)
        {
            case PlayerCurrencyType.Roguelike:
                _roguelikeCurrency -= amount;
                break;

            case PlayerCurrencyType.Run:
                _runCurrency -= amount;
                break;
        }

        PublishSnapshot();
        return true;
    }

    public int GetCurrency(PlayerCurrencyType type)
    {
        return type == PlayerCurrencyType.Roguelike
            ? _roguelikeCurrency
            : _runCurrency;
    }

    public void SetCurrency(PlayerCurrencyType type, int amount)
    {
        amount = Mathf.Max(0, amount);

        switch (type)
        {
            case PlayerCurrencyType.Roguelike:
                _roguelikeCurrency = amount;
                break;

            case PlayerCurrencyType.Run:
                _runCurrency = amount;
                break;
        }

        PublishSnapshot();
    }

    private void InitializeFromStatsConfig()
    {
        _runCurrency = _playerStatsConfig != null
            ? _playerStatsConfig.StartingRunCurrency
            : 0;

        _roguelikeCurrency = _playerStatsConfig != null
            ? _playerStatsConfig.StartingRoguelikeCurrencyForNewSave
            : 0;
    }

    private void PublishSnapshot()
    {
        if (_currencyChangedChannel == null)
            return;

        _currencyChangedChannel.RaiseEvent(
            new PlayerCurrencySnapshot(_roguelikeCurrency, _runCurrency));
    }
}