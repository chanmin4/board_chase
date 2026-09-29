/*
 * Role hierarchy: Gameplay > Entities > Shooter > Common > Core
 * Primary responsibility: Handles EntityShooterCharacterAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Serialization;

public class EntityShooterCharacterAudio : CharacterAudio
{
    [Header("Shooter")]
    [SerializeField] private EntityShootCore _shootCore;
    [FormerlySerializedAs("_weaponHolder")]
    [SerializeField] private ShooterEquipmentController _equipmentController;

    [Header("Damage")]
    [SerializeField] private Damageable _damageable;

    [Header("Audio Cues")]
    [SerializeField] private AudioCueSO _dieCue;
    [SerializeField] private AudioCueSO _specialCue;

    [Header("Audio Profile")]
    [Tooltip("Optional authored shooter handoff. Filled profile cues override the matching legacy fields below.")]
    [SerializeField] private ShooterAudioProfileSO _audioProfile;

    [Header("Optional Event Audio Configs")]
    [Tooltip("Optional world-audio configuration used only for gunshots. Leave empty on the Player to keep using the listener-centered Player SFX configuration.")]
    [SerializeField] private AudioConfigurationSO _gunshotAudioConfig;

    protected WeaponSO CurrentWeapon => _equipmentController != null ? _equipmentController.CurrentWeapon : null;
    protected Damageable Damageable => _damageable;
    protected ShooterAudioProfileSO AudioProfile => _audioProfile;

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        ResolveRefs();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        ResolveRefs();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    protected virtual void OnEnable()
    {
        ResolveRefs();

        if (_shootCore != null)
            _shootCore.Fired += PlayShoot;

        if (_damageable != null)
            _damageable.OnDie += PlayDie;

        SubscribeAdditionalEvents();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    protected virtual void OnDisable()
    {
        if (_shootCore != null)
            _shootCore.Fired -= PlayShoot;

        if (_damageable != null)
            _damageable.OnDie -= PlayDie;

        UnsubscribeAdditionalEvents();
    }

    // Purpose: Resolves the refs needed by this component.
    protected virtual void ResolveRefs()
    {
        if (_shootCore == null)
            _shootCore = EntityComponentResolver.Resolve<EntityShootCore>(this);

        if (_equipmentController == null)
            _equipmentController = EntityComponentResolver.Resolve<ShooterEquipmentController>(this);

        if (_damageable == null)
            _damageable = EntityComponentResolver.Resolve<Damageable>(this);
    }

    // Purpose: Subscribes to additional events notifications.
    protected virtual void SubscribeAdditionalEvents()
    {
    }

    // Purpose: Removes the additional events notification subscription.
    protected virtual void UnsubscribeAdditionalEvents()
    {
    }

    // Purpose: Plays the requested shoot action or effect.
    public virtual void PlayShoot(BulletAmmoType ammoType)
    {
        PlayShoot();
    }

    // Purpose: Plays the requested shoot action or effect.
    public virtual void PlayShoot()
    {
        RangedWeaponSO weapon = CurrentWeapon as RangedWeaponSO;
        AudioCueSO cue = weapon != null ? weapon.FireAudioCue : null;
        PlayAudio(cue, _gunshotAudioConfig != null ? _gunshotAudioConfig : _audioConfig, transform.position);
    }

    // Purpose: Plays the requested empty shoot action or effect.
    public virtual void PlayEmptyShoot()
    {
        RangedWeaponSO weapon = CurrentWeapon as RangedWeaponSO;
        AudioCueSO cue = weapon != null ? weapon.EmptyShootAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested player reload action or effect.
    public virtual void PlayPlayerReload()
    {
        RangedWeaponSO weapon = CurrentWeapon as RangedWeaponSO;
        AudioCueSO cue = weapon != null ? weapon.PlayerReloadAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested enemy reload action or effect.
    public virtual void PlayEnemyReload()
    {
        RangedWeaponSO weapon = CurrentWeapon as RangedWeaponSO;
        AudioCueSO cue = weapon != null ? weapon.EnemyReloadAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested special action or effect.
    public virtual void PlaySpecial()
    {
        AudioCueSO cue = AudioProfile != null && AudioProfile.SpecialCue != null
            ? AudioProfile.SpecialCue
            : _specialCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested melee swing action or effect.
    public virtual void PlayMeleeSwing(MeleeWeaponSO weapon)
    {
        AudioCueSO cue = weapon != null ? weapon.SwingAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested melee swing action or effect.
    public virtual void PlayMeleeSwing(AudioCueSO cue)
    {
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested throwable throw action or effect.
    public virtual void PlayThrowableThrow(ThrowableUsableItemSO weapon)
    {
        AudioCueSO cue = weapon != null ? weapon.ThrowAudioCue : null;
        PlayAudio(cue, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested throwable explode action or effect.
    public virtual void PlayThrowableExplode(ThrowableUsableItemSO weapon, Vector3 position)
    {
        AudioCueSO cue = weapon != null ? weapon.ExplosionAudioCue : null;
        PlayAudio(cue, _audioConfig, position);
    }

    // Purpose: Plays the requested die action or effect.
    public virtual void PlayDie()
    {
        if (Damageable != null && !Damageable.IsDead)
            return;

        AudioCueSO cue = AudioProfile != null && AudioProfile.DeathCue != null
            ? AudioProfile.DeathCue
            : _dieCue;
        PlayAudio(cue, _audioConfig, transform.position);
    }
}
