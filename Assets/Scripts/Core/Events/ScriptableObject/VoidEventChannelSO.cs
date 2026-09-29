/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the VoidEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannelSO : DescriptionBaseSO
{
    public UnityAction OnEventRaised;
    // Purpose: Publishes the event notification.
    public void RaiseEvent() => OnEventRaised?.Invoke();
}
