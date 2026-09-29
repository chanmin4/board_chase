/*
 * Role hierarchy: Core > Events > ScriptableObject
 * Primary responsibility: Defines the SectorOccupancySummaryEventChannelSO publish-subscribe contract to reduce direct system coupling.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using System;
[CreateAssetMenu(fileName = "SectorOccupancySummaryChanged", menuName = "Events/Sector Occupancy Summary Changed")]
public class SectorOccupancySummaryEventChannelSO : ScriptableObject
{
    public event Action<SectorOccupancySummary> OnEventRaised;
    // Purpose: Publishes the event notification.
    public void RaiseEvent(SectorOccupancySummary summary) => OnEventRaised?.Invoke(summary);
}
