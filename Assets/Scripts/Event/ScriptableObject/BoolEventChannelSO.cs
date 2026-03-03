using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Bool Event Channel")]
public class BoolEventChannelSO : DescriptionBaseSO
{
    public UnityAction<bool> OnEventRaised;
    public void RaiseEvent(bool value) => OnEventRaised?.Invoke(value);
}