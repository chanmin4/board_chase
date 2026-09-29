/*
 * Role hierarchy: Gameplay
 * Primary responsibility: Defines GameState state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */


using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
	Gameplay, //regular state: player moves, attacks, can perform actions
	Pause, //pause menu is opened, the whole game world is frozen
	Inventory, //when inventory UI or cooking UI are open
	Dialogue,
	Cutscene,
	Combat
}

//[CreateAssetMenu(fileName = "GameState", menuName = "Gameplay/GameState", order = 51)]
public class GameStateSO : DescriptionBaseSO
{
	public GameState CurrentGameState => _currentGameState;
	
	[Header("Game states")]
	[SerializeField][ReadOnly] private GameState _currentGameState = default;
	[SerializeField][ReadOnly] private GameState _previousGameState = default;

	[Header("Broadcasting on")]

	
	private List<Transform> _alertEnemies;

	// Purpose: Completes initialization after scene dependencies are ready.
	private void Start()
	{
		_alertEnemies = new List<Transform>();
	}

// Purpose: Adds the requested alert enemy to this component.
public void AddAlertEnemy(Transform enemy)
	{
		if (!_alertEnemies.Contains(enemy))
		{
			_alertEnemies.Add(enemy);
		}

		UpdateGameState(GameState.Combat);
	}

	// Purpose: Removes the requested alert enemy from this component.
	public void RemoveAlertEnemy(Transform enemy)
	{
		if (_alertEnemies.Contains(enemy))
		{
			_alertEnemies.Remove(enemy);

			if (_alertEnemies.Count == 0)
			{
				UpdateGameState(GameState.Gameplay);
			}
		}
	}
	// Purpose: Updates the game state from the current runtime state.
	public void UpdateGameState(GameState newGameState)
	{
		if (newGameState == CurrentGameState)
			return;

		_previousGameState = _currentGameState;
		_currentGameState = newGameState;
	}

	// Purpose: Performs the reset to previous game state workflow for this component.
	public void ResetToPreviousGameState()
	{
		if (_previousGameState == _currentGameState)
			return;
		
		GameState stateToReturnTo = _previousGameState;
		_previousGameState = _currentGameState;
		_currentGameState = stateToReturnTo;
	}
}
