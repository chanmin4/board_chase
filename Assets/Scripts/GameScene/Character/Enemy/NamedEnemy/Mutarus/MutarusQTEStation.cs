using UnityEngine;

public class MutarusQTEStation : MonoBehaviour
{
    [Header("Spawn Point")]
    [SerializeField] private Transform _visualRoot;

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject _activeVisualPrefab;
    [SerializeField] private GameObject _completedVisualPrefab;

    [Header("Listening")]
    [SerializeField] private MutarusQTEPatternControllerEventChannelSO _controllerReadyChannel;

    private GameObject _activeVisualInstance;
    private GameObject _completedVisualInstance;
    private MutarusQTEPatternController _controller;

    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }

    private Transform VisualRoot => _visualRoot != null ? _visualRoot : transform;

    private void Awake()
    {
        EnsureVisuals();
        Clear();
    }

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

    private void OnDisable()
    {
        if (_controllerReadyChannel != null)
            _controllerReadyChannel.OnEventRaised -= HandleControllerReady;
    }

    public bool CanInteract => IsActive && !IsCompleted && _controller != null;

    public bool TryInteract()
    {
        if (!CanInteract)
            return false;

        return _controller.TryStartQTE(this);
    }

    public void SetPatternActive(bool active)
    {
        EnsureVisuals();

        IsActive = active;
        IsCompleted = false;

        SetVisualVisible(_activeVisualInstance, active);
        SetVisualVisible(_completedVisualInstance, false);
    }

    public void MarkCompleted()
    {
        IsCompleted = true;

        SetVisualVisible(_activeVisualInstance, false);
        SetVisualVisible(_completedVisualInstance, true);
    }

    public void Clear()
    {
        IsActive = false;
        IsCompleted = false;

        SetVisualVisible(_activeVisualInstance, false);
        SetVisualVisible(_completedVisualInstance, false);
    }

    private void HandleControllerReady(MutarusQTEPatternController controller)
    {
        _controller = controller;
    }

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
}
