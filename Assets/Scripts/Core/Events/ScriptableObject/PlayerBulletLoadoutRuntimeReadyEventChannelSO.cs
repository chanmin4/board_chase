/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the PlayerBulletLoadoutRuntimeReadyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(PlayerBulletLoadoutRuntime runtime)
    {
        Current = runtime;
        OnEventRaised?.Invoke(runtime);
    }

    // Purpose: Clears the current clear state.
    public void Clear(PlayerBulletLoadoutRuntime runtime)
    {
        if (Current == runtime)
            Current = null;
    }
}
