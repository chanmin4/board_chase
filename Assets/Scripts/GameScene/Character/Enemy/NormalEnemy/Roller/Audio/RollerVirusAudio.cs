using UnityEngine;

public class RollerVirusAudio : EnemyAudio
{
    [Header("Roller Virus Audio")]
    [SerializeField] private AudioCueSO RollingSound;

    public void PlayRolling()
    {
        PlayAudio(RollingSound, _audioConfig, transform.position);
    }
}
