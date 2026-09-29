/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the IntEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannelSO : DescriptionBaseSO
{
    public UnityAction<int> OnEventRaised;
    // Purpose: Publishes the event notification.
    public void RaiseEvent(int value) => OnEventRaised?.Invoke(value);
}
