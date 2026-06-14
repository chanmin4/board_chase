using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Combat/Hit Received Event Channel")]
public class HitReceivedEventChannelSO : DescriptionBaseSO
{
    public UnityAction<GameObject> OnEventRaised;

    public void RaiseEvent(GameObject target)
    {
        OnEventRaised?.Invoke(target);
    }
}
