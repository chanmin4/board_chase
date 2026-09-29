/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Defines the authored audio handoff for shooter characters.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Shared audio handoff for a shooter character. Enemy shooters normally use
/// this asset directly; player-only actions are added by PlayerShooterAudioProfileSO.
/// Weapon fire and reload cues intentionally remain on WeaponAudioProfileSO.
/// </summary>
[CreateAssetMenu(fileName = "ShooterAudioProfile", menuName = "Audio/Profiles/Shooter Audio Profile")]
public class ShooterAudioProfileSO : DescriptionBaseSO
{
    [Header("Common Shooter Cues")]
    [SerializeField] private AudioCueSO _deathCue;
    [SerializeField] private AudioCueSO _specialCue;
    [SerializeField] private AudioCueSO _dashCue;
    [SerializeField] private AudioCueSO _footstepCue;
    [SerializeField] private AudioCueSO _hurtCue;

    public AudioCueSO DeathCue => _deathCue;
    public AudioCueSO SpecialCue => _specialCue;
    public AudioCueSO DashCue => _dashCue;
    public AudioCueSO FootstepCue => _footstepCue;
    public AudioCueSO HurtCue => _hurtCue;
}

/// <summary>
/// Player-specific extension of the common shooter handoff. Assign this asset
/// to PlayerShooterCharacterAudio through its inherited Audio Profile field.
/// </summary>
[CreateAssetMenu(fileName = "PlayerShooterAudioProfile", menuName = "Audio/Profiles/Player Shooter Audio Profile")]
public class PlayerShooterAudioProfileSO : ShooterAudioProfileSO
{
    [Header("Player-only Cues")]
    [SerializeField] private AudioCueSO _aimCue;
    [SerializeField] private AudioCueSO _hitConfirmFallbackCue;
    [SerializeField] private AudioCueSO _pickupCue;
    [SerializeField] private AudioCueSO _currencyPickupCue;

    [Header("Player World Actions")]
    [SerializeField] private AudioCueSO _genericInteractCue;
    [SerializeField] private AudioCueSO _portalInteractCue;
    [SerializeField] private AudioCueSO _corpseInteractCue;
    [SerializeField] private AudioCueSO _treasurePickupCue;

    public AudioCueSO AimCue => _aimCue;
    public AudioCueSO HitConfirmFallbackCue => _hitConfirmFallbackCue;
    public AudioCueSO PickupCue => _pickupCue;
    public AudioCueSO CurrencyPickupCue => _currencyPickupCue;
    public AudioCueSO GenericInteractCue => _genericInteractCue;
    public AudioCueSO PortalInteractCue => _portalInteractCue;
    public AudioCueSO CorpseInteractCue => _corpseInteractCue;
    public AudioCueSO TreasurePickupCue => _treasurePickupCue;
}
