/*
 * Role hierarchy: Gameplay
 * Primary responsibility: Manages SpawnSystem stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Assets/Scripts/GameScene/SpawnSystem.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnSystem : MonoBehaviour
{
    [Header("Asset References")]
    [SerializeField] private InputReader _inputReader = default;
    [SerializeField] private PlayerCharacter _playerPrefab = default;
    [SerializeField] private TransformAnchor _playerTransformAnchor = default;
    [SerializeField] private TransformEventChannelSO _playerInstantiatedChannel = default;

    [Header("Scene Ready Event")]
    [SerializeField] private VoidEventChannelSO _onSceneReady = default;

    [Header("Sector Events")]
    [Tooltip("Current generated StartSector. Player spawn/reposition uses this sector's PlayerStartPoint.")]
    [SerializeField] private SectorRuntimeEventChannelSO _startSectorReadyEvent = default;
    
    [Header("Scene Refs")]
    [SerializeField] private Transform _projectilesRoot = default;
    [SerializeField] private CameraManager _cameraManager;

    [Header("Placement")]
    [SerializeField] private bool _parentPlayerToCurrentSector = true;
    [SerializeField, Min(0f)] private float _spawnYOffset = 0.05f;
    [SerializeField] private bool _useCharacterControllerSpawnHeight = true;

    private Transform _defaultSpawnPoint;
    private Transform _spawnedPlayer;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        _defaultSpawnPoint = transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_onSceneReady != null)
            _onSceneReady.OnEventRaised += SpawnPlayer;

        if (_startSectorReadyEvent != null)
            _startSectorReadyEvent.OnEventRaised += HandleStartSectorReady;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
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

    // Purpose: Spawns player for the current runtime context.
    private void SpawnPlayer()
    {
        if (_spawnedPlayer != null || _playerPrefab == null)
            return;

        SectorRuntime startSector = ResolveStartSector();
        Transform spawnLocation = ResolveSpawnLocation(startSector);
        Transform spawnParent = ResolveSpawnParent(startSector);

        PlayerCharacter playerInstance = Instantiate(
            _playerPrefab,
            ResolveSpawnPosition(spawnLocation, _playerPrefab.GetComponent<CharacterController>()),
            spawnLocation.rotation,
            spawnParent);

        _spawnedPlayer = playerInstance.transform;

        ShooterEquipmentController equipmentController =
            EntityComponentResolver.Resolve<ShooterEquipmentController>(playerInstance);

        if (equipmentController != null)
            equipmentController.SetProjectilesRoot(_projectilesRoot);

        if (_playerInstantiatedChannel != null)
            _playerInstantiatedChannel.RaiseEvent(_spawnedPlayer);

        if (_playerTransformAnchor != null)
            _playerTransformAnchor.Provide(_spawnedPlayer);

        MovePlayerToOwningScene(startSector != null ? startSector.gameObject.scene : gameObject.scene);
        BindPlayerCamera();

        if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }

    // Purpose: Handles the incoming start sector ready request or event.
    private void HandleStartSectorReady(SectorRuntime startSector)
    {
        if (_spawnedPlayer == null || startSector == null)
            return;

        MovePlayerTo(startSector.PlayerStartPoint, ResolveSpawnParent(startSector));
    }

    // Purpose: Resolves the start sector needed by this component.
    private SectorRuntime ResolveStartSector()
    {
        return _startSectorReadyEvent != null
            ? _startSectorReadyEvent.Current
            : null;
    }

    // Purpose: Resolves the spawn location needed by this component.
    private Transform ResolveSpawnLocation(SectorRuntime startSector)
    {
        if (startSector != null)
            return startSector.PlayerStartPoint;

        return _defaultSpawnPoint != null ? _defaultSpawnPoint : transform;
    }

    // Purpose: Resolves the spawn parent needed by this component.
    private Transform ResolveSpawnParent(SectorRuntime startSector)
    {
        if (!_parentPlayerToCurrentSector || startSector == null)
            return null;

        return startSector.transform;
    }

    // Purpose: Moves player to toward its requested destination.
    private void MovePlayerTo(Transform target, Transform parent)
    {
        if (_spawnedPlayer == null || target == null)
            return;

        CharacterController controller = _spawnedPlayer.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        if (_parentPlayerToCurrentSector)
            _spawnedPlayer.SetParent(parent, worldPositionStays: true);

        MovePlayerToOwningScene(target.gameObject.scene);

        _spawnedPlayer.SetPositionAndRotation(
            ResolveSpawnPosition(target, controller),
            target.rotation);

        if (controller != null)
            controller.enabled = true;

        BindPlayerCamera();
    }

    // Ownership follows the current room even when parenting is disabled in the Inspector.
    private void MovePlayerToOwningScene(Scene scene)
    {
        if (_spawnedPlayer == null || !scene.IsValid() || !scene.isLoaded ||
            _spawnedPlayer.gameObject.scene == scene) return;
        Transform previousParent = _spawnedPlayer.parent;
        _spawnedPlayer.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(_spawnedPlayer.gameObject, scene);
        if (previousParent != null && previousParent.gameObject.scene == scene)
            _spawnedPlayer.SetParent(previousParent, true);
    }

    private void BindPlayerCamera()
    {
        if (_cameraManager == null) _cameraManager = FindFirstObjectByType<CameraManager>();
        if (_cameraManager != null) _cameraManager.SetFollowTarget(_spawnedPlayer, instant: true);
    }
    // Purpose: Resolves the spawn position needed by this component.
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
