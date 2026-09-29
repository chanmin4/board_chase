/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Manages NamedEnemySpawnInfo stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(NamedEnemySpawnInfo context)
    {
        OnEventRaised?.Invoke(context);
    }
}
