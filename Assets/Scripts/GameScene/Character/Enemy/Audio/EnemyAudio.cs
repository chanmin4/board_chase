using UnityEngine;

public class EnemyAudio : CharacterAudio
{
    [Header("Common Enemy Audio")]
    [SerializeField] private AudioCueSO _idle;
    [SerializeField] private AudioCueSO _move;
    [SerializeField] private AudioCueSO _attack;
    [SerializeField] private AudioCueSO _getHit;
    [SerializeField] private AudioCueSO _die;

    public virtual void PlayIdle() => PlayAudio(_idle, _audioConfig, transform.position);
    public virtual void PlayMove() => PlayAudio(_move, _audioConfig, transform.position);
    public virtual void PlayAttack() => PlayAudio(_attack, _audioConfig, transform.position);
    public virtual void PlayGetHit() => PlayAudio(_getHit, _audioConfig, transform.position);
    public virtual void PlayDie() => PlayAudio(_die, _audioConfig, transform.position);
}
