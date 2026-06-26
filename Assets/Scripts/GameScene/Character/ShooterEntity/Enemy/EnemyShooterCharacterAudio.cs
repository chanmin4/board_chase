using UnityEngine;

public class EnemyShooterCharacterAudio : EntityShooterCharacterAudio
{
    [Header("Enemy Shooter")]
    [SerializeField] private EnemyShooterShoot _enemyShoot;

    [Header("Options")]
    [SerializeField] private bool _playEnemyReloadAudio = false;

    protected override void ResolveRefs()
    {
        base.ResolveRefs();

        if (_enemyShoot == null)
            _enemyShoot = GetComponent<EnemyShooterShoot>();
    }

    protected override void SubscribeAdditionalEvents()
    {
        if (_playEnemyReloadAudio && _enemyShoot != null)
            _enemyShoot.ReloadStarted += PlayEnemyReload;
    }

    protected override void UnsubscribeAdditionalEvents()
    {
        if (_enemyShoot != null)
            _enemyShoot.ReloadStarted -= PlayEnemyReload;
    }
}