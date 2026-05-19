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

    [Header("Broadcasting on")]
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

        // MainMenu 같은 Menu 씬을 에디터에서 바로 Play 했을 때는
        // GamePlay 매니저 씬을 Additive 로드하지 않는다.
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
            return;
        }

        if (_showLoadingScreen && _toggleLoadingScreen != null)
            _toggleLoadingScreen.RaiseEvent(true);

        _loadingOperationHandle = _sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true, 0);
        _loadingOperationHandle.Completed += OnNewSceneLoaded;
    }

    private void OnNewSceneLoaded(AsyncOperationHandle<SceneInstance> obj)
    {
        _currentlyLoadedScene = _sceneToLoad;

        Scene scene = obj.Result.Scene;
        SceneManager.SetActiveScene(scene);
        LightProbes.TetrahedralizeAsync();

        _isLoading = false;

        if (_showLoadingScreen && _toggleLoadingScreen != null)
            _toggleLoadingScreen.RaiseEvent(false);

        if (_fadeRequestChannel != null)
            _fadeRequestChannel.FadeIn(_fadeDuration);

        if (_raiseSceneReadyAfterLoad)
            StartGameplay();
    }

    private void StartGameplay()
    {
        if (_onSceneReady != null)
            _onSceneReady.RaiseEvent();
    }

    private void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exit!");
    }
}
