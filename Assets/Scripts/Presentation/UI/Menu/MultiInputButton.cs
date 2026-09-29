/*
 * Role hierarchy: Presentation > UI > Menu
 * Primary responsibility: Collects MultiInputButton input and forwards it as gameplay action requests.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("Game/UI/MultiInputButton")]
public class MultiInputButton : Button
{
    public bool IsSelected;

    private MenuSelectionHandler _menuSelectionHandler;

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();
        ResolveMenuSelectionHandler();
    }

    // Purpose: Responds to the pointer enter callback.
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.HandleMouseEnter(gameObject);

        base.OnPointerEnter(eventData);
    }

    // Purpose: Responds to the pointer exit callback.
    public override void OnPointerExit(PointerEventData eventData)
    {
        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.HandleMouseExit(gameObject);

        base.OnPointerExit(eventData);
    }

    // Purpose: Responds to the select callback.
    public override void OnSelect(BaseEventData eventData)
    {
        IsSelected = true;

        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.UpdateSelection(gameObject);

        base.OnSelect(eventData);
    }

    // Purpose: Updates the selected from the current runtime state.
    public void UpdateSelected()
    {
        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.UpdateSelection(gameObject);
    }

    // Purpose: Responds to the submit callback.
    public override void OnSubmit(BaseEventData eventData)
    {
        if (!ResolveMenuSelectionHandler() || _menuSelectionHandler.AllowsSubmit())
            base.OnSubmit(eventData);
    }

    // Purpose: Resolves the menu selection handler needed by this component.
    private bool ResolveMenuSelectionHandler()
    {
        if (_menuSelectionHandler != null)
            return true;

        Transform root = transform.root;
        if (root == null)
            return false;

        _menuSelectionHandler = root.GetComponentInChildren<MenuSelectionHandler>(true);
        return _menuSelectionHandler != null;
    }
}
