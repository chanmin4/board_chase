using UnityEngine;

public class RollerVirusAudio : CreatureEnemyAudio
{
    [Header("Roller Virus Audio")]
    [SerializeField] private AudioCueSO RollingSound;

    public void PlayRolling()
    {
        PlayAudio(RollingSound, _audioConfig, transform.position);
    }
}
