using System;
using UnityEngine;

[Serializable]
public struct PlayerLevelSnapshot
{
    public int level;
    public float currentXp;
    public float requiredXp;
    public float progress01;

    public int stageIndex;
    public float stageEarnedXp;
    public float stageXpLimit;
    public bool stageXpCapped;

    public PlayerLevelSnapshot(
        int level,
        float currentXp,
        float requiredXp,
        int stageIndex,
        float stageEarnedXp,
        float stageXpLimit)
    {
        this.level = level;
        this.currentXp = currentXp;
        this.requiredXp = requiredXp;
        this.progress01 = requiredXp > 0f ? Mathf.Clamp01(currentXp / requiredXp) : 1f;

        this.stageIndex = stageIndex;
        this.stageEarnedXp = stageEarnedXp;
        this.stageXpLimit = stageXpLimit;
        this.stageXpCapped = stageXpLimit > 0f && stageEarnedXp >= stageXpLimit;
    }
}

[CreateAssetMenu(
    fileName = "PlayerLevelSnapshotEventChannel",
    menuName = "Events/Player/Level Snapshot Event Channel")]
public class PlayerLevelSnapshotEventChannelSO : ScriptableObject
{
    public event Action<PlayerLevelSnapshot> OnEventRaised;

    public void RaiseEvent(PlayerLevelSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
