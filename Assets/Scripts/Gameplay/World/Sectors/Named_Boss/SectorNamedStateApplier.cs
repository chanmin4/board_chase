/*
 * Role hierarchy: Gameplay > World > Sectors > Named_Boss
 * Primary responsibility: Manages SectorNamedStateApplier stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class SectorNamedStateApplier : MonoBehaviour
{
    [Header("Runtime Ready Channels")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    private MaskRenderManager _maskRenderManager;

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

    // Purpose: Updates the reserved state.
    public void SetReserved(SectorRuntime sector)
    {
        SectorOccupancy occupancy = GetOccupancy(sector);
        if (occupancy == null)
        {
            Debug.LogWarning($"[SectorNamedStateApplier] SectorOccupancy missing. sector={GetSectorName(sector)}", this);
            return;
        }

        occupancy.RemoveSpecialState(SectorSpecialState.NamedActive);
        occupancy.AddSpecialState(SectorSpecialState.NamedReserved);
    }

    // Purpose: Updates the presented state.
    public void SetPresented(SectorRuntime sector)
    {
        SectorOccupancy occupancy = GetOccupancy(sector);
        if (occupancy == null)
        {
            Debug.LogWarning($"[SectorNamedStateApplier] SectorOccupancy missing. sector={GetSectorName(sector)}", this);
            return;
        }

        occupancy.RemoveSpecialState(SectorSpecialState.NamedReserved);
        occupancy.AddSpecialState(SectorSpecialState.NamedActive);

        ClearSectorPaint(sector);
        occupancy.ForceOwnerAndRatios(SectorOwner.Neutral, 0f, 0f);
    }

    // Purpose: Clears the current named state state.
    public void ClearNamedState(SectorRuntime sector)
    {
        SectorOccupancy occupancy = GetOccupancy(sector);
        if (occupancy == null)
            return;

        occupancy.RemoveSpecialState(SectorSpecialState.NamedReserved);
        occupancy.RemoveSpecialState(SectorSpecialState.NamedActive);
    }

    // Purpose: Handles the incoming mask render manager ready request or event.
    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    // Purpose: Clears the current sector paint state.
    private void ClearSectorPaint(SectorRuntime sector)
    {
        if (_maskRenderManager == null || sector == null)
            return;

        SectorPaint paint = sector.GetComponentInChildren<SectorPaint>(true);
        if (paint == null)
        {
            Debug.LogWarning($"[SectorNamedStateApplier] SectorPaint missing. sector={sector.name}", this);
            return;
        }

        _maskRenderManager.ClearAllToNeutral(paint);
        paint.ClearAllStoredPaint();
    }

    // Purpose: Returns the current occupancy.
    private SectorOccupancy GetOccupancy(SectorRuntime sector)
    {
        return sector != null
            ? sector.GetComponentInChildren<SectorOccupancy>(true)
            : null;
    }

    // Purpose: Returns the current sector name.
    private string GetSectorName(SectorRuntime sector)
    {
        return sector != null ? sector.name : "null";
    }
}
