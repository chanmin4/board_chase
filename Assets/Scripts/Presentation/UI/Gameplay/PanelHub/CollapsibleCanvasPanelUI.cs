/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub
 * Primary responsibility: Presents CollapsibleCanvasPanelUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CollapsibleCanvasPanelUI : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private CanvasGroup _contentGroup;

    [Header("Toggle")]
    [SerializeField] private Button _toggleButton;
    [SerializeField] private Image _toggleImage;
    [SerializeField] private Sprite _collapsedSprite;
    [SerializeField] private Sprite _expandedSprite;

    [Header("Default")]
    [SerializeField] private bool _defaultExpanded = false;

    public bool IsExpanded { get; private set; }
    public event Action<bool> OnExpandedChanged;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        ApplyExpanded(_defaultExpanded, false);
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_toggleButton != null)
            _toggleButton.onClick.AddListener(Toggle);

        ApplyExpanded(IsExpanded, false);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_toggleButton != null)
            _toggleButton.onClick.RemoveListener(Toggle);
    }

    // Purpose: Performs the reset to default workflow for this component.
    public void ResetToDefault()
    {
        SetExpanded(_defaultExpanded);
    }

    // Purpose: Toggles the toggle state.
    public void Toggle()
    {
        SetExpanded(!IsExpanded);
    }

    // Purpose: Updates the expanded state.
    public void SetExpanded(bool expanded)
    {
        ApplyExpanded(expanded, true);
    }

    // Purpose: Applies the requested expanded change.
    private void ApplyExpanded(bool expanded, bool notify)
    {
        IsExpanded = expanded;

        if (_contentGroup != null)
        {
            _contentGroup.alpha = expanded ? 1f : 0f;
            _contentGroup.interactable = expanded;
            _contentGroup.blocksRaycasts = expanded;
        }
        else
        {
            Debug.LogWarning("[CollapsibleCanvasPanelUI] Content Group is missing.", this);
        }

        if (_toggleImage != null)
        {
            Sprite sprite = expanded ? _expandedSprite : _collapsedSprite;
            if (sprite != null)
                _toggleImage.sprite = sprite;
        }

        if (notify)
            OnExpandedChanged?.Invoke(expanded);
    }
}
