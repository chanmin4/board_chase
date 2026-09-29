/*
 * Role hierarchy: Presentation > UI > Common
 * Primary responsibility: Presents PanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[DisallowMultipleComponent]
public abstract class PanelUI : MonoBehaviour, IUIOverlayLifecycle
{
    [Header("Panel")]
    [SerializeField] private CanvasGroup _panelGroup;
    [SerializeField] private bool _hideOnAwake = false;

    protected CanvasGroup PanelGroup => _panelGroup;

    // Purpose: Initializes Inspector defaults for this component.
    protected virtual void Reset()
    {
        _panelGroup = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    protected virtual void Awake()
    {
        EnsurePanelGroup();

        if (_hideOnAwake)
            SetPanelVisible(false);
    }

    // Purpose: Responds to the overlay shown callback.
    public virtual void OnOverlayShown()
    {
    }

    // Purpose: Responds to the overlay hidden callback.
    public virtual void OnOverlayHidden()
    {
        HidePanel();
    }

    // Purpose: Shows the panel UI or visual state.
    public void ShowPanel()
    {
        SetPanelVisible(true);
        OnPanelShown();
    }

    // Purpose: Hides the panel UI or visual state.
    public void HidePanel()
    {
        SetPanelVisible(false);
        OnPanelHidden();
    }

    // Purpose: Responds to the panel shown callback.
    protected virtual void OnPanelShown()
    {
    }

    // Purpose: Responds to the panel hidden callback.
    protected virtual void OnPanelHidden()
    {
    }

    // Purpose: Updates the panel visible state.
    protected void SetPanelVisible(bool visible)
    {
        EnsurePanelGroup();

        if (_panelGroup == null)
            return;

        _panelGroup.alpha = visible ? 1f : 0f;
        _panelGroup.interactable = visible;
        _panelGroup.blocksRaycasts = visible;
    }

    // Purpose: Ensures panel group is ready before the workflow continues.
    protected void EnsurePanelGroup()
    {
        if (_panelGroup == null)
            _panelGroup = GetComponent<CanvasGroup>();

        if (_panelGroup == null)
            _panelGroup = gameObject.AddComponent<CanvasGroup>();
    }
}
