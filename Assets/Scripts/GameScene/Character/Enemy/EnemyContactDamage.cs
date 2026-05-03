using UnityEngine;

// contact damagebox manager
public class EnemyContactDamageManager : MonoBehaviour
{
    [Header("Hitboxes")]
    [SerializeField] private EnemyContactAttack[] _hitboxes;

    [Header("Settings")]
    [SerializeField] private EnemyContactDamageSettingsSO _settings;

    private float _nextHitTime;

    private void Reset()
    {
        if (_hitboxes == null || _hitboxes.Length == 0)
            _hitboxes = GetComponentsInChildren<EnemyContactAttack>(true);
    }

    private void Awake()
    {
        RegisterHitboxes();
    }

    private void RegisterHitboxes()
    {
        if (_hitboxes == null)
            return;

        for (int i = 0; i < _hitboxes.Length; i++)
        {
            if (_hitboxes[i] != null)
                _hitboxes[i].Initialize(this);
        }
    }

    public void TryDamage(Collider other)
    {
        if (_settings == null)
            return;

        if (Time.time < _nextHitTime)
            return;

        VSplatter_Character playerCharacter = other.GetComponentInParent<VSplatter_Character>();
        if (playerCharacter == null)
            return;

        Damageable damageable = playerCharacter.GetComponent<Damageable>();
        if (damageable == null)
            return;

        if (damageable.gameObject == gameObject)
            return;

        if (!damageable.CanReceiveDamage)
            return;

        PlayerInfection playerInfection = playerCharacter.GetComponent<PlayerInfection>();

        bool appliedAny = false;

        if (_settings.HealthDamage > 0f)
        {
            damageable.ReceiveAnAttack(_settings.HealthDamage, gameObject);
            appliedAny = true;
        }

        if (playerInfection != null && _settings.InfectionDamage > 0f)
        {
            playerInfection.AddInfection(_settings.InfectionDamage);
            appliedAny = true;
        }

        if (appliedAny)
            _nextHitTime = Time.time + _settings.HitCooldown;
    }
}
