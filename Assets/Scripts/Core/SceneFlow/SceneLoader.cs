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

    [Tooltip("Authored HomeSector scene. Unloaded after deployment and reloaded before returning home; RUNScene managers remain alive.")]
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
    [SerializeField, Min(1f)] private float _runtimeReadinessTimeoutSeconds = 20f;
    [Tooltip("준비 상태에 처음 도달한 뒤 연속 준비 프레임 확인에 주는 추가 유예시간입니다. 준비 실패를 무시하고 로딩을 열지는 않습니다.")]
    [SerializeField, Min(0.1f)] private float _runtimeReadinessGraceSeconds = 3f;
    [Tooltip("5초마다 대기 조건을 기록하고, 준비 완료 시 경과 시간을 기록합니다. 타임아웃 상세 오류는 이 옵션과 무관하게 출력됩니다.")]
    [SerializeField] private bool _logRuntimeReadinessDiagnostics = true;

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
    private bool _homeEnvironmentUnloadInProgress;
    private bool _homeLoadAfterUnload;
    private Coroutine _homeTransitionRoutine;

    public bool IsHomeEnvironmentUnloading => _homeEnvironmentUnloadInProgress;

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

        if (_homeEnvironmentUnloadInProgress)
        {
            if (onReady != null) _homeEnvironmentReadyCallbacks += onReady;
            _homeLoadAfterUnload = true;
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
            if (_homeTransitionRoutine != null) return; // The return gate handles failure without uncovering gameplay.
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
    private void UnloadHomeEnvironment(bool preservePlayer = false)
    {
        if (_homeEnvironmentUnloadInProgress || !IsHomeEnvironmentLoaded())
            return;

        _homeEnvironmentUnloadInProgress = true;
        StartCoroutine(UnloadHomeEnvironmentRoutine(preservePlayer));
    }

    private IEnumerator UnloadHomeEnvironmentRoutine(bool preservePlayer)
    {
        Scene home = SceneManager.GetSceneByName(_homeSceneName);
        if (preservePlayer && !CanUnloadHomeEnvironment(home, FindObjectsByType<PlayerCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None)))
        {
            _homeEnvironmentUnloadInProgress = false;
            Debug.LogError("[SceneLoader] Home unload refused: move the live player to RUNScene first.", this);
            yield break;
        }

        if (_homeEnvironmentLoadingOpHandle.IsValid() &&
            _homeEnvironmentLoadingOpHandle.Status == AsyncOperationStatus.Succeeded)
        {
            var operation = Addressables.UnloadSceneAsync(_homeEnvironmentLoadingOpHandle, true);
            yield return operation;
        }
        else
        {
            yield return SceneManager.UnloadSceneAsync(_homeSceneName);
        }

        _homeEnvironmentLoadingOpHandle = default;
        _homeEnvironmentUnloadInProgress = false;
        if (_homeLoadAfterUnload)
        {
            _homeLoadAfterUnload = false;
            EnsureHomeEnvironmentLoaded(null);
        }
    }

    public static bool CanUnloadHomeEnvironment(Scene home, PlayerCharacter[] players)
    {
        if (!home.IsValid() || !home.isLoaded) return false;
        foreach (var player in players)
            if (player != null && player.gameObject.scene == home) return false;
        return true;
    }

    public void ReleaseHomeEnvironmentForRun()
    {
        SectorStateManager states = FindFirstObjectByType<SectorStateManager>();
        if (states == null || states.CurrentSector == null || !states.IsManagedSector(states.CurrentSector) ||
            states.CurrentSector.gameObject.scene.name == _homeSceneName) return;
        UnloadHomeEnvironment(preservePlayer: true);
    }

    private bool HasReleasedHomeForRun()
    {
        SectorStateManager states = FindFirstObjectByType<SectorStateManager>();
        return states == null || states.CurrentSector == null || !states.IsManagedSector(states.CurrentSector) ||
            !IsHomeEnvironmentLoaded();
    }

    // Return uses the existing loading overlay, but does not require another start-confirmation click.
    public bool RequestHomeTransition(Func<bool> enterHome, Action<bool> completed)
    {
        if (_isLoading || _homeTransitionRoutine != null || enterHome == null || _homeScene == null ||
            _loadingGroup == null) return false;
        _homeTransitionRoutine = StartCoroutine(HomeTransitionRoutine(enterHome, completed));
        return true;
    }

    private IEnumerator HomeTransitionRoutine(Func<bool> enterHome, Action<bool> completed)
    {
        _isLoading = true;
        _inputReader?.DisableAllInput();
        AcquireLoadingPause();
        SetLoadingMessage(_loadingText);
        SetLoadingScreenVisible(true);
        yield return null;
        float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, _runtimeReadinessTimeoutSeconds);
        while (_homeEnvironmentUnloadInProgress && Time.realtimeSinceStartup < deadline) yield return null;
        if (_homeEnvironmentUnloadInProgress)
        {
            HandleRuntimeReadinessFailure("waiting for HomeScene unload");
            _homeTransitionRoutine = null;
            completed?.Invoke(false);
            yield break;
        }
        EnsureHomeEnvironmentLoaded(null);
        deadline = Time.realtimeSinceStartup + Mathf.Max(1f, _runtimeReadinessTimeoutSeconds);
        while (_homeEnvironmentLoadInProgress && Time.realtimeSinceStartup < deadline) yield return null;
        bool entered = false;
        try { if (!_homeEnvironmentLoadInProgress && IsHomeEnvironmentLoaded() && HasActivePlayer()) entered = enterHome(); }
        catch (Exception error) { Debug.LogException(error, this); }
        if (!entered)
        {
            HandleRuntimeReadinessFailure("return to HomeScene");
            _homeTransitionRoutine = null;
            completed?.Invoke(false);
            yield break;
        }
        SetLoadingMessage(_cameraLoadingText);
        yield return new WaitForEndOfFrame();
        SnapCameraToPlayer();
        yield return new WaitForEndOfFrame();
        ReleaseLoadingPause();
        SetLoadingScreenVisible(false);
        _isLoading = false;
        _homeTransitionRoutine = null;
        completed?.Invoke(true);
        _inputReader?.EnableGameplayInput();
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
                completed: result => runtimeReady = result,
                allowInitialHideout: true);

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
            ReleaseHomeEnvironmentForRun();
            while (_homeEnvironmentUnloadInProgress) yield return null;
            if (!HasReleasedHomeForRun())
            { HandleRuntimeReadinessFailure("initial raid HomeScene unload"); yield break; }
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

        ReleaseHomeEnvironmentForRun();
        while (_homeEnvironmentUnloadInProgress) yield return null;
        if (!HasReleasedHomeForRun())
        {
            HandleRuntimeReadinessFailure("deployment HomeScene unload");
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

    // Purpose: Waits for the selected initial environment or a generated deployment map.
    private IEnumerator WaitForRuntimeReadiness(
        bool requirePlayer,
        string context,
        Action<bool> completed,
        bool allowInitialHideout = false)
    {
        float started = Time.realtimeSinceStartup;
        float timeout = Mathf.Max(1f, _runtimeReadinessTimeoutSeconds);
        float deadline = started + timeout;
        float lastPoll = started;
        float longestPollGap = 0f;
        float nextProgressLog = started + 5f;
        string lastNotReadyReason = "No unready condition observed.";

        SetLoadingMessage(_mapLoadingText);
        while (true)
        {
            float now = Time.realtimeSinceStartup;
            longestPollGap = Mathf.Max(longestPollGap, now - lastPoll);
            lastPoll = now;
            bool environmentReady = HasRuntimeEnvironmentReady(
                FindAnyObjectByType<SectorStateManager>(),
                FindAnyObjectByType<StageProgressionManager>(FindObjectsInactive.Include),
                allowInitialHideout, out _, out string reason);
            if (environmentReady) break;
            lastNotReadyReason = reason;
            LogReadinessWait(context, "environment", now - started, reason, ref nextProgressLog);
            if (now >= deadline)
            {
                ReportRuntimeReadinessTimeout(context, requirePlayer, allowInitialHideout,
                    "environment", now - started, 0, 0, lastNotReadyReason, longestPollGap);
                completed?.Invoke(false);
                yield break;
            }
            yield return null;
        }
        float environmentElapsed = Time.realtimeSinceStartup - started;
        if (_logRuntimeReadinessDiagnostics)
            Debug.Log($"[SceneLoader] Environment ready. context={context}, elapsed={environmentElapsed:0.000}s", this);

        // SectorContentSlotSpawner.Populate is synchronous. One rendered frame
        // here lets every newly-instantiated loot/content prefab complete Awake
        // and Start before the sector system is declared ready.
        SetLoadingMessage(_contentLoadingText);
        yield return null;

        SetLoadingMessage(_systemLoadingText);
        int stableFrames = 0;
        int requiredFrames = Mathf.Max(1, _requiredStableRuntimeReadyFrames);
        float systemStarted = Time.realtimeSinceStartup;
        // Give content's first rendered frame a fresh, bounded stabilization window.
        float stabilizationDeadline = Mathf.Max(deadline,
            systemStarted + Mathf.Max(0.1f, _runtimeReadinessGraceSeconds));
        bool grantedReadyGrace = false;
        int stableResets = 0;

        while (stableFrames < requiredFrames)
        {
            float now = Time.realtimeSinceStartup;
            longestPollGap = Mathf.Max(longestPollGap, now - lastPoll);
            lastPoll = now;
            bool ready = IsGameplayRuntimeReady(requirePlayer, allowInitialHideout, out string reason);
            if (ready)
            {
                // Grant only once, never extend indefinitely when readiness flaps.
                if (!grantedReadyGrace)
                {
                    grantedReadyGrace = true;
                    stabilizationDeadline = Mathf.Max(stabilizationDeadline,
                        now + Mathf.Max(0.1f, _runtimeReadinessGraceSeconds));
                }
                stableFrames++;
            }
            else
            {
                if (stableFrames > 0) stableResets++;
                stableFrames = 0;
                lastNotReadyReason = reason;
            }

            if (stableFrames >= requiredFrames)
                break;

            string waitingReason = ready
                ? $"Runtime is ready; confirming consecutive frames {stableFrames}/{requiredFrames}."
                : reason;
            LogReadinessWait(context, "stabilization", now - started, waitingReason, ref nextProgressLog);
            if (now >= stabilizationDeadline)
            {
                ReportRuntimeReadinessTimeout(context, requirePlayer, allowInitialHideout,
                    "stabilization", now - started, stableFrames, requiredFrames,
                    lastNotReadyReason, longestPollGap, environmentElapsed, now - systemStarted, stableResets);
                completed?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        if (_logRuntimeReadinessDiagnostics)
            Debug.Log($"[SceneLoader] Runtime ready. context={context}, elapsed={Time.realtimeSinceStartup - started:0.000}s, " +
                $"environment={environmentElapsed:0.000}s, stabilization={Time.realtimeSinceStartup - systemStarted:0.000}s, " +
                $"stableFrames={stableFrames}/{requiredFrames}, stableResets={stableResets}, longestPollGap={longestPollGap:0.000}s", this);
        completed?.Invoke(true);
    }

    // Purpose: Waits for the player instance created by the scene-ready event before allowing camera setup.
    private IEnumerator WaitForPlayerReadiness(string context, Action<bool> completed)
    {
        float started = Time.realtimeSinceStartup;
        float deadline = started + Mathf.Max(1f, _runtimeReadinessTimeoutSeconds);
        float nextProgressLog = started + 5f;
        float lastPoll = started;
        float longestPollGap = 0f;

        while (!HasActivePlayer())
        {
            float now = Time.realtimeSinceStartup;
            longestPollGap = Mathf.Max(longestPollGap, now - lastPoll);
            lastPoll = now;
            const string reason = "PlayerCharacter has not been spawned or is inactive.";
            LogReadinessWait(context, "player spawn", now - started, reason, ref nextProgressLog);
            if (now >= deadline)
            {
                ReportRuntimeReadinessTimeout(context, true, true, "player spawn",
                    now - started, 0, 0, reason, longestPollGap);
                completed?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        completed?.Invoke(true);
    }

    // Home is accepted only after progression explicitly completed initial Home
    // entry. A loaded HomeScene alone must never hide a failed raid build/resume.
    private static bool HasRuntimeEnvironmentReady(
        SectorStateManager sectorStateManager,
        StageProgressionManager progression,
        bool allowInitialHideout,
        out bool usesHideout,
        out string diagnostic)
    {
        usesHideout = false;

        if (allowInitialHideout && progression != null)
        {
            if (!progression.isActiveAndEnabled || !progression.IsInitialRuntimePrepared)
            {
                diagnostic = string.IsNullOrEmpty(progression.InitialRuntimePreparationFailure)
                    ? "StageProgressionManager is still preparing initial Home/raid entry."
                    : progression.InitialRuntimePreparationFailure;
                return false;
            }

            usesHideout = !progression.InitialEntryRequiresGeneratedMap;
            if (usesHideout)
                return HasPreparedHideoutReady(sectorStateManager, progression.InitialHideoutSector, out diagnostic);
        }

        return HasGeneratedMapReady(sectorStateManager, out diagnostic);
    }

    private static bool HasPreparedHideoutReady(
        SectorStateManager sectorStateManager, SectorRuntime hideoutSector, out string diagnostic)
    {
        if (sectorStateManager == null)
        {
            diagnostic = "SectorStateManager is missing.";
            return false;
        }

        if (hideoutSector == null || sectorStateManager.CurrentSector != hideoutSector)
        {
            diagnostic = "Prepared Hideout has not been assigned as CurrentSector.";
            return false;
        }

        if (!hideoutSector.gameObject.scene.isLoaded || !hideoutSector.isActiveAndEnabled)
        {
            diagnostic = "Prepared Hideout is not active in a loaded scene.";
            return false;
        }

        HomeSector home = hideoutSector.GetComponent<HomeSector>();
        HideoutRuntime hideout = hideoutSector.GetComponent<HideoutRuntime>();
        if (home == null || !home.isActiveAndEnabled || home.SectorRuntime != hideoutSector ||
            hideout == null || !hideout.isActiveAndEnabled || hideout.Sector != hideoutSector ||
            !hideoutSector.IsOpened || !hideoutSector.IsCleared || hideoutSector.PlayerStartPoint == null)
        {
            diagnostic = "Authored HomeSector/Hideout runtime or spawn anchor is not prepared.";
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    // Purpose: Checks whether the generated stage layout has matching registered runtime sectors.
    private static bool HasGeneratedMapReady(SectorStateManager sectorStateManager, out string diagnostic)
    {
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

    // Purpose: Checks the chosen environment, runtime references, and optionally the player.
    private static bool IsGameplayRuntimeReady(bool requirePlayer, bool allowInitialHideout, out string diagnostic)
    {
        return IsRuntimeReady(
            FindAnyObjectByType<SectorStateManager>(),
            FindAnyObjectByType<StageProgressionManager>(FindObjectsInactive.Include),
            allowInitialHideout, requirePlayer, HasActivePlayer(), out diagnostic);
    }

    private static bool IsRuntimeReady(
        SectorStateManager sectorStateManager,
        StageProgressionManager progression,
        bool allowInitialHideout,
        bool requirePlayer,
        bool hasActivePlayer,
        out string diagnostic)
    {
        if (!HasRuntimeEnvironmentReady(sectorStateManager, progression, allowInitialHideout,
                out bool usesHideout, out diagnostic))
            return false;

        if (!usesHideout && sectorStateManager.StartSector == null)
        {
            diagnostic = "StartSector has not been registered.";
            return false;
        }

        if (sectorStateManager.CurrentSector == null)
        {
            diagnostic = "CurrentSector has not been assigned.";
            return false;
        }

        for (int i = 0; !usesHideout && i < sectorStateManager.Sectors.Count; i++)
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

        if (requirePlayer && !hasActivePlayer)
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
    private void LogReadinessWait(string context, string phase, float elapsed, string reason, ref float nextLog)
    {
        if (!_logRuntimeReadinessDiagnostics || Time.realtimeSinceStartup < nextLog) return;
        nextLog = Time.realtimeSinceStartup + 5f;
        Debug.LogWarning($"[SceneLoader] Still waiting. context={context}, phase={phase}, elapsed={elapsed:0.000}s, " +
            $"reason={reason}, timeScale={Time.timeScale:0.###}, editorFocused={Application.isFocused}", this);
    }

    private void ReportRuntimeReadinessTimeout(string context, bool requirePlayer, bool allowInitialHideout = false,
        string phase = "runtime", float elapsedSeconds = -1f, int stableFrames = 0, int requiredFrames = 0,
        string lastNotReadyReason = "Not recorded.", float longestPollGap = 0f,
        float environmentSeconds = -1f, float stabilizationSeconds = -1f, int stableResets = 0)
    {
        bool ready = IsGameplayRuntimeReady(requirePlayer, allowInitialHideout, out string diagnostic);
        string reason = ready
            ? $"Runtime is ready now, but the wait expired before stable confirmation completed ({stableFrames}/{requiredFrames})."
            : diagnostic;
        var scenes = new System.Text.StringBuilder();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (i > 0) scenes.Append(", ");
            scenes.Append(scene.name).Append(scene.isLoaded ? " (loaded)" : " (loading)");
        }
        var sectors = FindAnyObjectByType<SectorStateManager>();
        var progression = FindAnyObjectByType<StageProgressionManager>(FindObjectsInactive.Include);
        Debug.LogError(
            $"[SceneLoader] Runtime readiness timed out. context={context}, phase={phase}, " +
            $"requirePlayer={requirePlayer}, ready={ready}, reason={reason}\n" +
            $"elapsed={elapsedSeconds:0.000}s, timeout={_runtimeReadinessTimeoutSeconds:0.###}s, grace={_runtimeReadinessGraceSeconds:0.###}s, " +
            $"environment={environmentSeconds:0.000}s, stabilization={stabilizationSeconds:0.000}s, " +
            $"stableFrames={stableFrames}/{requiredFrames}, stableResets={stableResets}, longestPollGap={longestPollGap:0.000}s\n" +
            $"lastNotReadyReason={lastNotReadyReason}\n" +
            $"scenes=[{scenes}], sectorManagerPresent={sectors != null}, registeredSectors={(sectors != null ? sectors.Sectors.Count : 0)}, " +
            $"currentSector={(sectors != null && sectors.CurrentSector != null ? sectors.CurrentSector.name : "missing")}, " +
            $"startSector={(sectors != null && sectors.StartSector != null ? sectors.StartSector.name : "missing")}, " +
            $"initialPrepared={progression != null && progression.IsInitialRuntimePrepared}, " +
            $"preparationFailure={(progression != null ? progression.InitialRuntimePreparationFailure : "StageProgressionManager missing")}, " +
            $"playerActive={HasActivePlayer()}, timeScale={Time.timeScale:0.###}, focused={Application.isFocused}, frame={Time.frameCount}\n" +
            "longestPollGap includes editor pauses, frame stalls and synchronous work; it is not a profiler attribution.",
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
