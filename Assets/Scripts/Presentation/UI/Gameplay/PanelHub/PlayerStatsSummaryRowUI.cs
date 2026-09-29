/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub
 * Primary responsibility: Presents PlayerStatsSummaryRowUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player stat summary row UI.
/// One compact row displays an icon, stat name, and resolved value.
/// Use this as a prefab under PlayerStatsSummaryPanelUI.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStatsSummaryRowUI : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Optional stat icon. When a row definition has no icon, the prefab's placeholder remains visible.")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _valueText;

    private Sprite _placeholderIcon;

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_iconImage != null)
            _placeholderIcon = _iconImage.sprite;
    }

    // Purpose: Binds bind to the active runtime or UI target.
    public void Bind(Sprite icon, string statName, string statValue)
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = icon != null ? icon : _placeholderIcon;
            _iconImage.enabled = true;
        }

        if (_nameText != null)
            _nameText.text = statName;

        if (_valueText != null)
            _valueText.text = statValue;
    }
}
