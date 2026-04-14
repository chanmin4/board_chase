using System;
using UnityEngine;

[Serializable]
public struct InfectionControlSnapshot
{
    public float current;
    public float max;
    public float normalized;
    public float drainPerSecond;
}

[CreateAssetMenu(
    fileName = "InfectionControlChanged",
    menuName = "Events/Infection Control Changed")]
public class InfectionControlEventChannelSO : ScriptableObject
{
    public event Action<InfectionControlSnapshot> OnEventRaised;

    public void RaiseEvent(InfectionControlSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}