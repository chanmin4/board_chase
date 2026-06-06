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
    menuName = "Events/Player/Player Currency Changed Event Channel")]
public class PlayerCurrencyChangedEventChannelSO : ScriptableObject
{
    public event Action<PlayerCurrencySnapshot> OnEventRaised;

    public bool HasCurrent { get; private set; }
    public PlayerCurrencySnapshot Current { get; private set; }

    public void RaiseEvent(PlayerCurrencySnapshot snapshot)
    {
        Current = snapshot;
        HasCurrent = true;
        OnEventRaised?.Invoke(snapshot);
    }

    public void Clear()
    {
        Current = default;
        HasCurrent = false;
        OnEventRaised?.Invoke(Current);
    }
}