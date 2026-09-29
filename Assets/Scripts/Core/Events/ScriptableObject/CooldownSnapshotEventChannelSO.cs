/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Implements the focused CooldownSnapshot responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public struct CooldownSnapshot
{
    public bool isReady;
    public bool isActive;
    public float remainingSeconds;
    public float totalSeconds;

    public float Normalized01 =>
        totalSeconds > 0f
            ? Mathf.Clamp01(remainingSeconds / totalSeconds)
            : 0f;

    public CooldownSnapshot(
        bool isReady,
        bool isActive,
        float remainingSeconds,
        float totalSeconds)
    {
        this.isReady = isReady;
        this.isActive = isActive;
        this.remainingSeconds = remainingSeconds;
        this.totalSeconds = totalSeconds;
    }
}

[CreateAssetMenu(
    fileName = "CooldownSnapshotEventChannel",
    menuName = "Events/UI/Cooldown Snapshot Event Channel")]
public class CooldownSnapshotEventChannelSO : ScriptableObject
{
    public event Action<CooldownSnapshot> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(CooldownSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
