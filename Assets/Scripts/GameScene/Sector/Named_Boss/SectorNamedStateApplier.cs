using UnityEngine;

public class SectorNamedStateApplier : MonoBehaviour
{
    [Header("Runtime Ready Channels")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    private MaskRenderManager _maskRenderManager;

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

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

    public void ClearNamedState(SectorRuntime sector)
    {
        SectorOccupancy occupancy = GetOccupancy(sector);
        if (occupancy == null)
            return;

        occupancy.RemoveSpecialState(SectorSpecialState.NamedReserved);
        occupancy.RemoveSpecialState(SectorSpecialState.NamedActive);
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

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

    private SectorOccupancy GetOccupancy(SectorRuntime sector)
    {
        return sector != null
            ? sector.GetComponentInChildren<SectorOccupancy>(true)
            : null;
    }

    private string GetSectorName(SectorRuntime sector)
    {
        return sector != null ? sector.name : "null";
    }
}