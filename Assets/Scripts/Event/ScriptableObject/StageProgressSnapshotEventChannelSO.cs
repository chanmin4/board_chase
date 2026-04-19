using System;
using UnityEngine;

[Serializable]
public struct StageProgressSnapshot
{
    public int stageIndex;
    public string displayName;

    public float remainingSeconds;
    public float durationSeconds;
    public float progress01;

    public int requiredPlayerOwnedCount;
    public int currentPlayerOwnedCount;

    public bool requirementMet;
    public bool isCompleted;
    public bool hasNextStage;
}

[CreateAssetMenu(
    fileName = "StageProgressSnapshotChanged",
    menuName = "Events/Stage Progress Snapshot Changed")]
public class StageProgressSnapshotEventChannelSO : ScriptableObject
{
    public event Action<StageProgressSnapshot> OnEventRaised;

    public void RaiseEvent(StageProgressSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
