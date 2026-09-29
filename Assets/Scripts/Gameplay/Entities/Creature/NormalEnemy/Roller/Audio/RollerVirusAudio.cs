/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Roller > Audio
 * Primary responsibility: Handles RollerVirusAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class RollerVirusAudio : CreatureEnemyAudio
{
    [Header("Roller Virus Audio")]
    [SerializeField] private AudioCueSO RollingSound;

    // Purpose: Plays the requested rolling action or effect.
    public void PlayRolling()
    {
        PlayAudio(RollingSound, _audioConfig, transform.position);
    }
}
