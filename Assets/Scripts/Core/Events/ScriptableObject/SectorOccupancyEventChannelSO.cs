/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the SectorOccupancyEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using System;
[CreateAssetMenu(fileName = "SectorOccupancyChanged", menuName = "Events/Sector Occupancy Changed Event")]
public class SectorOccupancyEventChannelSO : ScriptableObject
{
    public event Action<SectorOccupancySnapshot> OnEventRaised;
    // Purpose: Publishes the event notification.
    public void RaiseEvent(SectorOccupancySnapshot snapshot) => OnEventRaised?.Invoke(snapshot);
}
