/*
 * Role hierarchy: Presentation > UI > Menu
 * Primary responsibility: Implements the focused MenuSelectionHandler responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSelectionHandler : MonoBehaviour
{
	[SerializeField] private InputReader _inputReader;
	[HideInInspector][SerializeField][ReadOnly] private GameObject _defaultSelection;
	[HideInInspector][SerializeField][ReadOnly] private GameObject _currentSelection;
	[HideInInspector][SerializeField][ReadOnly] private GameObject _mouseSelection;

	// Purpose: Activates runtime subscriptions and refreshes the enabled state.
	private void OnEnable()
	{
		if (_inputReader != null)
		{
			_inputReader.MenuMouseMoveEvent += HandleMoveCursor;
			_inputReader.MoveSelectionEvent += HandleMoveSelection;
		}

		StartCoroutine(SelectDefault());
	}

	// Purpose: Releases active subscriptions and transient state while disabled.
	private void OnDisable()
	{
		if (_inputReader != null)
		{
			_inputReader.MenuMouseMoveEvent -= HandleMoveCursor;
			_inputReader.MoveSelectionEvent -= HandleMoveSelection;
		}
	}

	// Purpose: Updates the default from the current runtime state.
	public void UpdateDefault(GameObject newDefault)
	{
		_defaultSelection = newDefault;
	}

	/// <summary>
	/// Highlights the default element
	/// </summary>
	private IEnumerator SelectDefault()
	{
		yield return new WaitForSeconds(.03f); // Necessary wait otherwise the highlight won't show up

		if (_defaultSelection != null)
			UpdateSelection(_defaultSelection);
	}

	// Purpose: Performs the unselect workflow for this component.
	public void Unselect()
	{
		_currentSelection = null;
		if (EventSystem.current != null)
			EventSystem.current.SetSelectedGameObject(null);
	}

	/// <summary>
	/// Fired by keyboard and gamepad inputs. Current selected UI element will be the ui Element that was selected
	/// when the event was fired. The _currentSelection is updated later on, after the EventSystem moves to the
	/// desired UI element, the UI element will call into UpdateSelection()
	/// </summary>
	private void HandleMoveSelection()
	{
		Cursor.visible = false;

		// Handle case where no UI element is selected because mouse left selectable bounds
		if (EventSystem.current.currentSelectedGameObject == null)
			EventSystem.current.SetSelectedGameObject(_currentSelection);
	}

	// Purpose: Handles the incoming move cursor request or event.
	private void HandleMoveCursor()
	{
		if (_mouseSelection != null)
		{
			EventSystem.current.SetSelectedGameObject(_mouseSelection);
		}

		Cursor.visible = true;
	}

	// Purpose: Handles the incoming mouse enter request or event.
	public void HandleMouseEnter(GameObject UIElement)
	{
		_mouseSelection = UIElement;
		EventSystem.current.SetSelectedGameObject(UIElement);
	}

	// Purpose: Handles the incoming mouse exit request or event.
	public void HandleMouseExit(GameObject UIElement)
	{
		if (EventSystem.current == null)
			return;

		if (EventSystem.current.currentSelectedGameObject != UIElement)
			return;

		_mouseSelection = null;
		EventSystem.current.SetSelectedGameObject(_currentSelection);
	}

	/// <summary>
	/// Method interactable UI elements should call on Submit interaction to determine whether to continue or not.
	/// </summary>
	/// <returns></returns>
	public bool AllowsSubmit()
	{
		if (_inputReader == null)
			return true;

		return !_inputReader.LeftMouseDown()
			|| _mouseSelection != null && _mouseSelection == _currentSelection;
	}
	/// <summary>
	/// Fired by gamepad or keyboard navigation inputs
	/// </summary>
	/// <param name="UIElement"></param>
	public void UpdateSelection(GameObject UIElement)
	{
		if ((UIElement.GetComponent<MultiInputSelectableElement>() != null) || (UIElement.GetComponent<MultiInputButton>() != null))
		{
			_mouseSelection = UIElement;
			_currentSelection = UIElement;
		}
	}

	// Debug
	// private void OnGUI()
	// {
	//	 	GUILayout.Box($"_currentSelection: {(_currentSelection != null ? _currentSelection.name : "null")}");
	//	 	GUILayout.Box($"_mouseSelection: {(_mouseSelection != null ? _mouseSelection.name : "null")}");
	// }
	// Purpose: Processes this component's per-frame runtime work.
	private void Update()
	{
		if ((EventSystem.current != null) && (EventSystem.current.currentSelectedGameObject == null) && (_currentSelection != null))
		{

			EventSystem.current.SetSelectedGameObject(_currentSelection);
		}
	}
}
