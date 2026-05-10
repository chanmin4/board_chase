using UnityEngine;

public class NamedSectorOccupancyProjection : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Occupancy component in the named battle sector.")]
    [SerializeField] private SectorOccupancy _battleSectorOccupancy;

    [Header("Listening")]
    [SerializeField] private SectorOccupancyEventChannelSO _sectorOccupancyChangedChannel;
    [SerializeField] private NamedSectorPhaseEventChannelSO _namedSectorPhaseChannel;

    [Header("Broadcasting")]
    [Tooltip("Projected occupancy is sent through the same channel so map/summary systems can read it.")]
    [SerializeField] private SectorOccupancyEventChannelSO _projectedOccupancyChannel;

    private SectorRuntime _sourceSector;
    private bool _isProjecting;

    private void OnEnable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised += HandleOccupancyChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised += HandleNamedSectorPhaseChanged;
    }

    private void OnDisable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised -= HandleOccupancyChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised -= HandleNamedSectorPhaseChanged;
    }

    private void HandleNamedSectorPhaseChanged(NamedSectorPhaseChange change)
    {
        if (change.Phase == NamedSectorPhase.Battle)
        {
            _sourceSector = change.Sector;
            _isProjecting = _sourceSector != null;
            PublishCurrentProjection();
            return;
        }

        if (change.Phase == NamedSectorPhase.EndingBattle ||
            change.Phase == NamedSectorPhase.DefeatedCooldown ||
            change.Phase == NamedSectorPhase.None)
        {
            _isProjecting = false;
            _sourceSector = null;
        }
    }

    private void HandleOccupancyChanged(SectorOccupancySnapshot snapshot)
    {
        if (!_isProjecting || _sourceSector == null || _battleSectorOccupancy == null)
            return;

        if (snapshot.sector != _battleSectorOccupancy.CurrentSnapshot.sector)
            return;

        PublishProjection(snapshot);
    }

    private void PublishCurrentProjection()
    {
        if (_battleSectorOccupancy == null)
            return;

        PublishProjection(_battleSectorOccupancy.CurrentSnapshot);
    }

    private void PublishProjection(SectorOccupancySnapshot battleSnapshot)
    {
        if (_projectedOccupancyChannel == null || _sourceSector == null)
            return;

        SectorOccupancySnapshot projected = battleSnapshot;
        projected.sector = _sourceSector;
        projected.specialState |= SectorSpecialState.NamedActive;

        _projectedOccupancyChannel.RaiseEvent(projected);
    }
}
