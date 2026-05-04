using UnityEngine;

public class ChaserVirusAudio : EnemyAudio
{
    [Header("Chaser Virus Audio")]
    [SerializeField] private AudioCueSO _selfDestructCharge;
    [SerializeField] private AudioCueSO _selfDestructExplosion;

    public void PlaySelfDestructCharge()
    {
        PlayAudio(_selfDestructCharge, _audioConfig, transform.position);
    }

    public void PlaySelfDestructExplosion()
    {
        PlayAudio(_selfDestructExplosion, _audioConfig, transform.position);
    }
}
