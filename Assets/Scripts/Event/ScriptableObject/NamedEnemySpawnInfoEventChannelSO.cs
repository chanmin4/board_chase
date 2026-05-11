using System;
using UnityEngine;

public readonly struct NamedEnemySpawnInfo
{
    public readonly NamedEnemy namedEnemy;
    public readonly SectorRuntime sourceSector;
    public readonly Transform spawnPoint;

    public NamedEnemySpawnInfo(
        NamedEnemy namedEnemy,
        SectorRuntime sourceSector,
        Transform spawnPoint)
    {
        this.namedEnemy = namedEnemy;
        this.sourceSector = sourceSector;
        this.spawnPoint = spawnPoint;
    }
}

[CreateAssetMenu(
    fileName = "NamedEnemySpawnInfoEventChannel",
    menuName = "Events/Named Enemy Spawn Info Event Channel")]
public class NamedEnemySpawnInfoEventChannelSO : ScriptableObject
{
    public event Action<NamedEnemySpawnInfo> OnEventRaised;

    public void RaiseEvent(NamedEnemySpawnInfo context)
    {
        OnEventRaised?.Invoke(context);
    }
}
