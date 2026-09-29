/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the HitReceivedEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Combat/Hit Received Event Channel")]
public class HitReceivedEventChannelSO : DescriptionBaseSO
{
    public UnityAction<GameObject> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(GameObject target)
    {
        OnEventRaised?.Invoke(target);
    }
}
