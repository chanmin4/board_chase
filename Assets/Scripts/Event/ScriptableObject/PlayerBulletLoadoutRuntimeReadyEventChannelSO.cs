using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerBulletLoadoutRuntimeReadyEventChannel",
    menuName = "Events/Player/Player Bullet Loadout Runtime Ready Event Channel")]
public class PlayerBulletLoadoutRuntimeReadyEventChannelSO : ScriptableObject
{
    public event Action<PlayerBulletLoadoutRuntime> OnEventRaised;

    public PlayerBulletLoadoutRuntime Current { get; private set; }
    public bool HasCurrent => Current != null;

    public void RaiseEvent(PlayerBulletLoadoutRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    public void Clear(PlayerBulletLoadoutRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}