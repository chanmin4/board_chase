using System;
using UnityEngine;

public readonly struct BossRewardRequest
{
    public readonly SectorRuntime sourceSector;
    public readonly NamedEnemy namedEnemy;

    public BossRewardRequest(SectorRuntime sourceSector, NamedEnemy namedEnemy)
    {
        this.sourceSector = sourceSector;
        this.namedEnemy = namedEnemy;
    }
}

[CreateAssetMenu(
    fileName = "BossRewardRequestEventChannel",
    menuName = "Events/Boss Reward/Boss Reward Request Event Channel")]
public class BossRewardRequestEventChannelSO : ScriptableObject
{
    public event Action<BossRewardRequest> OnEventRaised;

    public void RaiseEvent(BossRewardRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
