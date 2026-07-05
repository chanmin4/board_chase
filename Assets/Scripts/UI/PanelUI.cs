using UnityEngine;

[DisallowMultipleComponent]
public abstract class PanelUI : MonoBehaviour, IUIOverlayLifecycle
{
    [Header("Panel")]
    [SerializeField] private CanvasGroup _panelGroup;
    [SerializeField] private bool _hideOnAwake = false;

    protected CanvasGroup PanelGroup => _panelGroup;

    protected virtual void Reset()
    {
        _panelGroup = GetComponent<CanvasGroup>();
    }

    protected virtual void Awake()
    {
        EnsurePanelGroup();

        if (_hideOnAwake)
            SetPanelVisible(false);
    }

    public virtual void OnOverlayShown()
    {
    }

    public virtual void OnOverlayHidden()
    {
        HidePanel();
    }

    public void ShowPanel()
    {
        SetPanelVisible(true);
        OnPanelShown();
    }

    public void HidePanel()
    {
        SetPanelVisible(false);
        OnPanelHidden();
    }

    protected virtual void OnPanelShown()
    {
    }

    protected virtual void OnPanelHidden()
    {
    }

    protected void SetPanelVisible(bool visible)
    {
        EnsurePanelGroup();

        if (_panelGroup == null)
            return;

        _panelGroup.alpha = visible ? 1f : 0f;
        _panelGroup.interactable = visible;
        _panelGroup.blocksRaycasts = visible;
    }

    protected void EnsurePanelGroup()
    {
        if (_panelGroup == null)
            _panelGroup = GetComponent<CanvasGroup>();

        if (_panelGroup == null)
            _panelGroup = gameObject.AddComponent<CanvasGroup>();
    }
}
