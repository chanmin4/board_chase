/*
 * Role hierarchy: Presentation > UI > Gameplay > Overlay
 * Primary responsibility: Presents UIOverlayPanel state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIOverlayPanel : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private UIOverlayId _id;

    [Header("Visibility")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private bool _hideOnAwake = true;

    [Header("Gameplay Input")]
    [Tooltip("If true, blocks combat input while this overlay is open: shoot and aim. Movement, dash, and interact stay enabled unless the input map is changed elsewhere.")]
    [FormerlySerializedAs("_blockAttackInputWhileOpen")]
    [SerializeField] private bool _blockAttackInputWhileOpen = false;

    [Header("Events")]
    [SerializeField] private UnityEvent _onShown;
    [SerializeField] private UnityEvent _onHidden;

    private readonly List<IUIOverlayLifecycle> _lifecycleTargets = new();
    private Canvas _sortingCanvas;
    private GraphicRaycaster _sortingRaycaster;

    public UIOverlayId Id => _id;
    public bool BlockCombatInputWhileOpen => _blockAttackInputWhileOpen;
    public bool BlockAttackInputWhileOpen => _blockAttackInputWhileOpen;

    // Purpose: Configures this component''s runtime.
    public void ConfigureRuntime(
        UIOverlayId id,
        CanvasGroup canvasGroup,
        bool blockCombatInputWhileOpen,
        bool hideImmediately)
    {
        _id = id;
        _canvasGroup = canvasGroup;
        _blockAttackInputWhileOpen = blockCombatInputWhileOpen;
        _hideOnAwake = hideImmediately;

        EnsureCanvasGroup();

        if (hideImmediately)
            SetVisible(false);
    }

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureCanvasGroup();
        CacheLifecycleTargets();

        if (_hideOnAwake)
            SetVisible(false);
    }

    // Purpose: Shows the show UI or visual state.
    public void Show()
    {
        EnsureCanvasGroup();
        CacheLifecycleTargets();
        SetVisible(true);
        NotifyShown();
        _onShown?.Invoke();
    }

    // Purpose: Hides the hide UI or visual state.
    public void Hide()
    {
        EnsureCanvasGroup();
        CacheLifecycleTargets();
        SetVisible(false);
        NotifyHidden();
        _onHidden?.Invoke();
    }

    // Purpose: Updates the sorting order state.
    public void SetSortingOrder(int sortingOrder)
    {
        EnsureSortingCanvas();
        _sortingCanvas.overrideSorting = true;
        _sortingCanvas.sortingOrder = sortingOrder;
    }

    // Purpose: Ensures canvas group is ready before the workflow continues.
    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // Purpose: Ensures sorting canvas is ready before the workflow continues.
    private void EnsureSortingCanvas()
    {
        if (_sortingCanvas == null)
            _sortingCanvas = GetComponent<Canvas>();

        if (_sortingCanvas == null)
            _sortingCanvas = gameObject.AddComponent<Canvas>();

        if (_sortingRaycaster == null)
            _sortingRaycaster = GetComponent<GraphicRaycaster>();

        if (_sortingRaycaster == null)
            _sortingRaycaster = gameObject.AddComponent<GraphicRaycaster>();
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    // Purpose: Caches lifecycle targets for later reuse.
    private void CacheLifecycleTargets()
    {
        _lifecycleTargets.Clear();

        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IUIOverlayLifecycle lifecycle)
                _lifecycleTargets.Add(lifecycle);
        }
    }

    // Purpose: Publishes the shown notification.
    private void NotifyShown()
    {
        for (int i = 0; i < _lifecycleTargets.Count; i++)
            _lifecycleTargets[i]?.OnOverlayShown();
    }

    // Purpose: Publishes the hidden notification.
    private void NotifyHidden()
    {
        for (int i = 0; i < _lifecycleTargets.Count; i++)
            _lifecycleTargets[i]?.OnOverlayHidden();
    }
}
