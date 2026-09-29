/*
 * Role hierarchy: Gameplay > Entities > Creature > NormalEnemy > Chaser > Audio
 * Primary responsibility: Handles ChaserVirusAudio audio playback and sound-stimulus delivery.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class ChaserVirusAudio : CreatureEnemyAudio
{
    [Header("Chaser Virus Audio")]
    [SerializeField] private AudioCueSO _selfDestructCharge;
    [SerializeField] private AudioCueSO _selfDestructExplosion;

    // Purpose: Plays the requested self destruct charge action or effect.
    public void PlaySelfDestructCharge()
    {
        PlayAudio(_selfDestructCharge, _audioConfig, transform.position);
    }

    // Purpose: Plays the requested self destruct explosion action or effect.
    public void PlaySelfDestructExplosion()
    {
        PlayAudio(_selfDestructExplosion, _audioConfig, transform.position);
    }
}
