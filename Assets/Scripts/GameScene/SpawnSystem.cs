// Assets/Scripts/GameScene/SpawnSystem.cs
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
    [Header("Asset References")]
    [SerializeField] private InputReader _inputReader = default;
    [SerializeField] private VSplatter_Character _playerPrefab = default;
    [SerializeField] private TransformAnchor _playerTransformAnchor = default;
    [SerializeField] private TransformEventChannelSO _playerInstantiatedChannel = default;

    [Header("Scene Ready Event")]
    [SerializeField] private VoidEventChannelSO _onSceneReady = default;

    [Header("Sector Events")]
    [Tooltip("Current generated StartSector. Player spawn/reposition uses this sector's PlayerStartPoint.")]
    [SerializeField] private SectorRuntimeEventChannelSO _startSectorReadyEvent = default;

    [Header("Scene Refs")]
    [SerializeField] private Transform _projectilesRoot = default;

    private Transform _defaultSpawnPoint;
    private Transform _spawnedPlayer;

    private void Awake()
    {
        _defaultSpawnPoint = transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    private void OnEnable()
    {
        if (_onSceneReady != null)
            _onSceneReady.OnEventRaised += SpawnPlayer;

        if (_startSectorReadyEvent != null)
            _startSectorReadyEvent.OnEventRaised += HandleStartSectorReady;
    }

    private void OnDisable()
    {
        if (_onSceneReady != null)
            _onSceneReady.OnEventRaised -= SpawnPlayer;

        if (_startSectorReadyEvent != null)
            _startSectorReadyEvent.OnEventRaised -= HandleStartSectorReady;

        if (_playerTransformAnchor != null)
            _playerTransformAnchor.Unset();

        _spawnedPlayer = null;
    }

    private void SpawnPlayer()
    {
        if (_spawnedPlayer != null || _playerPrefab == null)
            return;

        Transform spawnLocation = ResolveSpawnLocation();

        VSplatter_Character playerInstance = Instantiate(
            _playerPrefab,
            spawnLocation.position,
            spawnLocation.rotation);

        _spawnedPlayer = playerInstance.transform;

        EntityWeaponHolder weaponHolder =
            playerInstance.GetComponentInChildren<EntityWeaponHolder>();

        if (weaponHolder != null)
            weaponHolder.SetProjectilesRoot(_projectilesRoot);

        if (_playerInstantiatedChannel != null)
            _playerInstantiatedChannel.RaiseEvent(_spawnedPlayer);

        if (_playerTransformAnchor != null)
            _playerTransformAnchor.Provide(_spawnedPlayer);

        if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }

    private void HandleStartSectorReady(SectorRuntime startSector)
    {
        if (_spawnedPlayer == null || startSector == null)
            return;

        MovePlayerTo(startSector.PlayerStartPoint);
    }

    private Transform ResolveSpawnLocation()
    {
        if (_startSectorReadyEvent != null &&
            _startSectorReadyEvent.Current != null)
        {
            return _startSectorReadyEvent.Current.PlayerStartPoint;
        }

        return _defaultSpawnPoint != null ? _defaultSpawnPoint : transform;
    }

    private void MovePlayerTo(Transform target)
    {
        if (_spawnedPlayer == null || target == null)
            return;

        CharacterController controller = _spawnedPlayer.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        _spawnedPlayer.SetPositionAndRotation(target.position, target.rotation);

        if (controller != null)
            controller.enabled = true;
    }
}
