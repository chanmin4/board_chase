/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Mutarus
 * Primary responsibility: Implements the focused MutarusQTEStation responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public class MutarusQTEStation : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform _visualRoot;

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject _activeVisualPrefab;
    [SerializeField] private GameObject _completedVisualPrefab;
    [Header("Timing QTE Setting")]
    [SerializeField, Min(1)] private int _timingSuccessRequired = 3;
    [Header("Timing QTE Runtime")]
    [SerializeField, ReadOnly] private int _timingSuccessCount;
    [Header("Listening")]
    [SerializeField] private MutarusQTEPatternControllerEventChannelSO _controllerReadyChannel;

    private GameObject _activeVisualInstance;
    private GameObject _completedVisualInstance;
    private MutarusQTEPatternController _controller;

    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }
    public int TimingSuccessRequired => Mathf.Max(1, _timingSuccessRequired);
    public int TimingSuccessCount => _timingSuccessCount;
    public int TimingSuccessRemaining => Mathf.Max(0, TimingSuccessRequired - _timingSuccessCount);
    private Transform VisualRoot => _visualRoot != null ? _visualRoot : transform;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        EnsureVisuals();
        Clear();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_controllerReadyChannel != null)
        {
            _controllerReadyChannel.OnEventRaised += HandleControllerReady;
            HandleControllerReady(_controllerReadyChannel.Current);
        }

        EnsureVisuals();
        Clear();
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_controllerReadyChannel != null)
            _controllerReadyChannel.OnEventRaised -= HandleControllerReady;
    }

    public bool CanInteract => IsActive && !IsCompleted && _controller != null;

    // Purpose: Attempts to interact and reports whether the request succeeded.
    public bool TryInteract()
    {
        if (!CanInteract)
            return false;

        return _controller.TryStartQTE(this);
    }

    // Purpose: Updates the pattern active state.
    public void SetPatternActive(bool active)
    {
        EnsureVisuals();

        IsActive = active;
        IsCompleted = false;
        _timingSuccessCount = 0;

        SetVisualVisible(_activeVisualInstance, active);
        SetVisualVisible(_completedVisualInstance, false);
    }

    // Purpose: Performs the mark completed workflow for this component.
    public void MarkCompleted()
    {
        IsCompleted = true;
        _timingSuccessCount = TimingSuccessRequired;

        SetVisualVisible(_activeVisualInstance, false);
        SetVisualVisible(_completedVisualInstance, true);
    }

    // Purpose: Clears the current clear state.
    public void Clear()
    {
        IsActive = false;
        IsCompleted = false;
        _timingSuccessCount = 0;

        SetVisualVisible(_activeVisualInstance, false);
        SetVisualVisible(_completedVisualInstance, false);
    }

    // Purpose: Handles the incoming controller ready request or event.
    private void HandleControllerReady(MutarusQTEPatternController controller)
    {
        _controller = controller;
    }

    // Purpose: Ensures visuals is ready before the workflow continues.
    private void EnsureVisuals()
    {
        if (_activeVisualInstance == null && _activeVisualPrefab != null)
        {
            _activeVisualInstance = Instantiate(_activeVisualPrefab, VisualRoot);
            _activeVisualInstance.name = $"{_activeVisualPrefab.name}_Active_Instance";
            _activeVisualInstance.transform.localPosition = Vector3.zero;
            _activeVisualInstance.transform.localRotation = Quaternion.identity;
            _activeVisualInstance.transform.localScale = Vector3.one;
        }

        if (_completedVisualInstance == null && _completedVisualPrefab != null)
        {
            _completedVisualInstance = Instantiate(_completedVisualPrefab, VisualRoot);
            _completedVisualInstance.name = $"{_completedVisualPrefab.name}_Completed_Instance";
            _completedVisualInstance.transform.localPosition = Vector3.zero;
            _completedVisualInstance.transform.localRotation = Quaternion.identity;
            _completedVisualInstance.transform.localScale = Vector3.one;
        }
    }

    // Purpose: Updates the visual visible state.
    private void SetVisualVisible(GameObject visual, bool visible)
    {
        if (visual == null)
            return;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = visible;

        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = visible;

        CanvasGroup[] canvasGroups = visual.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            canvasGroups[i].alpha = visible ? 1f : 0f;
            canvasGroups[i].interactable = visible;
            canvasGroups[i].blocksRaycasts = visible;
        }

        ParticleSystem[] particles = visual.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (visible)
                particles[i].Play(true);
            else
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    // Purpose: Registers timing success with its owning runtime service.
    public bool RegisterTimingSuccess()
    {
        if (IsCompleted)
            return true;

        _timingSuccessCount = Mathf.Clamp(
            _timingSuccessCount + 1,
            0,
            TimingSuccessRequired);

        if (_timingSuccessCount >= TimingSuccessRequired)
        {
            MarkCompleted();
            return true;
        }

        return false;
    }

    // Purpose: Performs the reset timing progress workflow for this component.
    public void ResetTimingProgress()
    {
        _timingSuccessCount = 0;
    }
}
