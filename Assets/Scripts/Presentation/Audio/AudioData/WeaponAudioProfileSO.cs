/*
 * Role hierarchy: Presentation > Audio > AudioData
 * Primary responsibility: Defines the authored audio handoff for one weapon.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

/// <summary>
/// Type-safe sound handoff for a weapon asset. One profile may be shared by
/// weapons with identical sound behaviour, or authored per unique weapon.
/// Each field references an AudioCueSO, which remains the owner of clips and
/// default playback settings.
/// </summary>
[CreateAssetMenu(fileName = "WeaponAudioProfile", menuName = "Audio/Profiles/Weapon Audio Profile")]
public class WeaponAudioProfileSO : DescriptionBaseSO
{
    [Header("Ranged Weapon")]
    [Tooltip("Played for a valid firearm shot at the weapon owner's world position.")]
    [SerializeField] private AudioCueSO _fireCue;

    [Tooltip("Played for a dry-fire attempt.")]
    [SerializeField] private AudioCueSO _emptyFireCue;

    [Tooltip("Local player confirmation played when this weapon damages a target.")]
    [SerializeField] private AudioCueSO _hitConfirmCue;

    [Tooltip("Reload cue used by a player holding this firearm.")]
    [SerializeField] private AudioCueSO _playerReloadCue;

    [Tooltip("Reload cue used by an enemy holding this firearm.")]
    [SerializeField] private AudioCueSO _enemyReloadCue;

    [Header("Melee Weapon")]
    [Tooltip("Played when a melee swing begins, whether or not it hits a target.")]
    [SerializeField] private AudioCueSO _meleeSwingCue;

    [Header("Throwable")]
    [Tooltip("Played when the throwable leaves the owner's hand.")]
    [SerializeField] private AudioCueSO _throwCue;

    [Tooltip("Played at the throwable explosion position.")]
    [SerializeField] private AudioCueSO _explosionCue;

    public AudioCueSO FireCue => _fireCue;
    public AudioCueSO EmptyFireCue => _emptyFireCue;
    public AudioCueSO HitConfirmCue => _hitConfirmCue;
    public AudioCueSO PlayerReloadCue => _playerReloadCue;
    public AudioCueSO EnemyReloadCue => _enemyReloadCue;
    public AudioCueSO MeleeSwingCue => _meleeSwingCue;
    public AudioCueSO ThrowCue => _throwCue;
    public AudioCueSO ExplosionCue => _explosionCue;
}
