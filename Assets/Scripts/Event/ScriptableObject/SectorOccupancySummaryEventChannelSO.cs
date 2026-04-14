using UnityEngine;
using System;
[CreateAssetMenu(fileName = "SectorOccupancySummaryChanged", menuName = "Events/Sector Occupancy Summary Changed")]
public class SectorOccupancySummaryEventChannelSO : ScriptableObject
{
    public event Action<SectorOccupancySummary> OnEventRaised;
    public void RaiseEvent(SectorOccupancySummary summary) => OnEventRaised?.Invoke(summary);
}
