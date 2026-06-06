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
    private PlayerInfection _playerInfection;
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

    private void Reset()
    {
        if (_infectionControlManager == null)
            _infectionControlManager = FindAnyObjectByType<InfectionControlManager>();

        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

    private void Awake()
    {
        if (_infectionControlManager == null)
            _infectionControlManager = FindAnyObjectByType<InfectionControlManager>();

        if (_namedSectorController == null)
            _namedSectorController = FindAnyObjectByType<NamedSectorController>();
    }

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

    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();
    }

    private void HandleMaskRenderManagerReady(MaskRenderManager manager)
    {
        _maskRenderManager = manager;
    }

    private void HandlePlayerInfectionReady(PlayerInfection playerInfection)
    {
        _playerInfection = playerInfection;
    }

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

    private void ApplyPlayerPaintFull(SectorRuntime sector)
    {
        if (_maskRenderManager == null || sector == null)
            return;

        SectorPaint paint = sector.GetComponentInChildren<SectorPaint>(true);
        if (paint == null)
            return;

        _maskRenderManager.FillSector(
            paint,
            MaskRenderManager.PaintChannel.Vaccine,
            clearOtherChannel: true
        );
    }

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

    private void RecoverInfectionControl()
    {
        if (_infectionControlManager != null)
            _infectionControlManager.RecoverNamedDefeated();
    }

    private void RecoverPlayerInfection()
    {
        if (_playerInfection != null)
            _playerInfection.RecoverOnNamedKilled();
    }
    private void HandleInfectionControlManagerReady(InfectionControlManager manager)
    {
        _infectionControlManager = manager;
    }

    private void HandleNamedSectorControllerReady(NamedSectorController controller)
    {
        _namedSectorController = controller;
    }
}
