using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSectorCenterRescue : MonoBehaviour
{
    [Header("Listening To")]
    [Tooltip("현재 플레이어 Transform을 받는 이벤트 채널입니다.")]
    [SerializeField] private PlayerRuntimeReadyEventChannelSO _playerRuntimeReadyChannel;

    [Tooltip("플레이어가 현재 위치한 섹터가 바뀔 때 받는 이벤트 채널입니다.")]
    [SerializeField] private SectorRuntimeEventChannelSO _currentSectorChangedEvent;

    [Header("Options")]
    [Tooltip("켜면 이동 후에도 플레이어의 현재 Y값을 유지합니다. 탑다운 평면 이동이면 보통 켜는 게 안전합니다.")]
    [SerializeField] private bool _keepPlayerY = true;

    [Tooltip("Keep Player Y가 꺼져 있을 때, 섹터 중심 Y에 더할 높이 보정값입니다.")]
    [SerializeField] private float _sectorCenterYOffset = 0f;

    private Transform _player;
    private SectorRuntime _currentSector;

    private void OnEnable()
    {
        if (_playerRuntimeReadyChannel != null)
        {
            _playerRuntimeReadyChannel.OnEventRaised += HandlePlayerReady;

            if (_playerRuntimeReadyChannel.Current != null)
                HandlePlayerReady(_playerRuntimeReadyChannel.Current);
        }

        if (_currentSectorChangedEvent != null)
            _currentSectorChangedEvent.OnEventRaised += HandleCurrentSectorChanged;
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
        targetPosition.y = _keepPlayerY
            ? _player.position.y
            : bounds.center.y + _sectorCenterYOffset;

        MovePlayer(targetPosition);
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

        bool hasCharacterController = characterController != null;
        bool wasControllerEnabled = hasCharacterController && characterController.enabled;

        if (hasCharacterController)
            characterController.enabled = false;

        _player.position = targetPosition;

        if (hasCharacterController)
            characterController.enabled = wasControllerEnabled;
    }
}