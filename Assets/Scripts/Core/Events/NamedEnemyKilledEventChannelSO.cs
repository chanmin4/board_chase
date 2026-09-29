/*
 * Role hierarchy: Core > Events
 * Primary responsibility: Defines the NamedEnemyKilledEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedEnemyKilledEventChannel",
    menuName = "Events/Named Enemy/Named Enemy Killed Event Channel")]
public class NamedEnemyKilledEventChannelSO : ScriptableObject
{
    public event Action<NamedEnemy> OnEventRaised;

    // Purpose: Publishes the event notification.
    public void RaiseEvent(NamedEnemy namedEnemy)
    {
        OnEventRaised?.Invoke(namedEnemy);
    }
}
