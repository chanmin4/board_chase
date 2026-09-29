/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the BoolEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Bool Event Channel")]
public class BoolEventChannelSO : DescriptionBaseSO
{
    public UnityAction<bool> OnEventRaised;
    // Purpose: Publishes the event notification.
    public void RaiseEvent(bool value) => OnEventRaised?.Invoke(value);
}
