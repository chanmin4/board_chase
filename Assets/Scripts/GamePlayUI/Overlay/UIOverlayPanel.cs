using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UIOverlayPanel : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private UIOverlayId _id;

    [Header("Visibility")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private bool _hideOnAwake = true;

    [Header("Gameplay Input")]
    [Tooltip("If true, blocks only attack-style input while this overlay is open: shoot, special shoot, and shockwave. Movement, dash, and interact stay enabled.")]
    [SerializeField] private bool _blockAttackInputWhileOpen = false;

    [Header("Events")]
    [SerializeField] private UnityEvent _onShown;
    [SerializeField] private UnityEvent _onHidden;

    private readonly List<IUIOverlayLifecycle> _lifecycleTargets = new();

    public UIOverlayId Id => _id;
    public bool BlockAttackInputWhileOpen => _blockAttackInputWhileOpen;

    private void Reset()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        EnsureCanvasGroup();
        CacheLifecycleTargets();

        if (_hideOnAwake)
            SetVisible(false);
    }

    public void Show()
    {
        EnsureCanvasGroup();
        CacheLifecycleTargets();
        SetVisible(true);
        NotifyShown();
        _onShown?.Invoke();
    }

    public void Hide()
    {
        EnsureCanvasGroup();
        CacheLifecycleTargets();
        SetVisible(false);
        NotifyHidden();
        _onHidden?.Invoke();
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

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

    private void NotifyShown()
    {
        for (int i = 0; i < _lifecycleTargets.Count; i++)
            _lifecycleTargets[i]?.OnOverlayShown();
    }

    private void NotifyHidden()
    {
        for (int i = 0; i < _lifecycleTargets.Count; i++)
            _lifecycleTargets[i]?.OnOverlayHidden();
    }
}
