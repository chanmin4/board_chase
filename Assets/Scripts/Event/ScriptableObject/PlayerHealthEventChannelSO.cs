using System;
using UnityEngine;
//HUD에 체력/감염 상태를 넘기는 이벤트.
[Serializable]
public struct PlayerHealthSnapshot
{
    public float maxHealth;
    public float currentHealth;
    public float currentInfection;
    public bool isDead;

    public float Health01 => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    public float Infection01 => maxHealth > 0f ? Mathf.Clamp01(currentInfection / maxHealth) : 0f;

    public PlayerHealthSnapshot(
        float maxHealth,
        float currentHealth,
        float currentInfection,
        bool isDead)
    {
        this.maxHealth = maxHealth;
        this.currentHealth = currentHealth;
        this.currentInfection = currentInfection;
        this.isDead = isDead;
    }
}

[CreateAssetMenu(
    fileName = "PlayerVitalsEventChannel",
    menuName = "Events/Player/Player Health Event Channel")]
public class PlayerHealthEventChannelSO : ScriptableObject
{
    public event Action<PlayerHealthSnapshot> OnEventRaised;

    public void RaiseEvent(PlayerHealthSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
