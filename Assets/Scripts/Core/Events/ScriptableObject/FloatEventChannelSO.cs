/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the FloatEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Float Event Channel")]
public class FloatEventChannelSO : DescriptionBaseSO
{
    public UnityAction<float> OnEventRaised;
    // Purpose: Publishes the event notification.
    public void RaiseEvent(float value) => OnEventRaised?.Invoke(value);
}
