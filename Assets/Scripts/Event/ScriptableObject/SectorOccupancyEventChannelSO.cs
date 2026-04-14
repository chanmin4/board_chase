using UnityEngine;
using System;
[CreateAssetMenu(fileName = "SectorOccupancyChanged", menuName = "Events/Sector Occupancy Changed Event")]
public class SectorOccupancyEventChannelSO : ScriptableObject
{
    public event Action<SectorOccupancySnapshot> OnEventRaised;
    public void RaiseEvent(SectorOccupancySnapshot snapshot) => OnEventRaised?.Invoke(snapshot);
}