using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealthStatsBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private PlayerStatsRuntime _statsRuntime;

    [Header("Listening")]
    [SerializeField] private PlayerStatsChangedEventChannelSO _statsChangedChannel;

    [Header("Options")]
    [SerializeField] private bool _healToFullOnFirstApply = true;

    private bool _appliedOnce;

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();
    }

    private void Awake()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();

        if (_statsRuntime == null)
            _statsRuntime = GetComponent<PlayerStatsRuntime>();
    }

    private void OnEnable()
    {
        if (_statsChangedChannel != null)
            _statsChangedChannel.OnEventRaised += HandleStatsChanged;

        if (_statsRuntime != null)
            Apply(_statsRuntime.Current);
    }

    private void OnDisable()
    {
        if (_statsChangedChannel != null)
            _statsChangedChannel.OnEventRaised -= HandleStatsChanged;
    }

    private void HandleStatsChanged(PlayerStatsSnapshot snapshot)
    {
        Apply(snapshot);
    }

    private void Apply(PlayerStatsSnapshot snapshot)
    {
        if (_damageable == null)
            return;

        if (snapshot.survival.maxHealth <= 0f)
            return;

        bool healToFull = !_appliedOnce && _healToFullOnFirstApply;
        _damageable.ApplyMaxHealthFromStats(snapshot.survival.maxHealth, healToFull);

        _appliedOnce = true;
    }
}
