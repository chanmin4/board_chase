/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the NamedBattleSectorResetRequestEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedBattleSectorResetRequestEventChannel",
    menuName = "Events/Named Enemy/Named Battle Sector Reset Request Event Channel")]
public class NamedBattleSectorResetRequestEventChannelSO : ScriptableObject
{
    public event Action OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
