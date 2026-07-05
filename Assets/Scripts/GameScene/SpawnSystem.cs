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

    [Header("Placement")]
    [SerializeField] private bool _parentPlayerToCurrentSector = true;
    [SerializeField, Min(0f)] private float _spawnYOffset = 0.05f;
    [SerializeField] private bool _useCharacterControllerSpawnHeight = true;

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

        SectorRuntime startSector = ResolveStartSector();
        Transform spawnLocation = ResolveSpawnLocation(startSector);
        Transform spawnParent = ResolveSpawnParent(startSector);

        VSplatter_Character playerInstance = Instantiate(
            _playerPrefab,
            ResolveSpawnPosition(spawnLocation, _playerPrefab.GetComponent<CharacterController>()),
            spawnLocation.rotation,
            spawnParent);

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

        MovePlayerTo(startSector.PlayerStartPoint, ResolveSpawnParent(startSector));
    }

    private SectorRuntime ResolveStartSector()
    {
        return _startSectorReadyEvent != null
            ? _startSectorReadyEvent.Current
            : null;
    }

    private Transform ResolveSpawnLocation(SectorRuntime startSector)
    {
        if (startSector != null)
            return startSector.PlayerStartPoint;

        return _defaultSpawnPoint != null ? _defaultSpawnPoint : transform;
    }

    private Transform ResolveSpawnParent(SectorRuntime startSector)
    {
        if (!_parentPlayerToCurrentSector || startSector == null)
            return null;

        return startSector.transform;
    }

    private void MovePlayerTo(Transform target, Transform parent)
    {
        if (_spawnedPlayer == null || target == null)
            return;

        CharacterController controller = _spawnedPlayer.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        if (_parentPlayerToCurrentSector)
            _spawnedPlayer.SetParent(parent, worldPositionStays: true);

        _spawnedPlayer.SetPositionAndRotation(
            ResolveSpawnPosition(target, controller),
            target.rotation);

        if (controller != null)
            controller.enabled = true;
    }
    private Vector3 ResolveSpawnPosition(Transform target, CharacterController controller)
    {
        float yOffset = Mathf.Max(0f, _spawnYOffset);

        if (_useCharacterControllerSpawnHeight && controller != null)
        {
            float feetToTransform =
                Mathf.Max(0f, controller.height * 0.5f - controller.center.y);

            yOffset = Mathf.Max(yOffset, feetToTransform + Mathf.Max(0f, controller.skinWidth));
        }

        return target.position + Vector3.up * yOffset;
    }
}
