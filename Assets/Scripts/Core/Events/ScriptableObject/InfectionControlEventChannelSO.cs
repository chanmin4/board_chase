/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Handles InfectionControlSnapshot combat resolution, damage, projectile, or status changes.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Publishes the event notification.
    public void RaiseEvent(InfectionControlSnapshot snapshot)
    {
        OnEventRaised?.Invoke(snapshot);
    }
}
