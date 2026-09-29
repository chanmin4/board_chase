/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the ShootHitConfirmedEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Combat/Shoot Hit Confirmed Event Channel")]
public class ShootHitConfirmedEventChannelSO : DescriptionBaseSO
{
    public UnityAction OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
