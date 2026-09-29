/*
 * Role hierarchy: Presentation > UI > Menu
 * Primary responsibility: Coordinates the MainMenuController subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class MainMenuController : MonoBehaviour
{
	[SerializeField] private InputReader _inputReader;
	[SerializeField] private GameObject _menuPrefab;
	private GameObject _menuInstance;

	// Purpose: Opens the menu UI or workflow.
	private void OpenMenu()
	{
		if (_menuInstance == null)
			_menuInstance = Instantiate(_menuPrefab);

		_menuInstance.SetActive(true);
		_inputReader.EnableMenuInput();
	}

	// Purpose: Performs the unpause menu workflow for this component.
	private void UnpauseMenu()
	{
		_menuInstance.SetActive(false);
		_inputReader.EnableGameplayInput();
	}
}
