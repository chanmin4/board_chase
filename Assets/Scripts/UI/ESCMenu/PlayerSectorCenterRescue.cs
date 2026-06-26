using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSectorCenterRescue : MonoBehaviour
{
    [Header("Listening To")]
    [Tooltip("현재 플레이어 Transform을 받는 이벤트 채널입니다.")]
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;

    [Tooltip("플레이어가 현재 위치한 섹터가 바뀔 때 받는 이벤트 채널입니다.")]
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;

    [Header("Refs")]
    [Tooltip("Fallback manager used when the current sector event was not received.")]
    [SerializeField] private SectorStateManager _sectorStateManager;

    [Header("Options")]
    [Tooltip("켜면 이동 후에도 플레이어의 현재 Y값을 유지합니다. 탑다운 평면 이동이면 보통 켜는 게 안전합니다.")]
    [SerializeField] private bool _keepPlayerY = false;

    [Tooltip("Keep Player Y가 꺼져 있을 때, 섹터 중심 Y에 더할 높이 보정값입니다.")]
    [SerializeField] private float _sectorCenterYOffset = 0.2f;

    [Tooltip("If enabled, unsafe Y values below/above the sector bounds are corrected even when Keep Player Y is enabled.")]
    [SerializeField] private bool _forceSafeYWhenOutsideSectorBounds = true;

    [Tooltip("Clears leftover movement vectors after rescue movement.")]
    [SerializeField] private bool _clearMovementOnMove = true;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;

    private Transform _player;
    private SectorRuntime _currentSector;

    private void Awake()
    {
        ResolveFallbackRefs();
    }

    private void OnEnable()
    {
        ResolveFallbackRefs();

        if (_playerRuntimeReadyChannel != null)
        {
            _playerRuntimeReadyChannel.OnEventRaised += HandlePlayerReady;

            if (_playerRuntimeReadyChannel.Current != null)
                HandlePlayerReady(_playerRuntimeReadyChannel.Current);
        }

        if (_currentSectorChangedEvent != null)
        {
            _currentSectorChangedEvent.OnEventRaised += HandleCurrentSectorChanged;

            if (_currentSectorChangedEvent.Current != null)
                HandleCurrentSectorChanged(_currentSectorChangedEvent.Current);
        }
    }

    private void OnDisable()
    {
        if (_playerRuntimeReadyChannel != null)
            _playerRuntimeReadyChannel.OnEventRaised -= HandlePlayerReady;

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised -= HandleCurrentSectorChanged;
    }

    public bool TryMovePlayerToCurrentSectorCenter()
    {
        ResolveFallbackRefs();

        if (_player == null)
            ResolvePlayerFallback();

        if (_currentSector == null)
            ResolveCurrentSectorFallback();

        if (_debugLogs)
        {
            Debug.Log(
                $"[PlayerSectorCenterRescue] Request. player={Describe(_player)}, sector={Describe(_currentSector)}",
                this);
        }

        if (_player == null)
        {
            Debug.LogWarning("[PlayerSectorCenterRescue] Player is not ready.", this);
            return false;
        }

        if (_currentSector == null)
        {
            Debug.LogWarning("[PlayerSectorCenterRescue] Current sector is not set.", this);
            return false;
        }

        Bounds bounds = _currentSector.GetWorldBounds();

        Vector3 targetPosition = bounds.center;
        targetPosition.y = ResolveTargetY(bounds);

        MovePlayer(targetPosition);

        if (_debugLogs)
        {
            Debug.Log(
                $"[PlayerSectorCenterRescue] Moved player to sector center. sector={_currentSector.name}, target={targetPosition}",
                this);
        }

        return true;
    }

    private void HandlePlayerReady(Transform playerRoot)
    {
        _player = playerRoot;
    }

    private void HandleCurrentSectorChanged(SectorRuntime sector)
    {
        if (sector == null)
            return;

        _currentSector = sector;
    }

    private void MovePlayer(Vector3 targetPosition)
    {
        CharacterController characterController = _player.GetComponent<CharacterController>();
        VSplatter_Character character = _player.GetComponent<VSplatter_Character>();

        bool hasCharacterController = characterController != null;
        bool wasControllerEnabled = hasCharacterController && characterController.enabled;

        if (hasCharacterController)
            characterController.enabled = false;

        _player.position = targetPosition;

        if (_clearMovementOnMove && character != null)
        {
            character.movementInput = Vector3.zero;
            character.movementVector = Vector3.zero;
        }

        if (hasCharacterController)
            characterController.enabled = wasControllerEnabled;
    }

    private float ResolveTargetY(Bounds bounds)
    {
        float safeY = bounds.center.y + _sectorCenterYOffset;

        if (!_keepPlayerY || _player == null)
            return safeY;

        float currentY = _player.position.y;

        if (_forceSafeYWhenOutsideSectorBounds &&
            (currentY < bounds.min.y || currentY > bounds.max.y))
        {
            return safeY;
        }

        return currentY;
    }

    private void ResolveFallbackRefs()
    {
        if (_sectorStateManager == null)
            _sectorStateManager = FindAnyObjectByType<SectorStateManager>();
    }

    private void ResolvePlayerFallback()
    {
        VSplatter_Character character = FindAnyObjectByType<VSplatter_Character>();

        if (character != null)
            _player = character.transform;
    }

    private void ResolveCurrentSectorFallback()
    {
        if (_sectorStateManager == null)
            return;

        if (_sectorStateManager.CurrentSector != null)
        {
            _currentSector = _sectorStateManager.CurrentSector;
            return;
        }

        if (_player == null)
            return;

        IReadOnlyList<SectorRuntime> sectors = _sectorStateManager.Sectors;

        if (sectors == null)
            return;

        Vector3 playerPosition = _player.position;

        for (int i = 0; i < sectors.Count; i++)
        {
            SectorRuntime sector = sectors[i];

            if (sector == null)
                continue;

            if (ContainsXZ(sector.GetWorldBounds(), playerPosition))
            {
                _currentSector = sector;
                return;
            }
        }
    }

    private static bool ContainsXZ(Bounds bounds, Vector3 position)
    {
        return position.x >= bounds.min.x &&
               position.x <= bounds.max.x &&
               position.z >= bounds.min.z &&
               position.z <= bounds.max.z;
    }

    private static string Describe(Object target)
    {
        return target != null ? target.name : "null";
    }
}
