/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the MetaProgressChangedEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MetaProgressChangedEventChannel",
    menuName = "Events/Meta/Meta Progress Changed Event Channel")]
public class MetaProgressChangedEventChannelSO : ScriptableObject
{
    public event Action<MetaProgressSnapshot> OnEventRaised;

    public bool HasCurrent { get; private set; }
    public MetaProgressSnapshot Current { get; private set; }

    // Purpose: Publishes the event notification.
    public void RaiseEvent(MetaProgressSnapshot snapshot)
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
