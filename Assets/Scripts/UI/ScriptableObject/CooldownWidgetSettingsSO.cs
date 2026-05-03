using UnityEngine;

[CreateAssetMenu(
    fileName = "CooldownWidgetSettings",
    menuName = "Game/UI/Cooldown Widget Settings")]
public class CooldownWidgetSettingsSO : ScriptableObject
{
    [Header("Visual")]
    [SerializeField] private Sprite _icon;
    [SerializeField] private Color _iconColor = Color.white;
    [SerializeField] private Color _cooldownFillColor = Color.white;
    [SerializeField] private Color _readyFillColor = new Color(1f, 1f, 1f, 0.15f);

    [Header("Behavior")]
    [SerializeField] private bool _hideFillWhenReady = false;
    [SerializeField] private bool _showReadyText = false;
    [SerializeField] private string _readyText = "READY";
    [SerializeField] private string _cooldownFormat = "0.0";

    public Sprite Icon => _icon;
    public Color IconColor => _iconColor;
    public Color CooldownFillColor => _cooldownFillColor;
    public Color ReadyFillColor => _readyFillColor;
    public bool HideFillWhenReady => _hideFillWhenReady;
    public bool ShowReadyText => _showReadyText;
    public string ReadyText => _readyText;
    public string CooldownFormat => _cooldownFormat;
}
