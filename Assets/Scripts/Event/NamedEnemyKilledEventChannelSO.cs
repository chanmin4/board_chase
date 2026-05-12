using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NamedEnemyKilledEventChannel",
    menuName = "Events/Named Enemy/Named Enemy Killed Event Channel")]
public class NamedEnemyKilledEventChannelSO : ScriptableObject
{
    public event Action<NamedEnemy> OnEventRaised;

    public void RaiseEvent(NamedEnemy namedEnemy)
    {
        OnEventRaised?.Invoke(namedEnemy);
    }
}
