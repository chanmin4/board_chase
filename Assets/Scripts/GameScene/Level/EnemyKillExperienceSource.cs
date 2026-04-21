using UnityEngine;

[DisallowMultipleComponent]
public class EnemyKillExperienceSource : MonoBehaviour
{
    [Tooltip("적에게 부착")]
    [Header("Refs")]
    [SerializeField] private Damageable _damageable;

    [Header("Reward")]
    [SerializeField] private EnemyExperienceRewardSO _reward;

    [Header("Broadcasting On")]
    [SerializeField] private PlayerExperienceGainEventChannelSO _xpGainChannel;

    private void Reset()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();
    }

    private void Awake()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        if (_damageable != null)
            _damageable.OnDie += OnDie;
    }

    private void OnDisable()
    {
        if (_damageable != null)
            _damageable.OnDie -= OnDie;
    }

    private void OnDie()
    {
        if (_xpGainChannel == null || _reward == null)
            return;

        float xp = _reward.XpOnDeath;

        if (xp <= 0f)
            return;

        _xpGainChannel.RaiseEvent(new PlayerExperienceGain(
            xp,
            PlayerExperienceSource.EnemyKill,
            transform.position,
            gameObject));
    }
}
