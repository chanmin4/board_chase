/*
 * Role hierarchy: Gameplay > Entities > Creature > Audio
 * Primary responsibility: Handles CreatureEnemyAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class CreatureEnemyAudio : CharacterAudio
{
    [Header("Common Enemy Audio")]
    [SerializeField] private AudioCueSO _idle;
    [SerializeField] private AudioCueSO _move;
    [SerializeField] private AudioCueSO _attack;
    [SerializeField] private AudioCueSO _getHit;
    [SerializeField] private AudioCueSO _die;

    // Purpose: Plays the requested idle action or effect.
    public virtual void PlayIdle() => PlayAudio(_idle, _audioConfig, transform.position);
    // Purpose: Plays the requested move action or effect.
    public virtual void PlayMove() => PlayAudio(_move, _audioConfig, transform.position);
    // Purpose: Plays the requested attack action or effect.
    public virtual void PlayAttack() => PlayAudio(_attack, _audioConfig, transform.position);
    // Purpose: Plays the requested get hit action or effect.
    public virtual void PlayGetHit() => PlayAudio(_getHit, _audioConfig, transform.position);
    // Purpose: Plays the requested die action or effect.
    public virtual void PlayDie() => PlayAudio(_die, _audioConfig, transform.position);
}
