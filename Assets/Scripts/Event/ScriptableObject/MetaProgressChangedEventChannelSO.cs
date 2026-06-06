using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MetaProgressChangedEventChannel",
    menuName = "Events/Meta/Meta Progress Changed Event Channel")]
public class MetaProgressChangedEventChannelSO : ScriptableObject
{
    public event Action<MetaProgressSnapshot> OnEventRaised;

    public bool HasCurrent { get; private set; }
    public MetaProgressSnapshot Current { get; private set; }

    public void RaiseEvent(MetaProgressSnapshot snapshot)
    {
        Current = snapshot;
        HasCurrent = true;
        OnEventRaised?.Invoke(snapshot);
    }

    public void Clear()
    {
        Current = default;
        HasCurrent = false;
        OnEventRaised?.Invoke(Current);
    }
}
