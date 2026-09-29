/*
 * Role hierarchy: WIP > Legacy > NumericCurrency
 * Primary responsibility: Retains serialized numeric-currency event assets while the active economy uses physical item stacks.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

public enum PlayerCurrencyType
{
    Roguelike,
    Run
}

[Serializable]
public struct PlayerCurrencySnapshot
{
    public int roguelikeCurrency;
    public int runCurrency;

    public PlayerCurrencySnapshot(int roguelikeCurrency, int runCurrency)
    {
        this.roguelikeCurrency = Mathf.Max(0, roguelikeCurrency);
        this.runCurrency = Mathf.Max(0, runCurrency);
    }
}

[CreateAssetMenu(
    fileName = "PlayerCurrencyChangedEventChannel",
    menuName = "WIP/Legacy/Player Currency Changed Event Channel")]
public class PlayerCurrencyChangedEventChannelSO : ScriptableObject
{
    public event Action<PlayerCurrencySnapshot> OnEventRaised;

    public bool HasCurrent { get; private set; }
    public PlayerCurrencySnapshot Current { get; private set; }

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerCurrencySnapshot snapshot)
    {
        Current = snapshot;
        HasCurrent = true;
        OnEventRaised?.Invoke(snapshot);
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        Current = default;
        HasCurrent = false;
        OnEventRaised?.Invoke(Current);
    }
}
