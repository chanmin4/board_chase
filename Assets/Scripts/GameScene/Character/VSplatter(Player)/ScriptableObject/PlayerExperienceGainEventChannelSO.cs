using System;
using UnityEngine;

public enum PlayerExperienceSource
{
    EnemyKill,
    Paint
}

[Serializable]
public struct PlayerExperienceGain
{
    public float amount;
    public PlayerExperienceSource source;
    public Vector3 worldPosition;
    public GameObject sender;

    public PlayerExperienceGain(
        float amount,
        PlayerExperienceSource source,
        Vector3 worldPosition,
        GameObject sender)
    {
        this.amount = amount;
        this.source = source;
        this.worldPosition = worldPosition;
        this.sender = sender;
    }
}

[CreateAssetMenu(
    fileName = "PlayerExperienceGainEventChannel",
    menuName = "Events/Player/Experience Gain Event Channel")]
public class PlayerExperienceGainEventChannelSO : ScriptableObject
{
    public event Action<PlayerExperienceGain> OnEventRaised;

    public void RaiseEvent(PlayerExperienceGain gain)
    {
        OnEventRaised?.Invoke(gain);
    }
}
