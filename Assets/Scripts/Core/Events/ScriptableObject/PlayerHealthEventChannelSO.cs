/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Handles PlayerHealthSnapshot combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[Serializable]
public struct PlayerHealthSnapshot
{
    public float maxHealth;
    public float currentHealth;
    public float currentInfection;
    public float infectionRecoveryPerSecond;
    public bool isDead;

    public float Health01 => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
    public float Infection01 => currentHealth > 0f ? Mathf.Clamp01(currentInfection / currentHealth) : 0f;

    public PlayerHealthSnapshot(
        float maxHealth,
        float currentHealth,
        float currentInfection,
        float infectionRecoveryPerSecond,
        bool isDead)
    {
        this.maxHealth = maxHealth;
        this.currentHealth = currentHealth;
        this.currentInfection = currentInfection;
        this.infectionRecoveryPerSecond = infectionRecoveryPerSecond;
        this.isDead = isDead;
    }
}

[CreateAssetMenu(
    fileName = "PlayerVitalsEventChannel",
    menuName = "Events/Player/Player Health Event Channel")]
public class PlayerHealthEventChannelSO : ScriptableObject
{
    public event Action<PlayerHealthSnapshot> OnEventRaised;

    [NonSerialized] private bool _hasCurrent;
    [NonSerialized] private PlayerHealthSnapshot _current;

    public bool HasCurrent => _hasCurrent;
    public PlayerHealthSnapshot Current => _current;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerHealthSnapshot snapshot)
    {
        _current = snapshot;
        _hasCurrent = true;
        OnEventRaised?.Invoke(snapshot);
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        _hasCurrent = false;
        _current = default;
    }
}
