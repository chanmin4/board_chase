using UnityEngine;

[CreateAssetMenu(fileName = "FullMapUISettings", menuName = "Game/UI/Full Map Settings")]
public class FullMapUISettingsSO : ScriptableObject
{
    [Header("Layout")]
    [Tooltip("Base RectTransform size applied to every generated full-map cell.")]
    [SerializeField] private Vector2 _cellSize = new Vector2(86f, 46f);

    [Tooltip("Empty gap between adjacent cells. Final center-to-center distance is Cell Size + Cell Spacing.")]
    [SerializeField] private Vector2 _cellSpacing = new Vector2(10f, 10f);

    [Tooltip("Extra anchored-position offset applied per row. Positive X tilts the board to the right.")]
    [SerializeField] private Vector2 _rowOffset = new Vector2(26f, 0f);

    [Tooltip("Z rotation applied to each cell. Use 0 if the cell sprite itself is already slanted.")]
    [SerializeField] private float _cellRotationZ = 0f;

    [Tooltip("Padding around the generated map content before auto-fit is applied.")]
    [SerializeField] private Vector2 _contentPadding = new Vector2(120f, 90f);

    [Tooltip("If enabled, larger Z coordinates are drawn higher on the UI.")]
    [SerializeField] private bool _invertZOnScreen = true;

    [Header("Auto Fit")]
    [Tooltip("Scale and center the generated grid inside CellRoot automatically.")]
    [SerializeField] private bool _autoFitToRoot = true;

    [Tooltip("Padding reserved inside CellRoot when auto-fit calculates scale.")]
    [SerializeField] private Vector2 _fitPadding = new Vector2(80f, 80f);

    [Tooltip("Maximum auto-fit scale. Keeps small 2x2 maps from becoming too large.")]
    [SerializeField, Min(0.1f)] private float _maxAutoScale = 1.35f;

    [Tooltip("Minimum auto-fit scale. Keeps larger maps readable.")]
    [SerializeField, Min(0.1f)] private float _minAutoScale = 0.45f;

    [Header("Background Sprites")]
    [Tooltip("BG sprite for the current player sector.")]
    [SerializeField] private Sprite _currentSectorBackgroundSprite;

    [Tooltip("BG sprite for active sectors where the player is not currently located.")]
    [SerializeField] private Sprite _activeSectorBackgroundSprite;

    [Tooltip("BG sprite for inactive, locked, or unopened sectors.")]
    [SerializeField] private Sprite _inactiveSectorBackgroundSprite;

    [Header("Room Icon Sprites")]
    [Tooltip("Icon shown for the start sector.")]
    [SerializeField] private Sprite _startRoomIcon;

    [Tooltip("Icon shown for normal battle sectors.")]
    [SerializeField] private Sprite _normalBattleRoomIcon;

    [Tooltip("Icon shown for named sectors.")]
    [SerializeField] private Sprite _namedRoomIcon;

    [Tooltip("Icon shown for treasure sectors.")]
    [SerializeField] private Sprite _treasureRoomIcon;

    [Tooltip("Icon shown for shop sectors.")]
    [SerializeField] private Sprite _shopRoomIcon;

    [Tooltip("Icon shown for boss sectors.")]
    [SerializeField] private Sprite _bossRoomIcon;

    [Tooltip("Icon shown for rooms that are not cleared/discovered yet. Use a question-mark icon here.")]
    [SerializeField] private Sprite _undiscoveredRoomIcon;

    [Header("Cell Border")]
    [Tooltip("Turns on FullMapCellUI.Border. Border is a shared outline/frame overlay above the BG sprite. Disable this if each BG sprite already contains its own border.")]
    [SerializeField] private bool _showCellBorder = true;

    [Header("Visibility")]
    [Tooltip("Alpha for active/opened cells.")]
    [SerializeField, Range(0f, 1f)] private float _activeCellAlpha = 1f;

    [Tooltip("Alpha for inactive/locked/unopened cells.")]
    [SerializeField, Range(0f, 1f)] private float _inactiveCellAlpha = 0.45f;

    [Tooltip("Minimum visible alpha while the full map is open. Closing the full map still uses alpha 0.")]
    [SerializeField, Range(0f, 1f)] private float _minimumVisibleAlpha = 0.2f;

    [Header("Labels")]
    [Tooltip("Coord used as A1. Default means coord (0,0) displays as A1.")]
    [SerializeField] private Vector2Int _coordinateLabelOriginCoord = Vector2Int.zero;

    [Tooltip("Show A1/B3 style coordinate label on each visible cell.")]
    [SerializeField] private bool _showRoomLabel = true;

    [Tooltip("Text color for the room coordinate label.")]
    [SerializeField] private Color _roomLabelColor = new Color(0.8f, 1f, 1f, 1f);

    [Tooltip("Label shown on the generated start sector. Start sector uses this instead of A1/B2 coordinates.")]
    [SerializeField] private string _startSectorLabelText = "START";

    [Tooltip("Text shown to the left of the full-map opacity slider.")]
    [SerializeField] private string _opacityLabelText = "\uD22C\uBA85\uB3C4";

    public Vector2 CellSize => _cellSize;
    public Vector2 CellSpacing => _cellSpacing;
    public Vector2 RowOffset => _rowOffset;
    public float CellRotationZ => _cellRotationZ;
    public Vector2 ContentPadding => _contentPadding;
    public bool InvertZOnScreen => _invertZOnScreen;

    public bool AutoFitToRoot => _autoFitToRoot;
    public Vector2 FitPadding => _fitPadding;
    public float MaxAutoScale => _maxAutoScale;
    public float MinAutoScale => _minAutoScale;

    public bool ShowCellBorder => _showCellBorder;
    public float ActiveCellAlpha => _activeCellAlpha;
    public float InactiveCellAlpha => _inactiveCellAlpha;
    public float MinimumVisibleAlpha => _minimumVisibleAlpha;

    public Vector2Int CoordinateLabelOriginCoord => _coordinateLabelOriginCoord;
    public bool ShowRoomLabel => _showRoomLabel;
    public Color RoomLabelColor => _roomLabelColor;
    public string StartSectorLabelText => _startSectorLabelText;
    public string OpacityLabelText => _opacityLabelText;

    public Sprite GetBackgroundSprite(bool isCurrentSector, bool isInactive)
    {
        if (isCurrentSector)
            return _currentSectorBackgroundSprite;

        return isInactive
            ? _inactiveSectorBackgroundSprite
            : _activeSectorBackgroundSprite;
    }

    public Sprite GetRoomIcon(
        StageRoomType roomType,
        bool isStartSector,
        bool showUndiscoveredIcon)
    {
        if (showUndiscoveredIcon)
            return _undiscoveredRoomIcon;

        if (isStartSector)
            return _startRoomIcon;

        switch (roomType)
        {
            case StageRoomType.Start:
                return _startRoomIcon;

            case StageRoomType.NormalBattle:
                return _normalBattleRoomIcon;

            case StageRoomType.Named:
                return _namedRoomIcon;

            case StageRoomType.Treasure:
                return _treasureRoomIcon;

            case StageRoomType.Shop:
                return _shopRoomIcon;

            case StageRoomType.Boss:
                return _bossRoomIcon;

            default:
                return null;
        }
    }
}
