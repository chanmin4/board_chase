using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// This class is responsible for starting the game by loading the persistent managers scene 
/// and raising the event to load the Main Menu
/// </summary>

public class InitializationLoader : MonoBehaviour
{
	[SerializeField] private GameSceneSO _managersScene = default;
	[SerializeField] private GameSceneSO _menuToLoad = default;

	[Header("Broadcasting on")]
	//AssetReference (Addressables 전용 “에셋 참조 타입”)
	//인스펙터에서 에셋을 끌어다 넣을 수 있는데,
	// 일반 참조처럼 바로 로드하지 않고 필요할 때 비동기로 로드할 수 있게 해주는 참조
	[SerializeField] private AssetReference _menuLoadChannel = default;

//persistent manager 로드 완료시 LoadEventChannelSO에셋 Addressables로 로드
//채널로드끝나면 메인씬 로드이벤트 실행 마지막 initialization 씬 언로드
	private void Start()
	{
		
		_managersScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true).Completed += LoadEventChannel;
	}

	private void LoadEventChannel(AsyncOperationHandle<SceneInstance> obj)
	{
		//LoadAssetAsync<T>() Addressables로 특정 에셋(여기선 LoadEventChannelSO)을 비동기로 로드하는 함수
		_menuLoadChannel.LoadAssetAsync<LoadEventChannelSO>().Completed += LoadMainMenu;
		
	}

	private void LoadMainMenu(AsyncOperationHandle<LoadEventChannelSO> obj)
	{
		obj.Result.RaiseEvent(_menuToLoad, true);

		SceneManager.UnloadSceneAsync(0); //Initialization is the only scene in BuildSettings, thus it has index 0
	}
}
