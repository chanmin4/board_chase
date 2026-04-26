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
	[Header("Scene Refs")]
	[SerializeField] private Transform _projectilesRoot = default;
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
		Debug.Log($"player Spawn position: {spawnLocation.position}");

		VSplatter_Character playerInstance = Instantiate(_playerPrefab, spawnLocation.position, spawnLocation.rotation);

		VSplatterWeaponHolder weaponHolder = playerInstance.GetComponentInChildren<VSplatterWeaponHolder>();
		
		if (weaponHolder != null)
		{
			Debug.Log($"[SpawnSystem] scene projectilesRoot = {_projectilesRoot}");
			Debug.Log($"[SpawnSystem] weaponHolder = {weaponHolder}");
			weaponHolder.SetProjectilesRoot(_projectilesRoot);
			Debug.Log($"[SpawnSystem] holder.ProjectilesRoot after set = {weaponHolder.ProjectilesRoot}");
		}

		_playerInstantiatedChannel.RaiseEvent(playerInstance.transform);
		_playerTransformAnchor.Provide(playerInstance.transform);

		_inputReader.EnableGameplayInput();
	}
}
