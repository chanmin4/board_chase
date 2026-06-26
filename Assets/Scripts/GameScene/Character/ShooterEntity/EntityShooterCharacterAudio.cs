using UnityEngine;

public class EntityShooterCharacterAudio : CharacterAudio
{
    [Header("Shooter")]
    [SerializeField] private EntityShootCore _shootCore;
    [SerializeField] private EntityWeaponHolder _weaponHolder;

    [Header("Damage")]
    [SerializeField] private Damageable _damageable;

    [Header("Audio Cues")]
    [SerializeField] private AudioCueSO _dieCue;
    [SerializeField] private AudioCueSO _specialCue;

    protected WeaponSO CurrentWeapon => _weaponHolder != null ? _weaponHolder.CurrentWeapon : null;
    protected Damageable Damageable => _damageable;

    protected virtual void Reset()
    {
        ResolveRefs();
    }

    protected virtual void Awake()
    {
        ResolveRefs();
    }

    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_shootCore != null)
            _shootCore.Fired += PlayShoot;

        if (_damageable != null)
            _damageable.OnDie += PlayDie;

        SubscribeAdditionalEvents();
    }

    protected virtual void OnDisable()
    {
        if (_shootCore != null)
            _shootCore.Fired -= PlayShoot;

        if (_damageable != null)
            _damageable.OnDie -= PlayDie;

        UnsubscribeAdditionalEvents();
    }

    protected virtual void ResolveRefs()
    {
        if (_shootCore == null)
        {
            _shootCore =
                GetComponent<EntityShootCore>() ??
                GetComponentInParent<EntityShootCore>() ??
                GetComponentInChildren<EntityShootCore>(true);
        }

        if (_weaponHolder == null)
        {
            _weaponHolder =
                GetComponent<EntityWeaponHolder>() ??
                GetComponentInParent<EntityWeaponHolder>() ??
                GetComponentInChildren<EntityWeaponHolder>(true);
        }

        if (_damageable == null)
        {
            _damageable =
                GetComponent<Damageable>() ??
                GetComponentInParent<Damageable>() ??
                GetComponentInChildren<Damageable>(true);
        }
    }

    protected virtual void SubscribeAdditionalEvents()
    {
    }

    protected virtual void UnsubscribeAdditionalEvents()
    {
    }

    public virtual void PlayShoot(BulletAmmoType ammoType)
    {
        PlayShoot();
    }

    public virtual void PlayShoot()
    {
        AudioCueSO cue = CurrentWeapon != null ? CurrentWeapon.FireAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    public virtual void PlayPlayerReload()
    {
        AudioCueSO cue = CurrentWeapon != null ? CurrentWeapon.PlayerReloadAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    public virtual void PlayEnemyReload()
    {
        AudioCueSO cue = CurrentWeapon != null ? CurrentWeapon.EnemyReloadAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    public virtual void PlaySpecial()
    {
        PlayAudio(_specialCue, _audioConfig, transform.position);
    }

    public virtual void PlayDie()
    {
        PlayAudio(_dieCue, _audioConfig, transform.position);
    }
}