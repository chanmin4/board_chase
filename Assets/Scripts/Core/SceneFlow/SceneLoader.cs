/*
 * Role hierarchy: Core > SceneFlow
 * Primary responsibility: Implements the focused SceneLoader responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private GameSceneSO _gameplayScene = default;
    [SerializeField] private InputReader _inputReader = default;

    [Header("Location Environments")]
    [Tooltip("The generated combat location. HomeScene is mounted before this scene so the authored Hideout is ready when StageProgressionManager initializes.")]
    [SerializeField] private GameSceneSO _runScene = default;

    [Tooltip("Static authored HomeSector scene. It stays mounted while RUNScene is active and becomes the active scene when the player returns home.")]
    [SerializeField] private GameSceneSO _homeScene = default;

    [SerializeField] private string _homeSceneName = "HomeScene";

    [Header("Listening to")]
    [SerializeField] private LoadEventChannelSO _loadMenu = default;
    [SerializeField] private LoadEventChannelSO _loadGameScene = default;
    [SerializeField] private LoadEventChannelSO _coldStartupLocation = default;

    [Header("Game Over")]
    [SerializeField] private VoidEventChannelSO _gameOverChannel;
    [SerializeField] private GameSceneSO _gameOverScene;
    [SerializeField] private float _gameOverDeathDelay = 2f;
    [SerializeField] private bool _showGameOverLoadingScreen = false;

    [Header("Loading Screen")]
    [Tooltip("PersistentManager scene에 있는 Loading Screen Canvas입니다.")]
    [SerializeField] private Canvas _loadingCanvas;

    [Tooltip("Loading Screen CanvasGroup입니다. SetActive 대신 alpha/interactable/blocksRaycasts로 제어합니다.")]
    [SerializeField] private CanvasGroup _loadingGroup;

    [Tooltip("Loading과 Click/Touch 안내를 함께 표시하는 TMP Text입니다.")]
    [SerializeField] private TextMeshProUGUI _loadingMessage;

    [Tooltip("다른 UI보다 위에 오도록 충분히 큰 sorting order를 사용합니다.")]
    [SerializeField] private int _loadingSortingOrder = 30000;

    [Tooltip("로딩 화면이 너무 번쩍하고 사라지는 것을 막기 위한 최소 표시 시간입니다.")]
    [SerializeField, Min(0f)] private float _minimumLoadingScreenSeconds = 0.35f;

    [Header("Runtime Readiness Gate")]
    [Tooltip("단순 시간 대기 대신, 생성된 맵/컨텐츠/시스템 상태가 연속으로 준비됐다고 확인해야 하는 프레임 수입니다.")]
    [FormerlySerializedAs("_waitFramesAfterSceneReady")]
    [FormerlySerializedAs("_waitFramesAfterStageBuild")]
    [SerializeField, Min(1)] private int _requiredStableRuntimeReadyFrames = 2;

    [Tooltip("맵, 컨텐츠, 시스템 또는 플레이어 준비가 이 시간 안에 끝나지 않으면 로딩 화면을 유지하고 Console에 원인을 남깁니다.")]
    [SerializeField, Min(1f)] private float _runtimeReadinessTimeoutSeconds = 12f;

    [Header("Gameplay Start Gate")]
    [Tooltip("Gameplay Location 로드가 끝나도 즉시 시작하지 않고 Click/Touch 확인을 기다립니다.")]
    [SerializeField] private bool _requireStartConfirmationForGameplay = true;

    [Tooltip("씬 또는 스테이지 데이터가 아직 준비되지 않았을 때 표시합니다.")]
    [SerializeField] private string _loadingText = "Loading...";

    [Tooltip("생성된 룸과 맵 레이아웃을 등록/검증하는 동안 표시합니다.")]
    [SerializeField] private string _mapLoadingText = "MAP LOADING...";

    [Tooltip("룸에 배치된 아이템, 루팅 오브젝트, 컨텐츠 슬롯을 초기화하는 동안 표시합니다.")]
    [SerializeField] private string _contentLoadingText = "ITEM / LOOT LOADING...";

    [Tooltip("섹터 런타임, 상호작용, 플레이어 스폰 등 게임플레이 시스템을 준비하는 동안 표시합니다.")]
    [SerializeField] private string _systemLoadingText = "SYSTEM LOADING...";

    [Tooltip("플레이어를 기준으로 카메라 위치를 확정하는 동안 표시합니다.")]
    [SerializeField] private string _cameraLoadingText = "CAMERA LOADING...";

    [Tooltip("최초 Gameplay 씬 준비가 끝나고 사용자 확인을 기다릴 때 표시합니다.")]
    [SerializeField] private string _readyToStartText = "클릭 / 터치하여 시작";

    [Tooltip("스테이지 전환 준비 완료 문구입니다. {0}은 StageDefinitionSO Display Name으로 치환됩니다.")]
    [SerializeField] private string _stageReadyFormat = "{0}\n클릭 / 터치하여 시작";

    [Tooltip("스테이지 생성에 실패했을 때 로딩 화면을 닫지 않고 표시할 오류 문구입니다.")]
    [SerializeField] private string _loadingFailedText = "Loading Failed\nCheck Console";

    [Header("Broadcasting on")]
    [Tooltip("기존 오픈프로젝트 방식 호환용입니다. SceneLoader가 직접 LoadingGroup을 제어한다면 비워도 됩니다.")]
    [SerializeField] private BoolEventChannelSO _toggleLoadingScreen = default;

    [SerializeField] private VoidEventChannelSO _onSceneReady = default;
    [SerializeField] private FadeChannelSO _fadeRequestChannel = default;

    private AsyncOperationHandle<SceneInstance> _loadingOperationHandle;
    private AsyncOperationHandle<SceneInstance> _gameplayManagerLoadingOpHandle;
    private AsyncOperationHandle<SceneInstance> _homeEnvironmentLoadingOpHandle;

    private GameSceneSO _sceneToLoad;
    private GameSceneSO _currentlyLoadedScene;
    private bool _showLoadingScreen;

    private SceneInstance _gameplayManagerSceneInstance = new SceneInstance();
    private float _fadeDuration = .5f;
    private bool _isLoading;
    private bool _raiseSceneReadyAfterLoad = true;
    private Coroutine _gameOverRoutine;
    private Coroutine _finishLoadingRoutine;
    private Coroutine _stageTransitionRoutine;
    private float _loadingScreenShownAt;
    private bool _waitingForStartConfirmation;
    private bool _pausedByLoadingGate;
    private bool _homeEnvironmentLoadInProgress;
    private Action _homeEnvironmentReadyCallbacks;

    public bool IsWaitingForStartConfirmation => _waitingForStartConfirmation;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[SceneLoader] Multiple SceneLoader instances exist.", this);
            return;
        }

        Instance = this;
        InitializeLoadingScreen();
        SetLoadingScreenVisible(false);
    }

    // Purpose: Releases owned resources before this component is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ReleaseLoadingPause();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_loadMenu != null)
            _loadMenu.OnLoadingRequested += LoadMenu;

        if (_loadGameScene != null)
            _loadGameScene.OnLoadingRequested += LoadGameScene;

        if (_gameOverChannel != null)
            _gameOverChannel.OnEventRaised += HandleGameOverRequested;

#if UNITY_EDITOR
        if (_coldStartupLocation != null)
            _coldStartupLocation.OnLoadingRequested += CurrentSceneColdStartup;
#endif
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_loadMenu != null)
            _loadMenu.OnLoadingRequested -= LoadMenu;

        if (_loadGameScene != null)
            _loadGameScene.OnLoadingRequested -= LoadGameScene;

        if (_gameOverChannel != null)
            _gameOverChannel.OnEventRaised -= HandleGameOverRequested;

#if UNITY_EDITOR
        if (_coldStartupLocation != null)
            _coldStartupLocation.OnLoadingRequested -= CurrentSceneColdStartup;
#endif
    }

#if UNITY_EDITOR
    // Purpose: Performs the current scene cold startup workflow for this component.
    private void CurrentSceneColdStartup(GameSceneSO currentlyOpenedLocation, bool showLoadingScreen, bool fadeScreen)
    {
        if (_isLoading || _waitingForStartConfirmation)
            return;

        _currentlyLoadedScene = currentlyOpenedLocation;

        if (ShouldMountHomeEnvironment(currentlyOpenedLocation))
            EnsureHomeEnvironmentLoaded(null);

        bool shouldLoadGameplayManager =
            currentlyOpenedLocation != null &&
            currentlyOpenedLocation.sceneType == GameSceneSO.GameSceneType.Location;

        _raiseSceneReadyAfterLoad = shouldLoadGameplayManager;

        if (!shouldLoadGameplayManager)
            return;

        _showLoadingScreen = showLoadingScreen || _requireStartConfirmationForGameplay;
        _isLoading = true;
        SetLoadingMessage(_loadingText);
        SetLoadingScreenVisible(true);

        if (_gameplayManagerSceneInstance.Scene == null || !_gameplayManagerSceneInstance.Scene.isLoaded)
        {
            _gameplayManagerLoadingOpHandle =
                _gameplayScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);

            _gameplayManagerLoadingOpHandle.Completed += OnColdStartupGameplayManagersLoaded;
        }
        else
        {
            BeginFinishLoadingRoutine();
        }
    }

    // Purpose: Responds to the cold startup gameplay managers loaded callback.
    private void OnColdStartupGameplayManagersLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _gameplayManagerSceneInstance = obj.Result;
        BeginFinishLoadingRoutine();
    }
#endif

    // Purpose: Loads game scene into the active runtime state.
    private void LoadGameScene(GameSceneSO gameToLoad, bool showLoadingScreen, bool fadeScreen)
    {
        if (_isLoading)
            return;

        RunResult.Clear();

        _sceneToLoad = gameToLoad;
        _showLoadingScreen = showLoadingScreen || _requireStartConfirmationForGameplay;
        _raiseSceneReadyAfterLoad = true;
        _isLoading = true;

        if (_showLoadingScreen)
        {
            SetLoadingMessage(_loadingText);
            SetLoadingScreenVisible(true);
        }

        bool needsGameplayManager = ShouldLoadGameplayManager(gameToLoad);

        if (needsGameplayManager &&
            (_gameplayManagerSceneInstance.Scene == null || !_gameplayManagerSceneInstance.Scene.isLoaded))
        {
            _gameplayManagerLoadingOpHandle =
                _gameplayScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);

            _gameplayManagerLoadingOpHandle.Completed += OnGameplayManagersLoaded;
        }
        else
        {
            StartCoroutine(UnloadPreviousScene());
        }
    }

    // Purpose: Performs the should load gameplay manager workflow for this component.
    private bool ShouldLoadGameplayManager(GameSceneSO scene)
    {
        return scene != null &&
            scene.sceneType == GameSceneSO.GameSceneType.Location;
    }

    // Purpose: Responds to the gameplay managers loaded callback.
    private void OnGameplayManagersLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _gameplayManagerSceneInstance = _gameplayManagerLoadingOpHandle.Result;
        StartCoroutine(UnloadPreviousScene());
    }

    // Purpose: Loads menu into the active runtime state.
    private void LoadMenu(GameSceneSO menuToLoad, bool showLoadingScreen, bool fadeScreen)
    {
        if (_isLoading)
            return;

        _sceneToLoad = menuToLoad;
        _showLoadingScreen = showLoadingScreen;
        _raiseSceneReadyAfterLoad = false;
        _isLoading = true;

        if (_showLoadingScreen)
        {
            SetLoadingMessage(_loadingText);
            SetLoadingScreenVisible(true);
        }

        if (_gameplayManagerSceneInstance.Scene != null &&
            _gameplayManagerSceneInstance.Scene.isLoaded &&
            _gameplayManagerLoadingOpHandle.IsValid())
        {
            Addressables.UnloadSceneAsync(_gameplayManagerLoadingOpHandle, true);
            _gameplayManagerSceneInstance = new SceneInstance();
        }

        StartCoroutine(UnloadPreviousScene());
    }

    // Purpose: Handles the incoming game over requested request or event.
    private void HandleGameOverRequested()
    {
        if (_isLoading || _gameOverRoutine != null)
            return;

        _gameOverRoutine = StartCoroutine(LoadGameOverSceneRoutine());
    }

    // Purpose: Loads game over scene routine into the active runtime state.
    private IEnumerator LoadGameOverSceneRoutine()
    {
        if (_gameOverScene == null)
        {
            Debug.LogError("[SceneLoader] GameOverScene is missing.", this);
            _gameOverRoutine = null;
            yield break;
        }

        _isLoading = true;
        _raiseSceneReadyAfterLoad = false;
        _sceneToLoad = _gameOverScene;
        _showLoadingScreen = _showGameOverLoadingScreen;

        if (_showLoadingScreen)
        {
            SetLoadingMessage(_loadingText);
            SetLoadingScreenVisible(true);
        }

        if (_inputReader != null)
            _inputReader.DisableAllInput();

        if (_gameOverDeathDelay > 0f)
            yield return new WaitForSeconds(_gameOverDeathDelay);

        if (_fadeRequestChannel != null)
            _fadeRequestChannel.FadeOut(_fadeDuration);

        yield return new WaitForSeconds(_fadeDuration);

        if (_gameplayManagerSceneInstance.Scene != null &&
            _gameplayManagerSceneInstance.Scene.isLoaded &&
            _gameplayManagerLoadingOpHandle.IsValid())
        {
            Addressables.UnloadSceneAsync(_gameplayManagerLoadingOpHandle, true);
            _gameplayManagerSceneInstance = new SceneInstance();
        }

        if (_currentlyLoadedScene != null)
        {
            if (_currentlyLoadedScene.sceneReference.OperationHandle.IsValid())
            {
                _currentlyLoadedScene.sceneReference.UnLoadScene();
            }
#if UNITY_EDITOR
            else
            {
                SceneManager.UnloadSceneAsync(_currentlyLoadedScene.sceneReference.editorAsset.name);
            }
#endif
        }

        UnloadHomeEnvironment();

        LoadNewScene();
        _gameOverRoutine = null;
    }

    // Purpose: Performs the unload previous scene workflow for this component.
    private IEnumerator UnloadPreviousScene()
    {
        if (_inputReader != null)
            _inputReader.DisableAllInput();

        if (_fadeRequestChannel != null)
            _fadeRequestChannel.FadeOut(_fadeDuration);

        yield return new WaitForSeconds(_fadeDuration);

        if (!ShouldMountHomeEnvironment(_sceneToLoad))
            UnloadHomeEnvironment();

        if (_currentlyLoadedScene != null)
        {
            if (_currentlyLoadedScene.sceneReference.OperationHandle.IsValid())
            {
                _currentlyLoadedScene.sceneReference.UnLoadScene();
            }
#if UNITY_EDITOR
            else
            {
                SceneManager.UnloadSceneAsync(_currentlyLoadedScene.sceneReference.editorAsset.name);
            }
#endif
        }

        LoadNewScene();
    }

    // Purpose: Loads new scene into the active runtime state.
    private void LoadNewScene()
    {
        if (_sceneToLoad == null)
        {
            Debug.LogError("[SceneLoader] SceneToLoad is null.", this);
            _isLoading = false;

            if (_showLoadingScreen)
                SetLoadingScreenVisible(false);

            return;
        }

        if (_showLoadingScreen)
        {
            SetLoadingMessage(_loadingText);
            SetLoadingScreenVisible(true);
        }

        if (ShouldMountHomeEnvironment(_sceneToLoad))
        {
            EnsureHomeEnvironmentLoaded(LoadRequestedScene);
            return;
        }

        LoadRequestedScene();
    }

    // Purpose: Loads requested scene into the active runtime state.
    private void LoadRequestedScene()
    {
        _loadingOperationHandle = _sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true, 0);
        _loadingOperationHandle.Completed += OnNewSceneLoaded;
    }

    // Purpose: Performs the should mount home environment workflow for this component.
    private bool ShouldMountHomeEnvironment(GameSceneSO scene)
    {
        return scene != null && scene == _runScene && _homeScene != null;
    }

    /// <summary>
    /// Lets a direct RUNScene editor launch request the same authored HomeScene
    /// setup as the normal menu loading flow. Calling this repeatedly is safe:
    /// the existing in-progress operation and callbacks are reused.
    /// </summary>
    public bool EnsureHomeEnvironmentForRun()
    {
        if (_homeScene == null)
            return false;

        EnsureHomeEnvironmentLoaded(null);
        return true;
    }

    // Purpose: Ensures home environment loaded is ready before the workflow continues.
    private void EnsureHomeEnvironmentLoaded(Action onReady)
    {
        if (_homeScene == null)
        {
            onReady?.Invoke();
            return;
        }

        if (IsHomeEnvironmentLoaded())
        {
            onReady?.Invoke();
            return;
        }

        if (onReady != null)
            _homeEnvironmentReadyCallbacks += onReady;

        if (_homeEnvironmentLoadInProgress)
            return;

        _homeEnvironmentLoadInProgress = true;
        _homeEnvironmentLoadingOpHandle =
            _homeScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
        _homeEnvironmentLoadingOpHandle.Completed += OnHomeEnvironmentLoaded;
    }

    // Purpose: Responds to the home environment loaded callback.
    private void OnHomeEnvironmentLoaded(AsyncOperationHandle<SceneInstance> handle)
    {
        _homeEnvironmentLoadInProgress = false;
        Action callbacks = _homeEnvironmentReadyCallbacks;
        _homeEnvironmentReadyCallbacks = null;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[SceneLoader] Failed to load the authored HomeScene environment.", this);
            _isLoading = false;
            SetLoadingScreenVisible(false);
            return;
        }

        callbacks?.Invoke();
    }

    // Purpose: Checks whether home environment loaded is currently true.
    private bool IsHomeEnvironmentLoaded()
    {
        return !string.IsNullOrWhiteSpace(_homeSceneName) &&
               SceneManager.GetSceneByName(_homeSceneName).isLoaded;
    }

    // Purpose: Performs the unload home environment workflow for this component.
    private void UnloadHomeEnvironment()
    {
        if (!IsHomeEnvironmentLoaded())
            return;

        if (_homeEnvironmentLoadingOpHandle.IsValid() &&
            _homeEnvironmentLoadingOpHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Addressables.UnloadSceneAsync(_homeEnvironmentLoadingOpHandle, true);
        }
        else
        {
            SceneManager.UnloadSceneAsync(_homeSceneName);
        }

        _homeEnvironmentLoadingOpHandle = default;
    }

    // Purpose: Responds to the new scene loaded callback.
    private void OnNewSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _currentlyLoadedScene = _sceneToLoad;

        Scene scene = obj.Result.Scene;
        SceneManager.SetActiveScene(scene);
        LightProbes.TetrahedralizeAsync();

        BeginFinishLoadingRoutine();
    }

    // Purpose: Begins the finish loading routine workflow.
    private void BeginFinishLoadingRoutine()
    {
        if (_finishLoadingRoutine != null)
            StopCoroutine(_finishLoadingRoutine);

        _finishLoadingRoutine = StartCoroutine(FinishSceneLoadRoutine());
    }

    // Purpose: Finishes the active scene load routine workflow.
    private IEnumerator FinishSceneLoadRoutine()
    {
        bool requiresConfirmation =
            _raiseSceneReadyAfterLoad &&
            _requireStartConfirmationForGameplay;

        if (requiresConfirmation)
        {
            _inputReader?.DisableAllInput();
            AcquireLoadingPause();
        }

        if (_raiseSceneReadyAfterLoad)
        {
            bool runtimeReady = false;
            yield return WaitForRuntimeReadiness(
                requirePlayer: false,
                context: "initial scene load",
                completed: result => runtimeReady = result);

            if (!runtimeReady)
            {
                HandleRuntimeReadinessFailure("initial scene load");
                yield break;
            }
        }

        // The player must exist and the camera must already frame that player
        // before the loading screen can offer its Click/Touch confirmation.
        // SpawnSystem enables input as part of its legacy spawn contract, so
        // immediately keep it disabled until the loading gate is confirmed.
        if (_raiseSceneReadyAfterLoad)
        {
            SetLoadingMessage(_systemLoadingText);
            StartGameplay();
            _inputReader?.DisableAllInput();

            bool playerReady = false;
            yield return WaitForPlayerReadiness(
                context: "initial player spawn",
                completed: result => playerReady = result);

            if (!playerReady)
            {
                HandleRuntimeReadinessFailure("initial player spawn");
                yield break;
            }

            // Let the spawned player finish its first frame before resolving
            // the camera, then snap while the loading overlay still covers it.
            SetLoadingMessage(_cameraLoadingText);
            yield return new WaitForEndOfFrame();
            SnapCameraToPlayer();
            yield return new WaitForEndOfFrame();
        }

        if (_showLoadingScreen)
        {
            float elapsed = Time.unscaledTime - _loadingScreenShownAt;
            float remaining = _minimumLoadingScreenSeconds - elapsed;

            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);
        }

        if (requiresConfirmation && CanPresentStartConfirmation())
        {
            SetLoadingMessage(_readyToStartText);
            _waitingForStartConfirmation = true;
            yield return WaitForStartConfirmation();
            _waitingForStartConfirmation = false;
        }
        else if (requiresConfirmation)
        {
            Debug.LogError(
                "[SceneLoader] Gameplay start confirmation requires both Loading CanvasGroup and Loading Message. Starting immediately to avoid an invisible input lock.",
                this);
        }

        if (_raiseSceneReadyAfterLoad)
        {
            ReleaseLoadingPause();
            _inputReader?.EnableGameplayInput();
        }

        if (_fadeRequestChannel != null)
            _fadeRequestChannel.FadeIn(_fadeDuration);

        if (_showLoadingScreen)
            SetLoadingScreenVisible(false);

        _isLoading = false;
        _finishLoadingRoutine = null;
    }

    // Purpose: Requests stage transition from the owning service.
    public bool RequestStageTransition(
        string stageDisplayName,
        Func<bool> buildStage,
        Action onConfirmed)
    {
        if (buildStage == null)
        {
            Debug.LogError("[SceneLoader] Stage transition requires a build callback.", this);
            return false;
        }

        if (_isLoading || _stageTransitionRoutine != null)
        {
            Debug.LogError("[SceneLoader] Another loading request is already active.", this);
            return false;
        }

        if (!CanPresentStartConfirmation())
        {
            Debug.LogError(
                "[SceneLoader] Stage transition requires both Loading CanvasGroup and Loading Message.",
                this);
            return false;
        }

        _stageTransitionRoutine = StartCoroutine(
            StageTransitionRoutine(stageDisplayName, buildStage, onConfirmed));
        return true;
    }

    // Purpose: Performs the stage transition routine workflow for this component.
    private IEnumerator StageTransitionRoutine(
        string stageDisplayName,
        Func<bool> buildStage,
        Action onConfirmed)
    {
        _isLoading = true;
        _showLoadingScreen = true;
        _inputReader?.DisableAllInput();
        AcquireLoadingPause();
        SetLoadingMessage(_mapLoadingText);
        SetLoadingScreenVisible(true);

        // Render one fully covered frame before replacing the generated stage.
        yield return null;

        bool built = false;

        try
        {
            built = buildStage();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (!built)
        {
            SetLoadingMessage(_loadingFailedText);
            Debug.LogError(
                $"[SceneLoader] Stage build failed. stage={stageDisplayName}",
                this);
            _stageTransitionRoutine = null;
            yield break;
        }

        bool runtimeReady = false;
        yield return WaitForRuntimeReadiness(
            requirePlayer: true,
            context: $"stage transition '{stageDisplayName}'",
            completed: result => runtimeReady = result);

        if (!runtimeReady)
        {
            HandleRuntimeReadinessFailure($"stage transition '{stageDisplayName}'");
            _stageTransitionRoutine = null;
            yield break;
        }

        SetLoadingMessage(_cameraLoadingText);
        yield return new WaitForEndOfFrame();
        SnapCameraToPlayer();
        yield return new WaitForEndOfFrame();

        float elapsed = Time.unscaledTime - _loadingScreenShownAt;
        float remaining = _minimumLoadingScreenSeconds - elapsed;

        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        string readyText = string.IsNullOrWhiteSpace(stageDisplayName)
            ? _readyToStartText
            : SafeFormatStageReadyText(stageDisplayName);

        SetLoadingMessage(readyText);
        _waitingForStartConfirmation = true;
        yield return WaitForStartConfirmation();
        _waitingForStartConfirmation = false;

        ReleaseLoadingPause();
        onConfirmed?.Invoke();

        if (_fadeRequestChannel != null)
            _fadeRequestChannel.FadeIn(_fadeDuration);

        SetLoadingScreenVisible(false);
        _inputReader?.EnableGameplayInput();
        _isLoading = false;
        _stageTransitionRoutine = null;
    }

    // Purpose: Begins the gameplay workflow.
    private void StartGameplay()
    {
        if (_onSceneReady != null)
            _onSceneReady.RaiseEvent();
    }

    // Purpose: Snaps the active gameplay camera to its player target while the loading overlay is still visible.
    private static void SnapCameraToPlayer()
    {
        CameraManager cameraManager = FindAnyObjectByType<CameraManager>();

        if (cameraManager != null)
            cameraManager.SnapToTarget();
    }

    // Purpose: Waits until the generated map, synchronous content placement, and sector runtime registration are all valid.
    private IEnumerator WaitForRuntimeReadiness(
        bool requirePlayer,
        string context,
        Action<bool> completed)
    {
        float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, _runtimeReadinessTimeoutSeconds);

        SetLoadingMessage(_mapLoadingText);

        while (!HasGeneratedMapReady(out _))
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                ReportRuntimeReadinessTimeout(context, requirePlayer);
                completed?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        // SectorContentSlotSpawner.Populate is synchronous. One rendered frame
        // here lets every newly-instantiated loot/content prefab complete Awake
        // and Start before the sector system is declared ready.
        SetLoadingMessage(_contentLoadingText);
        yield return null;

        SetLoadingMessage(_systemLoadingText);
        int stableFrames = 0;
        int requiredFrames = Mathf.Max(1, _requiredStableRuntimeReadyFrames);

        while (stableFrames < requiredFrames)
        {
            if (IsGameplayRuntimeReady(requirePlayer, out _))
            {
                stableFrames++;
            }
            else
            {
                stableFrames = 0;
            }

            if (stableFrames >= requiredFrames)
                break;

            if (Time.realtimeSinceStartup >= deadline)
            {
                ReportRuntimeReadinessTimeout(context, requirePlayer);
                completed?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        completed?.Invoke(true);
    }

    // Purpose: Waits for the player instance created by the scene-ready event before allowing camera setup.
    private IEnumerator WaitForPlayerReadiness(string context, Action<bool> completed)
    {
        float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, _runtimeReadinessTimeoutSeconds);

        while (!HasActivePlayer())
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                ReportRuntimeReadinessTimeout(context, requirePlayer: true);
                completed?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        completed?.Invoke(true);
    }

    // Purpose: Checks whether the generated stage layout has a matching set of registered runtime sectors.
    private static bool HasGeneratedMapReady(out string diagnostic)
    {
        SectorStateManager sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (sectorStateManager == null)
        {
            diagnostic = "SectorStateManager is missing.";
            return false;
        }

        StageMapLayout layout = sectorStateManager.CurrentStageMapLayout;

        if (layout == null || layout.rooms == null)
        {
            diagnostic = "Generated StageMapLayout is missing.";
            return false;
        }

        int expectedSectorCount = 0;

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            StageRoomNode room = layout.rooms[i];

            if (room != null && room.roomType != StageRoomType.Empty)
                expectedSectorCount++;
        }

        if (expectedSectorCount <= 0)
        {
            diagnostic = "Generated StageMapLayout contains no playable rooms.";
            return false;
        }

        int registeredSectorCount = sectorStateManager.Sectors.Count;

        if (registeredSectorCount != expectedSectorCount)
        {
            diagnostic =
                $"Registered sector count is {registeredSectorCount}, expected {expectedSectorCount}.";
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    // Purpose: Checks that all generated rooms, runtime references, and optionally the player are available for gameplay.
    private static bool IsGameplayRuntimeReady(bool requirePlayer, out string diagnostic)
    {
        if (!HasGeneratedMapReady(out diagnostic))
            return false;

        SectorStateManager sectorStateManager = FindAnyObjectByType<SectorStateManager>();

        if (sectorStateManager.StartSector == null)
        {
            diagnostic = "StartSector has not been registered.";
            return false;
        }

        if (sectorStateManager.CurrentSector == null)
        {
            diagnostic = "CurrentSector has not been assigned.";
            return false;
        }

        for (int i = 0; i < sectorStateManager.Sectors.Count; i++)
        {
            SectorRuntime sector = sectorStateManager.Sectors[i];

            if (sector == null)
            {
                diagnostic = $"Registered sector {i} is missing.";
                return false;
            }

            if (!sector.gameObject.scene.isLoaded || !sector.gameObject.activeInHierarchy)
            {
                diagnostic = $"Registered sector '{sector.name}' is not active in a loaded scene.";
                return false;
            }
        }

        if (requirePlayer && !HasActivePlayer())
        {
            diagnostic = "PlayerCharacter has not been spawned or is inactive.";
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    // Purpose: Checks whether the active player instance required by the gameplay camera exists.
    private static bool HasActivePlayer()
    {
        PlayerCharacter player = FindAnyObjectByType<PlayerCharacter>();
        return player != null && player.isActiveAndEnabled && player.gameObject.activeInHierarchy;
    }

    // Purpose: Emits a targeted readiness timeout diagnostic while keeping the loading overlay active.
    private void ReportRuntimeReadinessTimeout(string context, bool requirePlayer)
    {
        bool ready = IsGameplayRuntimeReady(requirePlayer, out string diagnostic);

        Debug.LogError(
            $"[SceneLoader] Runtime readiness timed out. context={context}, " +
            $"requirePlayer={requirePlayer}, ready={ready}, reason={diagnostic}",
            this);
    }

    // Purpose: Holds the loading screen on a readiness failure so an incomplete runtime never becomes playable.
    private void HandleRuntimeReadinessFailure(string context)
    {
        SetLoadingMessage(_loadingFailedText);
        Debug.LogError(
            $"[SceneLoader] Loading gate remains closed because runtime preparation failed. context={context}",
            this);
    }

    // Purpose: Initializes loading screen for use by this component.
    private void InitializeLoadingScreen()
    {
        if (_loadingCanvas != null)
        {
            _loadingCanvas.overrideSorting = true;
            _loadingCanvas.sortingOrder = _loadingSortingOrder;
        }

        ResolveLoadingMessage();
    }

    // Purpose: Checks whether present start confirmation is currently permitted.
    private bool CanPresentStartConfirmation()
    {
        ResolveLoadingMessage();
        return _loadingGroup != null && _loadingMessage != null;
    }

    // Purpose: Resolves the loading message needed by this component.
    private void ResolveLoadingMessage()
    {
        if (_loadingMessage == null && _loadingCanvas != null)
            _loadingMessage = _loadingCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    // Purpose: Updates the loading message state.
    private void SetLoadingMessage(string message)
    {
        ResolveLoadingMessage();

        if (_loadingMessage != null)
            _loadingMessage.text = message ?? string.Empty;
    }

    // Purpose: Performs the wait for start confirmation workflow for this component.
    private IEnumerator WaitForStartConfirmation()
    {
        // The click that requested loading must be released before it can confirm start.
        while (IsStartConfirmationHeld())
            yield return null;

        while (!WasStartConfirmationPressedThisFrame())
            yield return null;
    }

    // Purpose: Checks whether start confirmation held is currently true.
    private static bool IsStartConfirmationHeld()
    {
        return (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
               (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) ||
               (Keyboard.current != null &&
                (Keyboard.current.enterKey.isPressed || Keyboard.current.spaceKey.isPressed)) ||
               (Gamepad.current != null &&
                (Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed));
    }

    // Purpose: Performs the was start confirmation pressed this frame workflow for this component.
    private static bool WasStartConfirmationPressedThisFrame()
    {
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
               (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
               (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
               (Gamepad.current != null &&
                (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
    }

    // Purpose: Performs the acquire loading pause workflow for this component.
    private void AcquireLoadingPause()
    {
        if (GamePause.IsPaused)
            return;

        GamePause.On();
        _pausedByLoadingGate = true;
    }

    // Purpose: Releases the owned loading pause resource.
    private void ReleaseLoadingPause()
    {
        if (!_pausedByLoadingGate)
            return;

        _pausedByLoadingGate = false;
        GamePause.Off();
    }

    // Purpose: Performs the safe format stage ready text workflow for this component.
    private string SafeFormatStageReadyText(string stageDisplayName)
    {
        try
        {
            return string.Format(_stageReadyFormat, stageDisplayName);
        }
        catch (FormatException)
        {
            return $"{stageDisplayName}\n{_readyToStartText}";
        }
    }

    // Purpose: Updates the loading screen visible state.
    private void SetLoadingScreenVisible(bool visible)
    {
        if (visible)
            _loadingScreenShownAt = Time.unscaledTime;

        if (_loadingGroup != null)
        {
            _loadingGroup.alpha = visible ? 1f : 0f;
            _loadingGroup.interactable = visible;
            _loadingGroup.blocksRaycasts = visible;
        }

        if (_toggleLoadingScreen != null)
            _toggleLoadingScreen.RaiseEvent(visible);
    }

    // Purpose: Exits the active game state or area.
    private void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exit!");
    }
}
