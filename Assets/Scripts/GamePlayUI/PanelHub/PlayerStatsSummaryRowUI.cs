using TMPro;
using UnityEngine;

/// <summary>
/// Player stat summary row UI.
/// One row only displays a stat name and a stat value.
/// Use this as a prefab under PlayerStatsSummaryPanelUI.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStatsSummaryRowUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _valueText;

    public void Bind(string statName, string statValue)
    {
        if (_nameText != null)
            _nameText.text = statName;

        if (_valueText != null)
            _valueText.text = statValue;
    }
}