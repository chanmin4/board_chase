using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("Game/UI/MultiInputButton")]
public class MultiInputButton : Button
{
    public bool IsSelected;

    private MenuSelectionHandler _menuSelectionHandler;

    protected override void Awake()
    {
        base.Awake();
        ResolveMenuSelectionHandler();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.HandleMouseEnter(gameObject);

        base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.HandleMouseExit(gameObject);

        base.OnPointerExit(eventData);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        IsSelected = true;

        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.UpdateSelection(gameObject);

        base.OnSelect(eventData);
    }

    public void UpdateSelected()
    {
        if (ResolveMenuSelectionHandler())
            _menuSelectionHandler.UpdateSelection(gameObject);
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        if (!ResolveMenuSelectionHandler() || _menuSelectionHandler.AllowsSubmit())
            base.OnSubmit(eventData);
    }

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