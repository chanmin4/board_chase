using System;
using UnityEngine;

public readonly struct EnemyLootOpenRequest
{
    public readonly EnemyLootInventoryRuntime lootInventory;
    public readonly Component actor;
    public readonly float interactSeconds;

    public EnemyLootOpenRequest(
        EnemyLootInventoryRuntime lootInventory,
        Component actor,
        float interactSeconds)
    {
        this.lootInventory = lootInventory;
        this.actor = actor;
        this.interactSeconds = Mathf.Max(0f, interactSeconds);
    }
}

[CreateAssetMenu(
    fileName = "EnemyLootOpenRequestEventChannel",
    menuName = "Events/Loot/Enemy Loot Open Request Event Channel")]
public class EnemyLootOpenRequestEventChannelSO : ScriptableObject
{
    public event Action<EnemyLootOpenRequest> OnEventRaised;

    public void RaiseEvent(EnemyLootOpenRequest request)
    {
        OnEventRaised?.Invoke(request);
    }
}
