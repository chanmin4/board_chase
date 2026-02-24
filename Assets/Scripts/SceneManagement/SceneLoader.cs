using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private LoadEventChannelSO _loadChannel; // TODO: LoadMenu_Channel 같은 걸 직접 참조

    private AsyncOperationHandle<SceneInstance> _currentSceneHandle;
    private bool _hasCurrent;

    private void OnEnable()
    {
        _loadChannel.OnLoadingRequested += OnLoadRequested;
    }

    private void OnDisable()
    {
        _loadChannel.OnLoadingRequested -= OnLoadRequested;
    }

    private void OnLoadRequested(GameSceneSO sceneToLoad, bool showLoadingScreen, bool fadeScreen)
    {
        // TODO: showLoadingScreen/fadeScreen 처리

        // 이전 씬 언로드 후 새 씬 로드
        if (_hasCurrent)
        {
            Addressables.UnloadSceneAsync(_currentSceneHandle, true).Completed += _ =>
            {
                LoadNew(sceneToLoad);
            };
        }
        else
        {
            LoadNew(sceneToLoad);
        }
    }

    private void LoadNew(GameSceneSO sceneToLoad)
    {
        _currentSceneHandle = sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Single, true);
        _hasCurrent = true;
    }
}