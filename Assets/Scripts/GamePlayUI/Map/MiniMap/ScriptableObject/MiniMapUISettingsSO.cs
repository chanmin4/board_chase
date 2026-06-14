using UnityEngine;

[CreateAssetMenu(fileName = "MiniMapUISettings", menuName = "Game/UI/Mini Map Settings")]
public class MiniMapUISettingsSO : ScriptableObject
{
    [Header("Colors")]
    [SerializeField] private Color _playerColor = new Color(0.2f, 0.75f, 1f, 0.85f);
    [SerializeField] private Color _neutralColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
    [SerializeField] private Color _virusColor = new Color(0.9f, 0.15f, 0.25f, 0.85f);
    [SerializeField] private Color _lockedColor = new Color(0.15f, 0.15f, 0.15f, 0.75f);
    [SerializeField] private Color _startSectorColor = new Color(0.25f, 0.75f, 0.9f, 0.65f);

    [Header("Named Reserved")]
    [SerializeField, Range(0f, 1f)] private float _namedReservedCellAlpha = 0.45f;

    [Header("Icons")]
    [SerializeField] private Sprite _lockedIcon;
    [SerializeField] private Sprite _namedIcon;
    [SerializeField] private Sprite _bossIcon;

    public Color PlayerColor => _playerColor;
    public Color NeutralColor => _neutralColor;
    public Color VirusColor => _virusColor;
    public Color LockedColor => _lockedColor;
    public Color StartSectorColor => _startSectorColor;

    public float NamedReservedCellAlpha => _namedReservedCellAlpha;

    public Sprite LockedIcon => _lockedIcon;
    public Sprite NamedIcon => _namedIcon;
    public Sprite BossIcon => _bossIcon;
}
