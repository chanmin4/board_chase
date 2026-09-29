/*
 * Role hierarchy: Gameplay > World > Sectors > Runtime
 * Primary responsibility: Manages SectorPortalManager stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using PixeLadder.EasyTransition;
using UnityEngine;

[DisallowMultipleComponent]
public class SectorPortalManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SectorStateManager _sectorStateManager;
    [SerializeField] private CameraManager _cameraManager;
    [SerializeField] private StageProgressionManager _stageProgressionManager;
    [SerializeField] private StageSectorInstantiator _stageSectorInstantiator;

    [Header("Boss Transition")]
    [Tooltip("Shared runner that owns the fade request, input lock, and transition timeout flow when entering a Boss sector.")]
    [SerializeField] private GameplayScreenTransitionRunner _transitionRunner;

    [Tooltip("Screen transition request channel listened to by the gameplay TransitionInterfaceController.")]
    [SerializeField] private ScreenTransitionRequestEventChannelSO _transitionRequestChannel;

    [Tooltip("Gameplay input asset temporarily locked for the full Boss-sector entry transition.")]
    [SerializeField] private InputReader _inputReader;

    [Tooltip("Fade effect used when entering a Boss sector through any connected neighboring portal.")]
    [SerializeField] private TransitionEffect _bossEnterEffect;

    [Tooltip("Extra unscaled hold time after the screen becomes fully covered and the player is moved into the Boss sector.")]
    [Min(0f)]
    [SerializeField] private float _bossCoveredHoldSeconds = 0.2f;

    [Tooltip("Maximum unscaled time allowed for a Boss entry transition before the shared runner restores input.")]
    [Min(0.5f)]
    [SerializeField] private float _bossTransitionTimeoutSeconds = 5f;

    [Header("Events")]
    [SerializeField] private SectorRuntimeEventChannelSO _moveSectorCameraEvent;
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;
    [SerializeField] private SectorRuntimeEventChannelSO _sectorOpenedEvent;
    [SerializeField] private SectorStateManagerReadyEventChannelSO _sectorStateManagerReadyChannel;
    private readonly Dictionary<Vector2Int, SectorRuntime> _sectorByCoord = new();
    private bool _bossExitConsumed;
    private bool _extractionConsumed;
    private bool _bossTransitionInProgress;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_cameraManager == null)
            _cameraManager = FindAnyObjectByType<CameraManager>();
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();
        if (_stageProgressionManager == null)
            _stageProgressionManager = FindAnyObjectByType<StageProgressionManager>();
        if (_stageSectorInstantiator == null)
            _stageSectorInstantiator = FindAnyObjectByType<StageSectorInstantiator>();

        if (_sectorStateManager != null)
            _sectorStateManager.EnsureInitialized();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised += OnSectorOpened;
        if (_sectorStateManagerReadyChannel != null)
        {
            _sectorStateManagerReadyChannel.OnEventRaised += HandleSectorStateManagerReady;

            if (_sectorStateManagerReadyChannel.HasCurrent)
                HandleSectorStateManagerReady(_sectorStateManagerReadyChannel.Current);
        }

    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_sectorOpenedEvent != null)
            _sectorOpenedEvent.OnEventRaised -= OnSectorOpened;
        if (_sectorStateManagerReadyChannel != null)
            _sectorStateManagerReadyChannel.OnEventRaised -= HandleSectorStateManagerReady;

    }

    // Purpose: Completes initialization after scene dependencies are ready.
    private void Start()
    {
        RebuildPortalLinks(resetStartSectorConsumed: false);
    }

    // Purpose: Responds to the sector opened callback.
    private void OnSectorOpened(SectorRuntime sector)
    {
        RefreshAllPortals();
    }

    // Purpose: Caches sectors for later reuse.
    private void CacheSectors()
    {
        _sectorByCoord.Clear();

        if (_sectorStateManager == null)
        {
            Debug.LogWarning("[SectorPortalManager] SectorStateManager is missing.");
            return;
        }

        IReadOnlyList<SectorRuntime> sectors = _sectorStateManager.Sectors;

        if (sectors == null)
            return;

        for (int i = 0; i < sectors.Count; i++)
        {
            SectorRuntime sector = sectors[i];
            if (sector == null)
                continue;

            if (!_sectorStateManager.TryGetSectorCoord(sector, out Vector2Int coord))
                continue;

            if (!_sectorByCoord.ContainsKey(coord))
                _sectorByCoord.Add(coord, sector);
        }
    }

    // Purpose: Repairs or rebuilds portal links from the current source data.
    public void RebuildPortalLinks(bool resetStartSectorConsumed)
    {
        if (resetStartSectorConsumed)
        {
            _bossExitConsumed = false;
            _extractionConsumed = false;
        }

        CacheSectors();
        BindPortals();
        RefreshAllPortals();
    }

    // Purpose: Binds portals to the active runtime or UI target.
    private void BindPortals()
    {
        foreach (var pair in _sectorByCoord)
        {
            SectorRuntime sector = pair.Value;
            BindPortal(sector, SectorPortalDirection.XMin);
            BindPortal(sector, SectorPortalDirection.XMax);
            BindPortal(sector, SectorPortalDirection.ZMin);
            BindPortal(sector, SectorPortalDirection.ZMax);
        }
    }

    // Purpose: Binds portal to the active runtime or UI target.
    private void BindPortal(SectorRuntime sector, SectorPortalDirection direction)
    {
        SectorPortal portal = GetPortal(sector, direction);
        if (portal == null)
        {
            return;
        }

        portal.Initialize(this, sector, direction);
        portal.SetExtraction(IsExtractionPortal(portal));

        Vector2Int ownerCoord = _sectorStateManager.GetSectorCoord(sector);
        if (!HasDeclaredStageConnection(ownerCoord, direction))
        {
            portal.SetLink(null, null);
            return;
        }

        Vector2Int targetCoord = ownerCoord + SectorPortalDirectionUtility.ToCoordOffset(direction);

        if (!_sectorByCoord.TryGetValue(targetCoord, out SectorRuntime targetSector))
        {
            portal.SetLink(null, null);
            return;
        }

        SectorPortalDirection opposite = SectorPortalDirectionUtility.Opposite(direction);
        if (!HasDeclaredStageConnection(targetCoord, opposite))
        {
            portal.SetLink(null, null);
            return;
        }

        SectorPortal targetPortal = GetPortal(targetSector, opposite);


        portal.SetLink(targetSector, targetPortal);
    }

    /// <summary>
    /// Generated branch maps opt into portal links through their room flags.
    /// Home and other manually authored sectors have no map room, so they keep
    /// their historical adjacency-based portal behaviour.
    /// </summary>
    private bool HasDeclaredStageConnection(
        Vector2Int coord,
        SectorPortalDirection direction)
    {
        StageMapLayout layout = _sectorStateManager != null
            ? _sectorStateManager.CurrentStageMapLayout
            : null;

        if (layout == null || !layout.TryGetRoom(coord, out StageRoomNode room))
            return true;

        StageRoomConnectionFlags requiredConnection = direction switch
        {
            SectorPortalDirection.XMin => StageRoomConnectionFlags.Left,
            SectorPortalDirection.XMax => StageRoomConnectionFlags.Right,
            SectorPortalDirection.ZMin => StageRoomConnectionFlags.Down,
            SectorPortalDirection.ZMax => StageRoomConnectionFlags.Up,
            _ => StageRoomConnectionFlags.None
        };

        return requiredConnection != StageRoomConnectionFlags.None &&
               room.HasConnection(requiredConnection);
    }

    // Purpose: Refreshes the displayed or cached all portals.
    private void RefreshAllPortals()
    {
        if (_stageProgressionManager == null)
            _stageProgressionManager = FindAnyObjectByType<StageProgressionManager>();
        if (_stageSectorInstantiator == null)
            _stageSectorInstantiator = FindAnyObjectByType<StageSectorInstantiator>();

        foreach (var pair in _sectorByCoord)
        {
            SectorRuntime sector = pair.Value;

            RefreshPortal(sector, SectorPortalDirection.XMin);
            RefreshPortal(sector, SectorPortalDirection.XMax);
            RefreshPortal(sector, SectorPortalDirection.ZMin);
            RefreshPortal(sector, SectorPortalDirection.ZMax);
        }

    }

    // Purpose: Refreshes the displayed or cached portal.
    private void RefreshPortal(SectorRuntime sector, SectorPortalDirection direction)
    {
        SectorPortal portal = GetPortal(sector, direction);
        if (portal == null)
            return;

        bool isBossPortal = IsBossSector(sector);
        bool hasTopologyLink =
            portal.TargetSector != null &&
            portal.TargetPortal != null;
        bool isSyntheticBossExit = IsBossExitPortal(portal);
        bool isExtractionPortal = IsExtractionPortal(portal);

        portal.SetAvailable(
            CanUsePortal(portal),
            hideWholePortalWhenUnavailable:
                isBossPortal || (!hasTopologyLink && !isSyntheticBossExit && !isExtractionPortal));
    }

    // Purpose: Checks whether use portal is currently permitted.
    private bool CanUsePortal(SectorPortal portal)
    {
        if (portal == null)
        {
            Debug.LogWarning("[SectorPortalManager] CanUsePortal false: portal null");
            return false;
        }

        SectorRuntime source = portal.OwnerSector;
        SectorRuntime target = portal.TargetSector;

        if (source == null)
            return false;

        if (_bossTransitionInProgress)
            return false;

        // The generated extraction portal is a one-way Home return. It follows
        // the normal room-clear gate but never grants stage completion rewards.
        if (IsExtractionPortal(portal))
        {
            return !_extractionConsumed &&
                   source.IsOpened &&
                   (source.IsCleared ||
                    (_sectorStateManager != null && _sectorStateManager.IsSectorFailed(source)) ||
                    source.AllowsPortalTravelBeforeClear);
        }

        // A cleared Boss goal sector owns one synthetic exit: its unlinked north portal.
        if (IsBossExitPortal(portal))
        {
            return !_bossExitConsumed &&
                   source.IsOpened &&
                   source.IsCleared &&
                   IsCurrentGoalSector(source);
        }

        // After extraction, the reusable Start portal becomes the entry point for
        // the newly unlocked stage. It must never send the player back into the
        // already-cleared generated map.
        if (IsStartHubStageEntryPortal(portal))
        {
            return _stageProgressionManager != null &&
                   _stageProgressionManager.TryGetNextUnlockedStage(out _);
        }

        // Linked portals inside the Boss room stay hidden, but remain as arrival
        // anchors so the room can be entered from any connected neighboring room.
        if (IsBossSector(source) && _sectorStateManager.CurrentStageMapLayout?.usesRaidLayout != true)
            return false;

        if (target == null)
            return false;

        if (portal.TargetPortal == null)
            return false;

        if (!source.isOpened)
            return false;

        bool sourceResolved =
            source.IsCleared ||
            (_sectorStateManager != null && _sectorStateManager.IsSectorFailed(source)) ||
            source.AllowsPortalTravelBeforeClear;

        if (!sourceResolved)
            return false;

        if (!target.isOpened)
            return false;

        //Debug.Log($"[SectorPortalManager] CanUsePortal true. portal={portal.name}, source={source.name}, target={target.name}");
        return true;
    }
    // Purpose: Attempts to move through portal and reports whether the request succeeded.
    public bool TryMoveThroughPortal(SectorPortal sourcePortal, Transform player)
    {
        if (!CanUsePortal(sourcePortal) || player == null)
        {
            return false;
        }

        Transform playerRoot = ResolvePlayerRoot(player);
        if (playerRoot == null)
            return false;

        if (IsBossExitPortal(sourcePortal))
            return TryStartBossExitTransition(sourcePortal, playerRoot);

        if (IsExtractionPortal(sourcePortal))
        {
            bool extracted = _stageProgressionManager != null &&
                             _stageProgressionManager.TryExtractToHomeFromNormalExit(
                                 sourcePortal.OwnerSector);

            if (extracted)
                _extractionConsumed = true;

            return extracted;
        }

        if (IsStartHubStageEntryPortal(sourcePortal))
        {
            if (!_stageProgressionManager.TryGetNextUnlockedStage(
                    out StageDefinitionSO nextStage))
            {
                return false;
            }

            return _stageProgressionManager.TryEnterUnlockedStage(
                nextStage.StageIndex);
        }

        SectorPortal targetPortal = sourcePortal.TargetPortal;
        SectorRuntime targetSector = sourcePortal.TargetSector;

        if (targetPortal == null || targetSector == null)
        {
            return false;
        }

        if (IsBossSector(targetSector))
            return TryStartBossEntryTransition(sourcePortal, playerRoot);

        return MovePlayerThroughPortalNow(sourcePortal, playerRoot);
    }

    // Purpose: Checks whether start hub stage entry portal is currently true.
    private bool IsStartHubStageEntryPortal(SectorPortal portal)
    {
        return portal != null &&
               _stageProgressionManager != null &&
               _stageProgressionManager.CurrentStageCompletionClaimed &&
               _sectorStateManager != null &&
               _sectorStateManager.IsStartSector(portal.OwnerSector);
    }

    // Purpose: Checks whether the supplied portal is the selected stage extraction portal.
    private bool IsExtractionPortal(SectorPortal portal)
    {
        if (portal == null || _sectorStateManager == null)
            return false;

        StageMapLayout layout = _sectorStateManager.CurrentStageMapLayout;
        return layout != null &&
               layout.IsExtractionPortal(portal.OwnerSector != null
                   ? portal.OwnerSector.Coord
                   : default,
                   portal.Direction);
    }

    // Purpose: Attempts to start boss entry transition and reports whether the request succeeded.
    private bool TryStartBossEntryTransition(
        SectorPortal sourcePortal,
        Transform playerRoot)
    {
        if (_transitionRunner == null ||
            _transitionRequestChannel == null ||
            _inputReader == null ||
            _bossEnterEffect == null)
        {
            Debug.LogError(
                "[SectorPortalManager] Boss entry transition is incomplete. " +
                "Assign Transition Runner, Transition Request Channel, Input Reader, and Boss Enter Effect.",
                this);
            return false;
        }

        _bossTransitionInProgress = true;
        RefreshAllPortals();
        StartCoroutine(PlayBossEntryTransition(sourcePortal, playerRoot));
        return true;
    }

    // Purpose: Plays the requested boss entry transition action or effect.
    private IEnumerator PlayBossEntryTransition(
        SectorPortal sourcePortal,
        Transform playerRoot)
    {
        yield return _transitionRunner.PlayTransition(
            _transitionRequestChannel,
            _inputReader,
            _bossEnterEffect,
            _bossCoveredHoldSeconds,
            _bossTransitionTimeoutSeconds,
            () =>
            {
                if (!MovePlayerThroughPortalNow(sourcePortal, playerRoot))
                {
                    Debug.LogError(
                        "[SectorPortalManager] Boss entry transition reached full cover, " +
                        "but the player could not be moved through the portal.",
                        this);
                }
            },
            () =>
            {
                _bossTransitionInProgress = false;
                RefreshAllPortals();
            });

        // The shared runner normally invokes onComplete. Keep the portal manager from
        // remaining locked if its coroutine implementation changes or exits early.
        if (_bossTransitionInProgress)
        {
            _bossTransitionInProgress = false;
            RefreshAllPortals();
        }
    }

    // Purpose: Attempts to start boss exit transition and reports whether the request succeeded.
    private bool TryStartBossExitTransition(
        SectorPortal sourcePortal,
        Transform playerRoot)
    {
        if (_stageProgressionManager == null)
        {
            Debug.LogError(
                "[SectorPortalManager] Cannot complete the Boss exit because StageProgressionManager is missing.",
                this);
            return false;
        }

        if (_transitionRunner == null ||
            _transitionRequestChannel == null ||
            _inputReader == null ||
            _bossEnterEffect == null)
        {
            Debug.LogError(
                "[SectorPortalManager] Boss exit transition is incomplete. " +
                "Assign Transition Runner, Transition Request Channel, Input Reader, and Boss Enter Effect.",
                this);
            return false;
        }

        _bossTransitionInProgress = true;
        RefreshAllPortals();
        StartCoroutine(PlayBossExitTransition(sourcePortal, playerRoot));
        return true;
    }

    // Purpose: Plays the requested boss exit transition action or effect.
    private IEnumerator PlayBossExitTransition(
        SectorPortal sourcePortal,
        Transform playerRoot)
    {
        bool extracted = false;

        yield return _transitionRunner.PlayTransition(
            _transitionRequestChannel,
            _inputReader,
            _bossEnterEffect,
            _bossCoveredHoldSeconds,
            _bossTransitionTimeoutSeconds,
            () =>
            {
                bool completed =
                    sourcePortal != null &&
                    _stageProgressionManager.TryCompleteStageFromBossExit(
                        sourcePortal.OwnerSector);

                if (!completed)
                {
                    Debug.LogError(
                        "[SectorPortalManager] Boss exit reached full cover, but stage completion " +
                        "or return to the Hideout failed.",
                        this);
                    return;
                }

                extracted = true;
                _bossExitConsumed = true;
            },
            () =>
            {
                _bossTransitionInProgress = false;
                RefreshAllPortals();
            });

        if (_bossTransitionInProgress)
        {
            _bossTransitionInProgress = false;
            RefreshAllPortals();
        }

        if (extracted)
        {
            Debug.Log(
                "[SectorPortalManager] Boss sector cleared. Player extracted to the Hideout.",
                this);
        }
    }

    // Purpose: Moves player to hideout toward its requested destination.
    private bool MovePlayerToHideout(Transform playerRoot)
    {
        if (_stageSectorInstantiator == null)
            _stageSectorInstantiator = FindAnyObjectByType<StageSectorInstantiator>();

        if (_stageSectorInstantiator == null ||
            !_stageSectorInstantiator.EnsureHideout())
        {
            return false;
        }

        SectorRuntime hideoutSector = _stageSectorInstantiator.HideoutSector;

        if (playerRoot == null || hideoutSector == null)
            return false;

        CharacterController controller = playerRoot.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        playerRoot.SetParent(hideoutSector.transform, worldPositionStays: true);
        playerRoot.position = hideoutSector.PlayerStartPoint.position + Vector3.up * 0.05f;

        if (controller != null)
            controller.enabled = true;

        _cameraManager?.SnapToTarget();
        _moveSectorCameraEvent?.RaiseEvent(hideoutSector);

        return _sectorStateManager != null &&
               _sectorStateManager.SetExternalCurrentSector(
                   hideoutSector,
                   notifySpawnSystem: false);
    }

    // Purpose: Moves player through portal now toward its requested destination.
    private bool MovePlayerThroughPortalNow(
        SectorPortal sourcePortal,
        Transform playerRoot)
    {
        if (sourcePortal == null || playerRoot == null)
            return false;

        SectorPortal targetPortal = sourcePortal.TargetPortal;
        SectorRuntime targetSector = sourcePortal.TargetSector;

        if (targetPortal == null || targetSector == null)
            return false;

        CharacterController controller = playerRoot.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        playerRoot.SetParent(targetSector.transform, worldPositionStays: true);
        playerRoot.position = targetPortal.ArrivalPosition;

        if (controller != null)
            controller.enabled = true;

        if (_cameraManager != null)
            _cameraManager.SnapToTarget();
        if (_moveSectorCameraEvent != null)
            _moveSectorCameraEvent.RaiseEvent(targetSector);

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.RaiseEvent(targetSector);

        return true;
    }

    // Purpose: Resolves the player root needed by this component.
    private static Transform ResolvePlayerRoot(Transform actor)
    {
        if (actor == null)
            return null;

        PlayerCharacter player =
            actor.GetComponent<PlayerCharacter>() ??
            actor.GetComponentInParent<PlayerCharacter>();

        if (player != null)
            return player.transform;

        CharacterController controller =
            actor.GetComponent<CharacterController>() ??
            actor.GetComponentInParent<CharacterController>();

        return controller != null ? controller.transform : actor;
    }

    // Purpose: Returns the current portal.
    private SectorPortal GetPortal(SectorRuntime sector, SectorPortalDirection direction)
    {
        if (sector == null)
            return null;

        SectorEdge edge = GetEdge(sector, direction);
        if (edge == null || edge.portal == null)
            return null;

        return edge.portal.GetComponentInChildren<SectorPortal>(true);
    }

    // Purpose: Returns the current edge.
    private SectorEdge GetEdge(SectorRuntime sector, SectorPortalDirection direction)
    {
        switch (direction)
        {
            case SectorPortalDirection.XMin:
                return sector.XMin;

            case SectorPortalDirection.XMax:
                return sector.XMax;

            case SectorPortalDirection.ZMin:
                return sector.ZMin;

            case SectorPortalDirection.ZMax:
                return sector.ZMax;

            default:
                return null;
        }
    }

    // Purpose: Checks whether boss sector is currently true.
    private bool IsBossSector(SectorRuntime sector)
    {
        return _sectorStateManager != null &&
               sector != null &&
               _sectorStateManager.TryGetStageRoomType(
                   sector,
                   out StageRoomType roomType) &&
               roomType == StageRoomType.Boss;
    }

    // Purpose: Checks whether current goal sector is currently true.
    private bool IsCurrentGoalSector(SectorRuntime sector)
    {
        return _sectorStateManager != null &&
               sector != null &&
               _sectorStateManager.TryGetStageGoalSector(out SectorRuntime goalSector) &&
               goalSector == sector;
    }

    // Purpose: Checks whether boss exit portal is currently true.
    private bool IsBossExitPortal(SectorPortal portal)
    {
        return _sectorStateManager?.CurrentStageMapLayout?.usesRaidLayout != true && portal != null &&
               portal.Direction == SectorPortalDirection.ZMax &&
               portal.TargetSector == null &&
               portal.TargetPortal == null &&
               IsBossSector(portal.OwnerSector);
    }

    // Purpose: Handles the incoming sector state manager ready request or event.
    private void HandleSectorStateManagerReady(SectorStateManager manager)
    {
        if (manager == null)
            return;

        _sectorStateManager = manager;
        _sectorStateManager.EnsureInitialized();

        RebuildPortalLinks(resetStartSectorConsumed: false);
    }
}
