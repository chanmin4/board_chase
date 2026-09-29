/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy
 * Primary responsibility: Manages NamedBattleSectorResetter stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class NamedBattleSectorResetter : MonoBehaviour
{
    [Header("Battle Sector")]
    [Tooltip("The dedicated named battle sector to reset after the named battle ends.")]
    [SerializeField] private SectorRuntime _battleSector;
    [Header("Broadcasting On")]
    [Tooltip("Requests the Mutarus QTE pattern controller in another scene to cancel/reset its current runtime.")]
    [SerializeField] private MutarusQTEPatternResetRequestEventChannelSO _qtePatternResetRequestChannel;

    [Header("Listening")]
    [SerializeField] private NamedBattleSectorResetRequestEventChannelSO _battleSectorResetRequestChannel;
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

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }
        if (_battleSectorResetRequestChannel != null)
            _battleSectorResetRequestChannel.OnEventRaised += HandleBattleSectorResetRequested;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;
        if (_battleSectorResetRequestChannel != null)
            _battleSectorResetRequestChannel.OnEventRaised -= HandleBattleSectorResetRequested;
    
    }
    // Purpose: Handles the incoming battle sector reset requested request or event.
    private void HandleBattleSectorResetRequested()
    {
        ResetBattleSector();
    }
    // Purpose: Performs the reset battle sector workflow for this component.
    public void ResetBattleSector()
    {
        CancelPatternRuntime();
        ClearQTEStations();
        ClearBattleSectorPaint();
        DestroyRuntimeObjects();

    }

    // Purpose: Handles the incoming mask render manager ready request or event.
    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    // Purpose: Checks whether cancel pattern runtime is currently permitted.
    private void CancelPatternRuntime()
    {
        if (_qtePatternResetRequestChannel != null)
            _qtePatternResetRequestChannel.RaiseEvent();
    }

    // Purpose: Clears the current qte stations state.
    private void ClearQTEStations()
    {
        if (_qteStationGroup != null)
            _qteStationGroup.SetPatternActive(false);
    }

    // Purpose: Removes runtime objects from the active runtime context.
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
            {
                GameObject child = root.GetChild(childIndex).gameObject;
                
                child.SetActive(false);
                Destroy(child);
            }
        }
    }

    // Purpose: Clears the current battle sector paint state.
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
