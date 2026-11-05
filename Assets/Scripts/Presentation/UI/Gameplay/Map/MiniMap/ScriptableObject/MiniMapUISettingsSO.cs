/*
 * Role hierarchy: Presentation > UI > Gameplay > Map > MiniMap > ScriptableObject
 * Primary responsibility: Defines the MiniMapUISettingsSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

[CreateAssetMenu(fileName = "MiniMapUISettings", menuName = "Game/UI/Mini Map Settings")]
public class MiniMapUISettingsSO : ScriptableObject
{
    [Header("Shared sector-type sprites")]
    [SerializeField] private SectorMapIconSetSO _sectorIcons;
    public SectorMapIconSetSO SectorIcons => _sectorIcons;
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

    [Header("Raid Landmarks (optional, existing icons are placeholders)")]
    [SerializeField] private Sprite _hatchIcon;
    [SerializeField] private Sprite _generatorIcon;
    [SerializeField] private Sprite _extractionIcon;
    [SerializeField] private Sprite _merchantIcon;
    [SerializeField] private Sprite _armoryIcon;
    [SerializeField] private Sprite _medicalIcon;
    [SerializeField] private Sprite _vaultIcon;

    public Sprite GetLandmarkIcon(RaidRoomRole role, bool extraction = false)
    {
        var kind = SectorMapIconPolicy.KindForRole(role, extraction);
        if (_sectorIcons != null && kind != SectorMapIconKind.None) return _sectorIcons.GetIcon(kind);
        Sprite icon = extraction ? _extractionIcon : role switch
        {
            RaidRoomRole.Hatch => _hatchIcon,
            RaidRoomRole.Generator => _generatorIcon,
            RaidRoomRole.Merchant => _merchantIcon,
            RaidRoomRole.Armory => _armoryIcon,
            RaidRoomRole.Medical => _medicalIcon,
            RaidRoomRole.Vault => _vaultIcon,
            RaidRoomRole.UnknownCandidate => _lockedIcon,
            _ => null
        };
        if (icon != null) return icon;
        if (extraction || role == RaidRoomRole.Hatch || role == RaidRoomRole.Generator || role == RaidRoomRole.Boss)
            return _bossIcon != null ? _bossIcon : _namedIcon;
        return role != RaidRoomRole.None && role != RaidRoomRole.Normal ? _namedIcon : null;
    }

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
