using UnityEngine;

/// <summary>Authored hatch presentation backed by the existing raid extraction interaction.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RaidStation))]
[AddComponentMenu("Board Chase/Sector/Sector Hatch")]
public sealed class SectorHatch : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Already-placed closed object. Only its active state is changed; no object is created.")]
    [SerializeField] private GameObject _closedVisual;
    [Tooltip("Already-placed opened object. Only its active state is changed; no object is created.")]
    [SerializeField] private GameObject _openedVisual;

    [Header("Interaction")]
    [Tooltip("Required pre-placed trigger on this root or an always-active child outside both visual roots. Edit its Collider to change the interaction area.")]
    [SerializeField] private Collider _interactionTrigger;

    [Header("Prompt")]
    [SerializeField] private string _powerRequiredPrompt = "전력이 필요합니다";
    [SerializeField] private string _extractPrompt = "탈출";

    private RaidStation _station;
    private RaidRuntime _runtime;
    private RaidRuntime _subscribedRuntime;
    private bool _hasPresentation, _isOpen;

    public bool IsOpen => _isOpen;
    public string PromptLabel => !_isOpen ? _powerRequiredPrompt :
        _runtime != null && _runtime.Definition != null && _runtime.Definition.RaidRuntime.hatchAdvancesStage
            ? "다음 스테이지로 이동" : _extractPrompt;
    public Collider InteractionTrigger => _interactionTrigger;
    public bool HasValidInteractionTrigger => ValidateInteractionTrigger(out _);

    private void Reset()
    {
        foreach (var collider in GetComponents<Collider>())
            if (collider.isTrigger) { _interactionTrigger = collider; break; }
    }

    private void Awake()
    {
        _station = GetComponent<RaidStation>();
        if (!ValidateVisuals(out string visualProblem))
            Debug.LogWarning("[SectorHatch] " + visualProblem, this);
        if (!ValidateInteractionTrigger(out string problem))
            Debug.LogWarning("[SectorHatch] " + problem + " No runtime trigger is created; assign a pre-placed Collider in the Inspector.", this);
    }

    private void OnEnable()
    {
        if (_station == null) _station = GetComponent<RaidStation>();
        Configure(_station != null && _station.Kind == RaidStationKind.Hatch ? _station.Runtime : null);
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_station != null) _station.NotifyPromptChanged();
    }

    // Called by RaidStationFactory through RaidStation.Configure, including restored raids.
    internal void Configure(RaidRuntime runtime)
    {
        Unsubscribe();
        _runtime = runtime;
        if (isActiveAndEnabled && _runtime != null)
        {
            _subscribedRuntime = _runtime;
            _subscribedRuntime.StateChanged += RefreshPresentation;
        }
        RefreshPresentation();
    }

    private void Unsubscribe()
    {
        if (!ReferenceEquals(_subscribedRuntime, null))
            _subscribedRuntime.StateChanged -= RefreshPresentation;
        _subscribedRuntime = null;
    }

    public void RefreshPresentation()
    {
        bool opened = _runtime != null && _runtime.State != null && _runtime.State.active && _runtime.State.generatorPowered;
        bool changed = !_hasPresentation || opened != _isOpen;
        _isOpen = opened; _hasPresentation = true;
        if (gameObject.scene.IsValid() && ValidateVisuals(out _))
        {
            SetVisible(_closedVisual, !opened);
            SetVisible(_openedVisual, opened);
        }
        if (changed)
        {
            if (_station == null) _station = GetComponent<RaidStation>();
            if (_station != null) _station.NotifyPromptChanged();
        }
    }

    public bool ValidateVisuals(out string problem)
    {
        problem = null;
        if (_closedVisual == null || _openedVisual == null)
        {
            problem = "Closed and Opened Visual must both be assigned.";
            return false;
        }
        if (_closedVisual != null && _openedVisual != null &&
            (_closedVisual == _openedVisual || _closedVisual.transform.IsChildOf(_openedVisual.transform) ||
            _openedVisual.transform.IsChildOf(_closedVisual.transform)))
        {
            problem = "Closed/Open visuals must be separate sibling roots, not the same or nested objects.";
            return false;
        }
        foreach (var source in new[] { _closedVisual, _openedVisual })
        {
            if (!source.scene.IsValid() && !source.transform.IsChildOf(transform))
            {
                problem = "Visual objects must be already placed in a Scene, not Project prefab assets.";
                return false;
            }
            if (transform.IsChildOf(source.transform))
            {
                problem = "Visual objects must not include this component's own root or its parent objects.";
                return false;
            }
            if (source.GetComponentInChildren<InteractionPromptAnchor>(true) != null)
            {
                problem = "Keep InteractionPromptAnchor outside the visual roots so both hatch states retain it.";
                return false;
            }
        }
        return true;
    }

    /// <summary>Checks authored wiring without creating objects or changing the Collider geometry.</summary>
    public bool ValidateInteractionTrigger(out string problem)
    {
        problem = null;
        if (_interactionTrigger == null) problem = "Interaction Trigger is not assigned.";
        else if (!_interactionTrigger.transform.IsChildOf(transform)) problem = "Interaction Trigger must belong to this HatchRoot.";
        else if (InsideVisual(_interactionTrigger.transform)) problem = "Interaction Trigger must be outside the Closed/Open visual roots.";
        else if (!_interactionTrigger.isTrigger) problem = "Interaction Trigger requires Is Trigger.";
        else if (!_interactionTrigger.enabled) problem = "Interaction Trigger Collider is disabled.";
        else
        {
            for (var current = _interactionTrigger.transform; current != transform; current = current.parent)
                if (!current.gameObject.activeSelf) { problem = "Interaction Trigger and its parent objects must remain active."; break; }
        }
        return problem == null;
    }

    private bool InsideVisual(Transform candidate) =>
        (_closedVisual != null && candidate.IsChildOf(_closedVisual.transform)) ||
        (_openedVisual != null && candidate.IsChildOf(_openedVisual.transform));

    private static void SetVisible(GameObject target, bool visible)
    {
        if (target != null && target.activeSelf != visible) target.SetActive(visible);
    }
}
