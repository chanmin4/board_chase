using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{ 
	[Header("Asset References")]
	[SerializeField] private InputReader _inputReader = default;
	[SerializeField] private VSplatter_Character _playerPrefab = default;
	[SerializeField] private TransformAnchor _playerTransformAnchor = default;
	[SerializeField] private TransformEventChannelSO _playerInstantiatedChannel = default;

	[Header("Scene Ready Event")]
	[SerializeField] private VoidEventChannelSO _onSceneReady = default; //Raised by SceneLoader when the scene is set to active

	private Transform _defaultSpawnPoint;

	private void Awake()
	{
		_defaultSpawnPoint = transform.GetChild(0);
	}

	private void OnEnable()
	{
		Debug.Log("onsceneready event raised");
		_onSceneReady.OnEventRaised += SpawnPlayer;
	}

	private void OnDisable()
	{
		_onSceneReady.OnEventRaised -= SpawnPlayer;

		_playerTransformAnchor.Unset();
	}


	private void SpawnPlayer()
	{
		Transform spawnLocation = _defaultSpawnPoint;
		VSplatter_Character playerInstance = Instantiate(_playerPrefab, spawnLocation.position, spawnLocation.rotation);
		Debug.Log("vplatter spawn ");
		_playerInstantiatedChannel.RaiseEvent(playerInstance.transform);
		_playerTransformAnchor.Provide(playerInstance.transform); //the CameraSystem will pick this up to frame the player

		//TODO: Probably move this to the GameManager once it's up and running
		_inputReader.EnableGameplayInput();
	}
}
