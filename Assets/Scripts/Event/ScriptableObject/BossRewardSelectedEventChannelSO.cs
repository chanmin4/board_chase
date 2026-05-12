using System;
using UnityEngine;

public readonly struct BossRewardSelection
{
    public readonly SectorRuntime sourceSector;
    public readonly NamedEnemy namedEnemy;
    public readonly BossRewardOptionSO reward;

    public BossRewardSelection(
        SectorRuntime sourceSector,
        NamedEnemy namedEnemy,
        BossRewardOptionSO reward)
    {
        this.sourceSector = sourceSector;
        this.namedEnemy = namedEnemy;
        this.reward = reward;
    }
}

[CreateAssetMenu(
    fileName = "BossRewardSelectedEventChannel",
    menuName = "Events/Boss Reward/Boss Reward Selected Event Channel")]
public class BossRewardSelectedEventChannelSO : ScriptableObject
{
    public event Action<BossRewardSelection> OnEventRaised;

    public void RaiseEvent(BossRewardSelection selection)
    {
        OnEventRaised?.Invoke(selection);
    }
}
