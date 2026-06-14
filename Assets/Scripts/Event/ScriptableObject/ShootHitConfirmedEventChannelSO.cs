using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Combat/Shoot Hit Confirmed Event Channel")]
public class ShootHitConfirmedEventChannelSO : DescriptionBaseSO
{
    public UnityAction OnEventRaised;

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
