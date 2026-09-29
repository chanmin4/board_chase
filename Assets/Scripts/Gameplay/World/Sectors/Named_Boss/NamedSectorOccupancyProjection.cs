/*
 * Role hierarchy: Gameplay > World > Sectors > Named_Boss
 * Primary responsibility: Manages NamedSectorOccupancyProjection stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised += HandleOccupancyChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised += HandleNamedSectorPhaseChanged;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_sectorOccupancyChangedChannel != null)
            _sectorOccupancyChangedChannel.OnEventRaised -= HandleOccupancyChanged;

        if (_namedSectorPhaseChannel != null)
            _namedSectorPhaseChannel.OnEventRaised -= HandleNamedSectorPhaseChanged;
    }

    // Purpose: Handles the incoming named sector phase changed request or event.
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

    // Purpose: Handles the incoming occupancy changed request or event.
    private void HandleOccupancyChanged(SectorOccupancySnapshot snapshot)
    {
        if (!_isProjecting || _sourceSector == null || _battleSectorOccupancy == null)
            return;

        if (snapshot.sector != _battleSectorOccupancy.CurrentSnapshot.sector)
            return;

        PublishProjection(snapshot);
    }

    // Purpose: Publishes the current projection notification.
    private void PublishCurrentProjection()
    {
        if (_battleSectorOccupancy == null)
            return;

        PublishProjection(_battleSectorOccupancy.CurrentSnapshot);
    }

    // Purpose: Publishes the projection notification.
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
