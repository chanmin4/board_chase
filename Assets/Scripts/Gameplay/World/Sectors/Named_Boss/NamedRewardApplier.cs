/*
 * Role hierarchy: Gameplay > World > Sectors > Named_Boss
 * Primary responsibility: Implements the focused NamedRewardApplier responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class NamedRewardApplier : MonoBehaviour
{
    [Header("Runtime Ready Channels")]
    [Tooltip("Receives the runtime SectorStateManager instance.")]
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;

    [Tooltip("Receives the runtime MaskRenderManager instance.")]
    [SerializeField] private MaskRenderManagerEventChannelSO _maskRenderManagerReadyChannel;

    [Tooltip("Receives the runtime PlayerInfection instance.")]
    [SerializeField] private PlayerInfectionEventChannelSO _playerInfectionReadyChannel;


    [Header("Reward Shape")]
    [Tooltip("If true, reward applies to center sector plus four cardinal neighbor sectors.")]
    [SerializeField] private bool _applyCrossFiveSectors = true;
    [SerializeField] private InfectionControlManagerReadyEventChannelSO _infectionControlManagerReadyChannel;
    [SerializeField] private NamedSectorControllerReadyEventChannelSO _namedSectorControllerReadyChannel;

    private SectorStateManager _sectorStateManager;
    private MaskRenderManager _maskRenderManager;
    private PlayerShooterInfection _playerInfection;
    private InfectionControlManager _infectionControlManager;
    private NamedSectorController _namedSectorController;
    private static readonly Vector2Int[] CrossOffsets =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        if (_infectionControlManager == null)
            _infectionControlManager = FindAnyObjectByType<InfectionControlManager>();

        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_infectionControlManager == null)
            _infectionControlManager = FindAnyObjectByType<InfectionControlManager>();

        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

        if (_maskRenderManagerReadyChannel != null)
        {
            _maskRenderManagerReadyChannel.OnEventRaised += HandleMaskRenderManagerReady;

            if (_maskRenderManagerReadyChannel.Current != null)
                HandleMaskRenderManagerReady(_maskRenderManagerReadyChannel.Current);
        }

        if (_playerInfectionReadyChannel != null)
        {
            _playerInfectionReadyChannel.OnEventRaised += HandlePlayerInfectionReady;

            if (_playerInfectionReadyChannel.Current != null)
                HandlePlayerInfectionReady(_playerInfectionReadyChannel.Current);
        }
        if (_infectionControlManagerReadyChannel != null)
        {
            _infectionControlManagerReadyChannel.OnEventRaised += HandleInfectionControlManagerReady;

            if (_infectionControlManagerReadyChannel.HasCurrent)
                HandleInfectionControlManagerReady(_infectionControlManagerReadyChannel.Current);
        }

        if (_namedSectorControllerReadyChannel != null)
        {
            _namedSectorControllerReadyChannel.OnEventRaised += HandleNamedSectorControllerReady;

            if (_namedSectorControllerReadyChannel.HasCurrent)
                HandleNamedSectorControllerReady(_namedSectorControllerReadyChannel.Current);
        }
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

        if (_maskRenderManagerReadyChannel != null)
            _maskRenderManagerReadyChannel.OnEventRaised -= HandleMaskRenderManagerReady;

        if (_playerInfectionReadyChannel != null)
            _playerInfectionReadyChannel.OnEventRaised -= HandlePlayerInfectionReady;
        if (_infectionControlManagerReadyChannel != null)
            _infectionControlManagerReadyChannel.OnEventRaised -= HandleInfectionControlManagerReady;

        if (_namedSectorControllerReadyChannel != null)
            _namedSectorControllerReadyChannel.OnEventRaised -= HandleNamedSectorControllerReady;
    }

    // Purpose: Applies the requested reward change.
    public void ApplyReward(SectorRuntime centerSector)
    {
        if (centerSector == null)
            return;

        ApplySectorRewards(centerSector);
        RecoverInfectionControl();
        RecoverPlayerInfection();

        if (_namedSectorController != null)
            _namedSectorController.ConfirmNamedRewardAndEndBattle();
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();
    }

    // Purpose: Handles the incoming mask render manager ready request or event.
    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    // Purpose: Handles the incoming player infection ready request or event.
    private void HandlePlayerInfectionReady(PlayerShooterInfection playerInfection)
    {
        _playerInfection = playerInfection;
    }

    // Purpose: Applies the requested sector rewards change.
    private void ApplySectorRewards(SectorRuntime centerSector)
    {
        if (_sectorStateManager == null)
            return;

        Vector2Int centerCoord = _sectorStateManager.GetSectorCoord(centerSector);

        if (_applyCrossFiveSectors)
        {
            for (int i = 0; i < CrossOffsets.Length; i++)
                TryApplySectorReward(centerCoord + CrossOffsets[i]);

            return;
        }

        TryApplySectorReward(centerCoord);
    }

    // Purpose: Attempts to apply sector reward and reports whether the request succeeded.
    private void TryApplySectorReward(Vector2Int coord)
    {
        if (_sectorStateManager == null)
            return;

        if (!_sectorStateManager.TryGetSector(coord, out SectorRuntime sector))
            return;

        if (sector == null || !sector.IsOpened)
            return;

        ApplyPlayerPaintFull(sector);
        ApplyPlayerOccupancyFull(sector);
    }

    // Purpose: Applies the requested player paint full change.
    private void ApplyPlayerPaintFull(SectorRuntime sector)
    {
        if (_maskRenderManager == null || sector == null)
            return;

        SectorPaint paint = sector.GetComponentInChildren<SectorPaint>(true);
        if (paint == null)
            return;

        _maskRenderManager.FillSector(
            paint,
            PaintChannel.Vaccine,
            clearOtherChannel: true
        );
    }

    // Purpose: Applies the requested player occupancy full change.
    private void ApplyPlayerOccupancyFull(SectorRuntime sector)
    {
        if (sector == null)
            return;

        SectorOccupancy occupancy = sector.GetComponentInChildren<SectorOccupancy>(true);
        if (occupancy == null)
            return;

        occupancy.ForceOwnerAndRatios(SectorOwner.Player, 1f, 0f);
        occupancy.RemoveSpecialState(SectorSpecialState.NamedReserved);
        occupancy.RemoveSpecialState(SectorSpecialState.NamedActive);
    }

    // Purpose: Performs the recover infection control workflow for this component.
    private void RecoverInfectionControl()
    {
        if (_infectionControlManager != null)
            _infectionControlManager.RecoverNamedDefeated();
    }

    // Purpose: Performs the recover player infection workflow for this component.
    private void RecoverPlayerInfection()
    {
        if (_playerInfection != null)
            _playerInfection.RecoverOnNamedKilled();
    }
    // Purpose: Handles the incoming infection control manager ready request or event.
    private void HandleInfectionControlManagerReady(InfectionControlManager manager)
    {
        _infectionControlManager = manager;
    }

    // Purpose: Handles the incoming named sector controller ready request or event.
    private void HandleNamedSectorControllerReady(NamedSectorController controller)
    {
        _namedSectorController = controller;
    }
}
