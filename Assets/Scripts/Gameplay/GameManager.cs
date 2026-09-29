/*
 * Role hierarchy: Gameplay
 * Primary responsibility: Coordinates the GameManager subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[SerializeField] private GameStateSO _gameState = default;
	//[Header("Broadcasting on")]

	// Purpose: Completes initialization after scene dependencies are ready.
	private void Start()
	{
		StartGame();
	}

	// Purpose: Activates runtime subscriptions and refreshes the enabled state.
	private void OnEnable()
	{
	}

	// Purpose: Releases active subscriptions and transient state while disabled.
	private void OnDisable()
	{
	}

	void StartGame()
	{
		_gameState.UpdateGameState(GameState.Gameplay);
	}
}
