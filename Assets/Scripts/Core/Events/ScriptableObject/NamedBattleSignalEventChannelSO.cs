/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the NamedBattleSignalEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedBattleSignalEventChannel",
    menuName = "Events/Named Battle Signal Event Channel")]
public class NamedBattleSignalEventChannelSO : ScriptableObject
{
    public event System.Action<SectorRuntime> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(SectorRuntime sector)
    {
        OnEventRaised?.Invoke(sector);
    }
}
