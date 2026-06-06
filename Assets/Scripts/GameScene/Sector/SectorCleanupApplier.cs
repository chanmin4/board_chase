using UnityEngine;

public class SectorCleanupApplier : MonoBehaviour
{
    [Header("Need Ref")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Header("Default Options")]
    [SerializeField] private bool _destroyEnemies = true;
    [SerializeField] private bool _destroyCleanupRootChildren = true;
    [SerializeField] private bool _clearPaintMasks = true;
    [SerializeField] private bool _resetOccupancy = true;

    private MaskRenderManager _maskRenderManager;

    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel == null)
            return;

        _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

        if (_maskRenderManagerReadyChannel.Current != null)
            HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
    }

    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
    }

    public void CleanupSector(SectorRuntime sector)
    {
        CleanupSector(sector, _clearPaintMasks);
    }

    public void CleanupSector(SectorRuntime sector, bool clearPaintMasks)
    {
        if (sector == null)
            return;

        if (_destroyEnemies)
            DestroyComponentsInChildren<Enemy>(sector.transform);

        if (_destroyCleanupRootChildren)
            DestroyCleanupRootChildren(sector);

        if (clearPaintMasks)
            ClearSectorPaint(sector);

        if (_resetOccupancy)
            ResetSectorOccupancy(sector);
    }

    public void ApplyPlayerCompletedState(SectorRuntime sector)
    {
        if (sector == null)
            return;

        SectorPaint paint = sector.GetComponentInChildren<SectorPaint>(true);

        if (paint != null)
        {
            if (_maskRenderManager != null)
            {
                _maskRenderManager.FillSector(
                    paint,
                    MaskRenderManager.PaintChannel.Vaccine,
                    clearOtherChannel: true);
            }
            else
            {
                paint.FillGameplay(
                    MaskRenderManager.PaintChannel.Vaccine,
                    clearOtherChannel: true);
                paint.ClearAllStoredPaint();
            }
        }

        SectorOccupancy occupancy =
            sector.GetComponentInChildren<SectorOccupancy>(true);

        if (occupancy != null)
            occupancy.ForceOwnerAndRatios(SectorOwner.Player, 1f, 0f);
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    private void ClearSectorPaint(SectorRuntime sector)
    {
        SectorPaint paint = sector.GetComponentInChildren<SectorPaint>(true);
        if (paint == null)
            return;

        if (_maskRenderManager != null)
            _maskRenderManager.ClearAllToNeutral(paint);
        else
            paint.ClearAllPaintCoverage();

        paint.ClearAllStoredPaint();
    }

    private void ResetSectorOccupancy(SectorRuntime sector)
    {
        SectorOccupancy occupancy = sector.GetComponentInChildren<SectorOccupancy>(true);
        if (occupancy != null)
            occupancy.ResetToNeutral();
    }

    private void DestroyComponentsInChildren<T>(Transform root) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
                Destroy(components[i].gameObject);
        }
    }

    private void DestroyCleanupRootChildren(SectorRuntime sector)
    {
        Transform[] roots = sector.CleanupRoots;
        if (roots == null)
            return;

        for (int i = 0; i < roots.Length; i++)
            DestroyChildren(roots[i]);
    }

    private void DestroyChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
    public void CleanupThenApplyPlayerCompletedState(SectorRuntime sector,
    bool clearPaintMasks)
    {
        if (sector == null)
            return;

        CleanupSector(sector, clearPaintMasks);
        ApplyPlayerCompletedState(sector);
    }
}
