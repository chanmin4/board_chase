using UnityEngine;

public class NamedBattleSectorResetter : MonoBehaviour
{
    [Header("Battle Sector")]
    [Tooltip("The dedicated named battle sector to reset after the named battle ends.")]
    [SerializeField] private SectorRuntime _battleSector;

    [Header("Pattern Runtime")]
    [Tooltip("Stops the current QTE/pattern runtime state if it is still running.")]
    [SerializeField] private MutarusQTEPatternController _qtePatternController;

    [Tooltip("Clears all QTE stations back to inactive/uncompleted state.")]
    [SerializeField] private MutarusQTEStationGroup _qteStationGroup;

    [Header("Destroy Roots")]
    [Tooltip("Children under these roots are destroyed when the named battle ends. Use for projectiles, puddles, temp pattern objects.")]
    [SerializeField] private Transform[] _runtimeObjectRoots;

    [Header("Paint Reset")]
    [Tooltip("Receives the runtime MaskRenderManager instance.")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Tooltip("If true, battle sector paint/mask is cleared back to neutral.")]
    [SerializeField] private bool _clearBattleSectorPaint = true;

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

    public void ResetBattleSector()
    {
        CancelPatternRuntime();
        ClearQTEStations();
        DestroyRuntimeObjects();
        ClearBattleSectorPaint();
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    private void CancelPatternRuntime()
    {
        if (_qtePatternController != null)
            _qtePatternController.CancelPatternWithoutResult();
    }

    private void ClearQTEStations()
    {
        if (_qteStationGroup != null)
            _qteStationGroup.SetPatternActive(false);
    }

    private void DestroyRuntimeObjects()
    {
        if (_runtimeObjectRoots == null)
            return;

        for (int i = 0; i < _runtimeObjectRoots.Length; i++)
        {
            Transform root = _runtimeObjectRoots[i];

            if (root == null)
                continue;

            for (int childIndex = root.childCount - 1; childIndex >= 0; childIndex--)
                Destroy(root.GetChild(childIndex).gameObject);
        }
    }

    private void ClearBattleSectorPaint()
    {
        if (!_clearBattleSectorPaint || _battleSector == null || _maskRenderManager == null)
            return;

        SectorPaint paint = _battleSector.GetComponentInChildren<SectorPaint>(true);
        if (paint == null)
            return;

        _maskRenderManager.ClearAllToNeutral(paint);
        paint.ClearAllStoredPaint();

        SectorOccupancy occupancy = _battleSector.GetComponentInChildren<SectorOccupancy>(true);
        if (occupancy != null)
            occupancy.ForceOwnerAndRatios(SectorOwner.Neutral, 0f, 0f);
    }
}
