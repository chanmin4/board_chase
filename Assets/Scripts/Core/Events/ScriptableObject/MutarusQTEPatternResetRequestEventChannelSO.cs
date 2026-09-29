/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the MutarusQTEPatternResetRequestEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MutarusQTEPatternResetRequestEventChannel",
    menuName = "Events/Named Enemy/Mutarus QTE Pattern Reset Request Event Channel")]
public class MutarusQTEPatternResetRequestEventChannelSO : ScriptableObject
{
    public event Action OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
