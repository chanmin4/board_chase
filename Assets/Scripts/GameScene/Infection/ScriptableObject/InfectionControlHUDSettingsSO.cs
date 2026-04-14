using UnityEngine;

[CreateAssetMenu(fileName = "InfectionControlHUDSettings", menuName = "Game/Infection Control HUD Settings")]
public class InfectionControlHUDSettingsSO : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private bool _showPercent = true;
    [SerializeField] private bool _showRawValue = false;
    [SerializeField] private bool _showDrainPerSecond = true;

    [Header("Fill")]
    [SerializeField] private bool _smoothFill = true;
    [SerializeField, Min(0f)] private float _smoothSpeed = 8f;

    [Header("Colors")]
    [SerializeField] private Color _safeColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField] private Color _warningColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _dangerColor = new Color(1f, 0.2f, 0.25f, 1f);

    [Header("Thresholds")]
    [SerializeField, Range(0f, 1f)] private float _warningThreshold = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _dangerThreshold = 0.25f;

    public bool ShowPercent => _showPercent;
    public bool ShowRawValue => _showRawValue;
    public bool ShowDrainPerSecond => _showDrainPerSecond;

    public bool SmoothFill => _smoothFill;
    public float SmoothSpeed => _smoothSpeed;

    public Color SafeColor => _safeColor;
    public Color WarningColor => _warningColor;
    public Color DangerColor => _dangerColor;

    public float WarningThreshold => _warningThreshold;
    public float DangerThreshold => _dangerThreshold;
}
