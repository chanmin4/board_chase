/*
 * Role hierarchy: Presentation > UI > Menu
 * Primary responsibility: Collects MultiInputSelectableElement input and forwards it as gameplay action requests.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("UOP1/UI/MultiInputSelectableElement")]
public class MultiInputSelectableElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
	private MenuSelectionHandler _menuSelectionHandler;

	// Purpose: Caches the component references required before runtime initialization.
	private void Awake()
	{
		_menuSelectionHandler = transform.root.gameObject.GetComponentInChildren<MenuSelectionHandler>();
	}

	// Purpose: Responds to the pointer enter callback.
	public void OnPointerEnter(PointerEventData eventData)
	{
		_menuSelectionHandler.HandleMouseEnter(gameObject);
	}

	// Purpose: Responds to the pointer exit callback.
	public void OnPointerExit(PointerEventData eventData)
	{
		_menuSelectionHandler.HandleMouseExit(gameObject);
	}

	// Purpose: Responds to the select callback.
	public void OnSelect(BaseEventData eventData)
	{
		_menuSelectionHandler.UpdateSelection(gameObject);
	}
	
}
