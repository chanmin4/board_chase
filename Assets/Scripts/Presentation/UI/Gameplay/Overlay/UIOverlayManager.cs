/*
 * Role hierarchy: Presentation > UI > Gameplay > Overlay
 * Primary responsibility: Coordinates the UIOverlayManager subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UIOverlayManager : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private UIOverlayRequestEventChannelSO _requestChannel;

    [Header("Input")]
    [SerializeField] private InputReader _inputReader;

    [Header("Panels")]
    [SerializeField] private UIOverlayPanel[] _panels;
    [SerializeField] private bool _autoFindPanelsInChildren = true;

    [Header("Ordering")]
    [Tooltip("Sorting order assigned to the oldest open overlay. Newly selected overlays receive a higher order. Keep this below the ESC menu order.")]
    [SerializeField] private int _baseSortingOrder = 100;

    [Header("Audio")]
    [SerializeField] private UIAudioEventChannelSO _uiAudioEventChannel;
    [SerializeField] private bool _playOverlayAudio = true;

    [Header("Debug")]
    [SerializeField] private bool _debugLogs = true;

    private readonly Dictionary<UIOverlayId, UIOverlayPanel> _panelById = new();
    private readonly List<UIOverlayId> _openOverlayStack = new();
    private bool _gameplayInputDisabledByOverlay;

    public UIOverlayRequestEventChannelSO RequestChannel => _requestChannel;
    public InputReader InputReader => _inputReader;
    public bool HasOpenOverlays => _openOverlayStack.Count > 0;
    public UIOverlayId TopOverlayId =>
        _openOverlayStack.Count > 0
            ? _openOverlayStack[_openOverlayStack.Count - 1]
            : UIOverlayId.None;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        CachePanels();
        CloseAllInternal(false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_requestChannel != null)
            _requestChannel.OnEventRaised += HandleOverlayRequest;
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_requestChannel != null)
            _requestChannel.OnEventRaised -= HandleOverlayRequest;

        GameplayActionInputBlocker.Clear(this);
        RestoreGameplayInputIfNeeded();
    }

    // Purpose: Opens the open UI or workflow.
    public void Open(UIOverlayId id)
    {
        if (id == UIOverlayId.None)
            return;

        if (!_panelById.TryGetValue(id, out UIOverlayPanel nextPanel) || nextPanel == null)
            CachePanels();

        if (!_panelById.TryGetValue(id, out nextPanel) || nextPanel == null)
        {
            if (_debugLogs)
                Debug.LogWarning($"[UIOverlayManager] Open failed. Panel not found. id={id}", this);

            return;
        }

        int existingIndex = _openOverlayStack.IndexOf(id);
        bool wasAlreadyOpen = existingIndex >= 0;

        if (wasAlreadyOpen)
            _openOverlayStack.RemoveAt(existingIndex);

        _openOverlayStack.Add(id);

        if (!wasAlreadyOpen)
        {
            nextPanel.Show();
            PlayOverlayAudio(UIAudioCueType.PanelOpen);
        }

        RefreshOpenOverlayState();

        if (_debugLogs)
        {
            Debug.Log(
                $"[UIOverlayManager] {(wasAlreadyOpen ? "Brought overlay to front" : "Opened overlay")}. " +
                $"id={id}, openCount={_openOverlayStack.Count}, blockCombat={GameplayActionInputBlocker.IsCombatInputBlocked}",
                this);
        }
    }

    // Purpose: Closes the active close UI or workflow.
    public void Close(UIOverlayId id)
    {
        int openIndex = _openOverlayStack.IndexOf(id);
        if (openIndex < 0)
            return;

        _openOverlayStack.RemoveAt(openIndex);

        if (_panelById.TryGetValue(id, out UIOverlayPanel panel) && panel != null)
            panel.Hide();

        PlayOverlayAudio(UIAudioCueType.PanelClose);
        RefreshOpenOverlayState();

        if (_debugLogs)
            Debug.Log($"[UIOverlayManager] Closed overlay. id={id}, openCount={_openOverlayStack.Count}", this);
    }

    // Purpose: Toggles the toggle state.
    public void Toggle(UIOverlayId id)
    {
        if (TopOverlayId == id)
        {
            if (_debugLogs)
                Debug.Log($"[UIOverlayManager] Toggle close. id={id}", this);

            Close(id);
            return;
        }

        if (_debugLogs)
            Debug.Log($"[UIOverlayManager] Toggle open. id={id}", this);

        Open(id);
    }

    // Purpose: Closes the active all UI or workflow.
    public void CloseAll()
    {
        CloseAllInternal();
    }

    // Purpose: Restores the saved input for open overlays state.
    public void RestoreInputForOpenOverlays()
    {
        ApplyGameplayInputMode(true);
    }

    // Purpose: Registers panel with its owning runtime service.
    public void RegisterPanel(UIOverlayPanel panel)
    {
        if (panel == null || panel.Id == UIOverlayId.None)
            return;

        _panelById[panel.Id] = panel;
    }

    // Purpose: Caches panels for later reuse.
    private void CachePanels()
    {
        _panelById.Clear();

        // Inspector references are the authoritative base list. Runtime hubs such as
        // ShopHub and LootHub are siblings of this manager, so child-only discovery
        // must never replace the serialized references.
        RegisterPanels(_panels, true);

        if (_autoFindPanelsInChildren)
            RegisterPanels(GetComponentsInChildren<UIOverlayPanel>(true), false);

        // Context hubs live under the gameplay Canvas instead of below the manager.
        // Include inactive scene panels because every overlay starts hidden.
        RegisterPanels(
            FindObjectsByType<UIOverlayPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            false);
    }

    // Purpose: Registers panels with its owning runtime service.
    private void RegisterPanels(UIOverlayPanel[] panels, bool overwriteExisting)
    {
        if (panels == null)
            return;

        for (int i = 0; i < panels.Length; i++)
        {
            UIOverlayPanel panel = panels[i];

            if (panel == null || panel.Id == UIOverlayId.None)
                continue;

            if (overwriteExisting || !_panelById.ContainsKey(panel.Id))
                _panelById[panel.Id] = panel;
        }
    }

    // Purpose: Handles the incoming overlay request request or event.
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

    // Purpose: Closes the active all internal UI or workflow.
    private void CloseAllInternal(bool playAudio = true)
    {
        bool hadOpenOverlay = _openOverlayStack.Count > 0;

        foreach (var pair in _panelById)
        {
            if (pair.Value != null)
                pair.Value.Hide();
        }

        _openOverlayStack.Clear();
        GameplayActionInputBlocker.Clear(this);
        RestoreGameplayInputIfNeeded();

        if (playAudio && hadOpenOverlay)
            PlayOverlayAudio(UIAudioCueType.PanelClose);
    }

    // Purpose: Plays the requested overlay audio action or effect.
    private void PlayOverlayAudio(UIAudioCueType cueType)
    {
        if (!_playOverlayAudio || _uiAudioEventChannel == null)
            return;

        _uiAudioEventChannel.RaiseEvent(cueType);
    }

    // Purpose: Performs the should block combat input workflow for this component.
    private static bool ShouldBlockCombatInput(UIOverlayPanel panel)
    {
        return panel != null &&
               (panel.BlockCombatInputWhileOpen ||
                panel.Id == UIOverlayId.PlayerPanelHub ||
                panel.Id == UIOverlayId.ShopHub ||
                panel.Id == UIOverlayId.LootHub);
    }

    // Purpose: Refreshes the displayed or cached open overlay state.
    private void RefreshOpenOverlayState()
    {
        RefreshPanelSorting();

        bool shouldBlockCombat = false;
        for (int i = 0; i < _openOverlayStack.Count; i++)
        {
            if (_panelById.TryGetValue(_openOverlayStack[i], out UIOverlayPanel panel) &&
                ShouldBlockCombatInput(panel))
            {
                shouldBlockCombat = true;
                break;
            }
        }

        if (shouldBlockCombat)
            GameplayActionInputBlocker.SetBlocked(true, this);
        else
            GameplayActionInputBlocker.Clear(this);

        ApplyGameplayInputMode(false);
    }

    // Purpose: Refreshes the displayed or cached panel sorting.
    private void RefreshPanelSorting()
    {
        for (int i = 0; i < _openOverlayStack.Count; i++)
        {
            if (_panelById.TryGetValue(_openOverlayStack[i], out UIOverlayPanel panel) && panel != null)
                panel.SetSortingOrder(_baseSortingOrder + i);
        }
    }

    // Purpose: Applies the requested gameplay input mode change.
    private void ApplyGameplayInputMode(bool force)
    {
        if (_inputReader == null)
            return;

        bool needsPlayerMenuInput =
            _openOverlayStack.Contains(UIOverlayId.PlayerPanelHub) ||
            _openOverlayStack.Contains(UIOverlayId.ShopHub) ||
            _openOverlayStack.Contains(UIOverlayId.LootHub) ||
            _openOverlayStack.Contains(UIOverlayId.LockedLootKey);

        if (needsPlayerMenuInput)
        {
            if (force || !_gameplayInputDisabledByOverlay)
                _inputReader.EnablePlayerMenuInput();

            _gameplayInputDisabledByOverlay = true;
            return;
        }

        if (force)
        {
            _gameplayInputDisabledByOverlay = false;
            _inputReader.EnableGameplayInput();
            return;
        }

        RestoreGameplayInputIfNeeded();
    }

    // Purpose: Restores the saved gameplay input if needed state.
    private void RestoreGameplayInputIfNeeded()
    {
        if (!_gameplayInputDisabledByOverlay)
            return;

        _gameplayInputDisabledByOverlay = false;

        if (_inputReader != null)
            _inputReader.EnableGameplayInput();
    }
}
