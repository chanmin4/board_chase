using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UIOverlayManager : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private UIOverlayRequestEventChannelSO _requestChannel;

    [Header("Panels")]
    [SerializeField] private UIOverlayPanel[] _panels;
    [SerializeField] private bool _autoFindPanelsInChildren = true;

    private readonly Dictionary<UIOverlayId, UIOverlayPanel> _panelById = new();
    private UIOverlayId _currentOverlayId = UIOverlayId.None;

    private void Awake()
    {
        CachePanels();
        CloseAllInternal();
    }

    private void OnEnable()
    {
        if (_requestChannel != null)
            _requestChannel.OnEventRaised += HandleOverlayRequest;
    }

    private void OnDisable()
    {
        if (_requestChannel != null)
            _requestChannel.OnEventRaised -= HandleOverlayRequest;

        GameplayAttackInputBlocker.Clear(this);
    }

    public void Open(UIOverlayId id)
    {
        if (id == UIOverlayId.None)
            return;

        if (!_panelById.TryGetValue(id, out UIOverlayPanel nextPanel) || nextPanel == null)
            return;

        if (_currentOverlayId == id)
            return;

        CloseCurrentInternal();

        _currentOverlayId = id;
        nextPanel.Show();
        GameplayAttackInputBlocker.SetBlocked(ShouldBlockAttackInput(nextPanel), this);
    }

    public void Close(UIOverlayId id)
    {
        if (_currentOverlayId != id)
            return;

        CloseCurrentInternal();
    }

    public void Toggle(UIOverlayId id)
    {
        if (_currentOverlayId == id)
        {
            Close(id);
            return;
        }

        Open(id);
    }

    public void CloseAll()
    {
        CloseAllInternal();
    }

    private void CachePanels()
    {
        _panelById.Clear();

        if ((_panels == null || _panels.Length == 0) && _autoFindPanelsInChildren)
            _panels = GetComponentsInChildren<UIOverlayPanel>(true);

        if (_panels == null)
            return;

        for (int i = 0; i < _panels.Length; i++)
        {
            UIOverlayPanel panel = _panels[i];

            if (panel == null || panel.Id == UIOverlayId.None)
                continue;

            _panelById[panel.Id] = panel;
        }
    }

    private void HandleOverlayRequest(UIOverlayRequest request)
    {
        switch (request.requestType)
        {
            case UIOverlayRequestType.Open:
                Open(request.overlayId);
                break;

            case UIOverlayRequestType.Close:
                Close(request.overlayId);
                break;

            case UIOverlayRequestType.Toggle:
                Toggle(request.overlayId);
                break;

            case UIOverlayRequestType.CloseAll:
                CloseAllInternal();
                break;
        }
    }

    private void CloseCurrentInternal()
    {
        if (_currentOverlayId == UIOverlayId.None)
            return;

        if (_panelById.TryGetValue(_currentOverlayId, out UIOverlayPanel currentPanel) &&
            currentPanel != null)
        {
            currentPanel.Hide();
        }

        _currentOverlayId = UIOverlayId.None;
        GameplayAttackInputBlocker.Clear(this);
    }

    private void CloseAllInternal()
    {
        foreach (var pair in _panelById)
        {
            if (pair.Value != null)
                pair.Value.Hide();
        }

        _currentOverlayId = UIOverlayId.None;
        GameplayAttackInputBlocker.Clear(this);
    }

    private static bool ShouldBlockAttackInput(UIOverlayPanel panel)
    {
        return panel != null &&
               (panel.BlockAttackInputWhileOpen || panel.Id == UIOverlayId.FullMap);
    }
}
