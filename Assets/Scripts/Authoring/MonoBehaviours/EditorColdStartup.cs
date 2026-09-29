/*
 * Role hierarchy: Authoring > MonoBehaviours
 * Primary responsibility: Implements the focused EditorColdStartup responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// Allows a "cold start" in the editor, when pressing Play and not passing from the Initialisation scene.
/// </summary> 
public class EditorColdStartup : MonoBehaviour
{
#if UNITY_EDITOR
	[SerializeField] private GameSceneSO _thisSceneSO = default;
	[SerializeField] private GameSceneSO _persistentManagersSO = default;
	[SerializeField] private AssetReference _notifyColdStartupChannel = default;
	[SerializeField] private VoidEventChannelSO _onSceneReadyChannel = default;
	[SerializeField] private SaveSystem _saveSystem = default;

	private bool _notificationRequested;
	// Purpose: Caches the component references required before runtime initialization.
	private void Awake()
	{
		CreateSaveFileIfNotPresent();
	}

	// Purpose: Completes initialization after scene dependencies are ready.
	private void Start()
	{
		bool persistentManagersLoaded =
			SceneManager.GetSceneByName(
				_persistentManagersSO.sceneReference.editorAsset.name).isLoaded;

		if (!persistentManagersLoaded)
		{
			_persistentManagersSO.sceneReference
				.LoadSceneAsync(LoadSceneMode.Additive, true)
				.Completed += LoadEventChannel;
		}
		else
		{
			RequestColdStartupNotification();
		}

		CreateSaveFileIfNotPresent();
	}
	// Purpose: Creates the save file if not present owned by this workflow.
	private void CreateSaveFileIfNotPresent()
	{
		if (_saveSystem != null && !_saveSystem.LoadSaveDataFromDisk())
		{
			_saveSystem.SetNewGameData();
		}
	}
	// Purpose: Loads event channel into the active runtime state.
	private void LoadEventChannel(AsyncOperationHandle<SceneInstance> obj)
	{
		RequestColdStartupNotification();
	}

	// Purpose: Requests cold startup notification from the owning service.
	private void RequestColdStartupNotification()
	{
		if (_notificationRequested)
			return;

		_notificationRequested = true;
		_notifyColdStartupChannel.LoadAssetAsync<LoadEventChannelSO>().Completed += OnNotifyChannelLoaded;
	}

	// Purpose: Responds to the notify channel loaded callback.
	private void OnNotifyChannelLoaded(AsyncOperationHandle<LoadEventChannelSO> obj)
	{
		if (obj.Status == AsyncOperationStatus.Succeeded &&
			obj.Result != null &&
			_thisSceneSO != null)
		{
			obj.Result.RaiseEvent(_thisSceneSO);
		}
		else
		{
			//Raise a fake scene ready event, so the player is spawned
			_onSceneReadyChannel?.RaiseEvent();
			//When this happens, the player won't be able to move between scenes because the SceneLoader has no conception of which scene we are in
		}
	}


#endif
}
