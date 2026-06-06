using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private GameSceneSO _gameplayScene = default;
    [SerializeField] private InputReader _inputReader = default;

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

    [Tooltip("다른 UI보다 위에 오도록 충분히 큰 sorting order를 사용합니다.")]
    [SerializeField] private int _loadingSortingOrder = 30000;

    [Tooltip("로딩 화면이 너무 번쩍하고 사라지는 것을 막기 위한 최소 표시 시간입니다.")]
    [SerializeField, Min(0f)] private float _minimumLoadingScreenSeconds = 0.35f;

    [Tooltip("새 씬 로드 후 onSceneReady를 보낸 다음 몇 프레임 더 덮고 있을지 정합니다.")]
    [SerializeField, Min(0)] private int _waitFramesAfterSceneReady = 2;

    [Header("Broadcasting on")]
    [Tooltip("기존 오픈프로젝트 방식 호환용입니다. SceneLoader가 직접 LoadingGroup을 제어한다면 비워도 됩니다.")]
    [SerializeField] private BoolEventChannelSO _toggleLoadingScreen = default;

    [SerializeField] private VoidEventChannelSO _onSceneReady = default;
    [SerializeField] private FadeChannelSO _fadeRequestChannel = default;

    private AsyncOperationHandle<SceneInstance> _loadingOperationHandle;
    private AsyncOperationHandle<SceneInstance> _gameplayManagerLoadingOpHandle;

    private GameSceneSO _sceneToLoad;
    private GameSceneSO _currentlyLoadedScene;
    private bool _showLoadingScreen;

    private SceneInstance _gameplayManagerSceneInstance = new SceneInstance();
    private float _fadeDuration = .5f;
    private bool _isLoading;
    private bool _raiseSceneReadyAfterLoad = true;
    private Coroutine _gameOverRoutine;
    private Coroutine _finishLoadingRoutine;
    private float _loadingScreenShownAt;

    private void Awake()
    {
        InitializeLoadingScreen();
        SetLoadingScreenVisible(false);
    }

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
    private void CurrentSceneColdStartup(GameSceneSO currentlyOpenedLocation, bool showLoadingScreen, bool fadeScreen)
    {
        _currentlyLoadedScene = currentlyOpenedLocation;

        bool shouldLoadGameplayManager =
            currentlyOpenedLocation != null &&
            currentlyOpenedLocation.sceneType == GameSceneSO.GameSceneType.Location;

        _raiseSceneReadyAfterLoad = shouldLoadGameplayManager;

        if (!shouldLoadGameplayManager)
            return;

        if (_gameplayManagerSceneInstance.Scene == null || !_gameplayManagerSceneInstance.Scene.isLoaded)
        {
            _gameplayManagerLoadingOpHandle =
                _gameplayScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);

            _gameplayManagerLoadingOpHandle.Completed += OnColdStartupGameplayManagersLoaded;
        }
        else
        {
            StartGameplay();
        }
    }

    private void OnColdStartupGameplayManagersLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _gameplayManagerSceneInstance = obj.Result;
        StartGameplay();
    }
#endif

    private void LoadGameScene(GameSceneSO gameToLoad, bool showLoadingScreen, bool fadeScreen)
    {
        if (_isLoading)
            return;

        RunResult.Clear();

        _sceneToLoad = gameToLoad;
        _showLoadingScreen = showLoadingScreen;
        _raiseSceneReadyAfterLoad = true;
        _isLoading = true;

        if (_showLoadingScreen)
            SetLoadingScreenVisible(true);

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

    private bool ShouldLoadGameplayManager(GameSceneSO scene)
    {
        return scene != null &&
            scene.sceneType == GameSceneSO.GameSceneType.Location;
    }

    private void OnGameplayManagersLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _gameplayManagerSceneInstance = _gameplayManagerLoadingOpHandle.Result;
        StartCoroutine(UnloadPreviousScene());
    }

    private void LoadMenu(GameSceneSO menuToLoad, bool showLoadingScreen, bool fadeScreen)
    {
        if (_isLoading)
            return;

        _sceneToLoad = menuToLoad;
        _showLoadingScreen = showLoadingScreen;
        _raiseSceneReadyAfterLoad = false;
        _isLoading = true;

        if (_showLoadingScreen)
            SetLoadingScreenVisible(true);

        if (_gameplayManagerSceneInstance.Scene != null &&
            _gameplayManagerSceneInstance.Scene.isLoaded &&
            _gameplayManagerLoadingOpHandle.IsValid())
        {
            Addressables.UnloadSceneAsync(_gameplayManagerLoadingOpHandle, true);
            _gameplayManagerSceneInstance = new SceneInstance();
        }

        StartCoroutine(UnloadPreviousScene());
    }

    private void HandleGameOverRequested()
    {
        if (_isLoading || _gameOverRoutine != null)
            return;

        _gameOverRoutine = StartCoroutine(LoadGameOverSceneRoutine());
    }

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
            SetLoadingScreenVisible(true);

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

        LoadNewScene();
        _gameOverRoutine = null;
    }

    private IEnumerator UnloadPreviousScene()
    {
        if (_inputReader != null)
            _inputReader.DisableAllInput();

        if (_fadeRequestChannel != null)
            _fadeRequestChannel.FadeOut(_fadeDuration);

        yield return new WaitForSeconds(_fadeDuration);

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
            SetLoadingScreenVisible(true);

        _loadingOperationHandle = _sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true, 0);
        _loadingOperationHandle.Completed += OnNewSceneLoaded;
    }

    private void OnNewSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _currentlyLoadedScene = _sceneToLoad;

        Scene scene = obj.Result.Scene;
        SceneManager.SetActiveScene(scene);
        LightProbes.TetrahedralizeAsync();

        if (_finishLoadingRoutine != null)
            StopCoroutine(_finishLoadingRoutine);

        _finishLoadingRoutine = StartCoroutine(FinishSceneLoadRoutine());
    }

    private IEnumerator FinishSceneLoadRoutine()
    {
        if (_raiseSceneReadyAfterLoad)
            StartGameplay();

        for (int i = 0; i < _waitFramesAfterSceneReady; i++)
            yield return null;

        if (_showLoadingScreen)
        {
            float elapsed = Time.unscaledTime - _loadingScreenShownAt;
            float remaining = _minimumLoadingScreenSeconds - elapsed;

            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);
        }

        if (_fadeRequestChannel != null)
            _fadeRequestChannel.FadeIn(_fadeDuration);

        if (_showLoadingScreen)
            SetLoadingScreenVisible(false);

        _isLoading = false;
        _finishLoadingRoutine = null;
    }

    private void StartGameplay()
    {
        if (_onSceneReady != null)
            _onSceneReady.RaiseEvent();
    }

    private void InitializeLoadingScreen()
    {
        if (_loadingCanvas != null)
        {
            _loadingCanvas.overrideSorting = true;
            _loadingCanvas.sortingOrder = _loadingSortingOrder;
        }
    }

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

    private void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exit!");
    }
}