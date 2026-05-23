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

    private void Awake()
    {
        ApplyExpanded(_defaultExpanded, false);
    }

    private void OnEnable()
    {
        if (_toggleButton != null)
            _toggleButton.onClick.AddListener(Toggle);

        ApplyExpanded(IsExpanded, false);
    }

    private void OnDisable()
    {
        if (_toggleButton != null)
            _toggleButton.onClick.RemoveListener(Toggle);
    }

    public void ResetToDefault()
    {
        SetExpanded(_defaultExpanded);
    }

    public void Toggle()
    {
        SetExpanded(!IsExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        ApplyExpanded(expanded, true);
    }

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