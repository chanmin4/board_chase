using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannelSO : DescriptionBaseSO
{
    public UnityAction<int> OnEventRaised;
    public void RaiseEvent(int value) => OnEventRaised?.Invoke(value);
}