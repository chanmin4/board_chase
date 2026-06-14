using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FullMapUI : MonoBehaviour, IUIOverlayLifecycle
{
    [Header("Input")]
    [Tooltip("Subscribes to InputReader.MapEvent and toggles the full map.")]
    [SerializeField] private InputReader _inputReader;

    [Header("Events")]
    [Tooltip("Map snapshot published by SectorOccupancyManager. FullMap uses the same source as MiniMap.")]
    [SerializeField] private SectorMapSnapshotEventChannelSO _mapSnapshotChangedChannel;

    [Tooltip("Raised when FullMap needs the latest map snapshot.")]
    [SerializeField] private VoidEventChannelSO _requestMapSnapshotChannel;

    [Header("Settings")]
    [SerializeField] private FullMapUISettingsSO _settings;

    [Header("UI")]
    [Tooltip("CanvasGroup for the whole FullMap panel.")]
    [SerializeField] private CanvasGroup _rootCanvasGroup;

    [Tooltip("Optional. Reuses the same alpha controller style as MiniMap.")]
    [SerializeField] private UICanvasGroupOpacity _uicanvasGroupOpacity;

    [Tooltip("Opacity slider under the FullMap panel.")]
    [SerializeField] private Slider _alphaSlider;

    [Tooltip("Text placed to the left of the opacity slider.")]
    [SerializeField] private TextMeshProUGUI _opacityLabelText;

    [Tooltip("MiniMap CanvasGroup hidden while FullMap is open. This replaces HUD-wide hiding.")]
    [SerializeField] private CanvasGroup _miniMapCanvasGroup;
    [SerializeField] private UICanvasGroupOpacity _miniMapOpacity;

    [Tooltip("Alpha restored to MiniMap when FullMap closes.")]
    [SerializeField, Range(0f, 1f)] private float _miniMapVisibleAlpha = 1f;

    [Tooltip("Root RectTransform where generated FullMap cells are placed.")]
    [SerializeField] private RectTransform _cellRoot;

    [Tooltip("Prefab used for each generated FullMap cell.")]
    [SerializeField] private FullMapCellUI _cellPrefab;

    [Header("Behavior")]
    [SerializeField] private bool _startHidden = true;
    [SerializeField] private bool _requestSnapshotWhenOpened = true;

    [Header("Overlay")]
    [Tooltip("If assigned, FullMap is opened through UIOverlayManager instead of directly toggling its own CanvasGroup.")]
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;

    [Tooltip("Overlay id used when sending open/close/toggle requests.")]
    [SerializeField] private UIOverlayId _overlayId = UIOverlayId.FullMap;

    [Tooltip("If true and Overlay Request Channel is assigned, Map key toggles through UIOverlayManager.")]
    [SerializeField] private bool _useOverlayManager = true;

    private readonly Dictionary<Vector2Int, FullMapCellUI> _cellByCoord = new();
    private readonly List<FullMapCellUI> _cellPool = new();

    private SectorMapSnapshot _latestSnapshot;
    private bool _hasSnapshot;
    private bool _isOpen;

    private FullMapUISettingsSO Settings => _settings;

    private void Reset()
    {
        _rootCanvasGroup = GetComponent<CanvasGroup>();
        _uicanvasGroupOpacity = GetComponent<UICanvasGroupOpacity>();
    }

    private void Awake()
    {
        if (_rootCanvasGroup == null)
            _rootCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (_cellRoot == null)
            _cellRoot = transform as RectTransform;

        ApplyOpacityLabelText();
    }

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.MapEvent += Toggle;

        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised += OnMapSnapshotChanged;

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.AddListener(OnAlphaSliderChanged);

        ApplyOpacityLabelText();

        if (_startHidden)
            SetVisible(false, immediate: true);
        else
            SetVisible(_isOpen, immediate: true);

        SetMiniMapVisible(!_isOpen);
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.MapEvent -= Toggle;

        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised -= OnMapSnapshotChanged;

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.RemoveListener(OnAlphaSliderChanged);

        SetMiniMapVisible(true);
    }

    public void Toggle()
    {
        if (_useOverlayManager && _overlayRequestChannel != null)
        {
            _overlayRequestChannel.Toggle(_overlayId);
            return;
        }

        SetOpen(!_isOpen);
    }

    public void SetOpen(bool open)
    {
        if (_isOpen == open)
            return;

        _isOpen = open;
        SetVisible(_isOpen, immediate: false);
        SetMiniMapVisible(!_isOpen);

        if (_isOpen)
            RequestSnapshotAndRefresh();
    }

    public void OnOverlayShown()
    {
        _isOpen = true;
        SetVisible(true, immediate: false);
        SetMiniMapVisible(false);
        RequestSnapshotAndRefresh();
    }

    public void OnOverlayHidden()
    {
        _isOpen = false;
        SetVisible(false, immediate: false);
        SetMiniMapVisible(true);
    }

    private void SetVisible(bool visible, bool immediate)
    {
        SetMiniMapVisible(!visible);

        if (_rootCanvasGroup != null)
        {
            _rootCanvasGroup.interactable = visible;
            _rootCanvasGroup.blocksRaycasts = visible;
        }

        if (!visible)
        {
            if (_uicanvasGroupOpacity != null)
                _uicanvasGroupOpacity.ApplyImmediate(0f);

            if (_rootCanvasGroup != null)
                _rootCanvasGroup.alpha = 0f;

            return;
        }

        float alpha = ResolveVisibleAlpha();

        if (_uicanvasGroupOpacity != null)
        {
            if (immediate)
                _uicanvasGroupOpacity.ApplyImmediate(alpha);
            else
                _uicanvasGroupOpacity.Show(alpha);

            return;
        }

        if (_rootCanvasGroup != null)
            _rootCanvasGroup.alpha = alpha;
    }

    private float ResolveVisibleAlpha()
    {
        float minimumVisibleAlpha = _settings != null
            ? _settings.MinimumVisibleAlpha
            : 0f;

        if (_alphaSlider != null)
            return Mathf.Clamp(_alphaSlider.value, minimumVisibleAlpha, 1f);

        return Mathf.Clamp(1f, minimumVisibleAlpha, 1f);
    }

    private void OnAlphaSliderChanged(float alpha)
    {
        float visibleAlpha = ResolveVisibleAlpha();

        if (_uicanvasGroupOpacity != null)
        {
            if (_isOpen)
                _uicanvasGroupOpacity.SetAlpha(visibleAlpha);

            return;
        }

        if (_rootCanvasGroup != null && _isOpen)
            _rootCanvasGroup.alpha = visibleAlpha;
    }

    private void OnMapSnapshotChanged(SectorMapSnapshot snapshot)
    {
        _latestSnapshot = snapshot;
        _hasSnapshot = true;

        if (_isOpen)
            RefreshMap();
    }

    private void RequestSnapshotAndRefresh()
    {
        if (_requestSnapshotWhenOpened && _requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.RaiseEvent();

        RefreshMap();
    }

    private void RefreshMap()
    {
        if (_settings == null)
        {
            Debug.LogWarning("[FullMapUI] FullMapUISettingsSO is missing. Assign a settings asset before opening the full map.", this);
            return;
        }

        if (!_hasSnapshot || _cellRoot == null || _cellPrefab == null)
            return;

        SectorMapCellSnapshot[] cells = _latestSnapshot.cells;

        if (cells == null || cells.Length == 0)
        {
            HideAllCells();
            return;
        }

        CalculateBounds(cells, out int minX, out int maxX, out int minY, out int maxY);
        Rect layoutRect = CalculateLayoutRect(cells, minX, maxX, minY, maxY);
        float mapScale = CalculateAutoScale(layoutRect);
        Vector2 centerOffset = CalculateCenterOffset(layoutRect, mapScale);

        HideAllCells();

        for (int i = 0; i < cells.Length; i++)
        {
            SectorMapCellSnapshot snapshot = cells[i];
            FullMapCellUI cell = GetOrCreateCell(snapshot.coord);

            Vector2 rawPosition = CalculateCellPosition(snapshot.coord, minX, maxX, minY, maxY);
            Vector2 position = TransformBoardPosition(rawPosition, mapScale, centerOffset);

            cell.ApplyLayout(
                position,
                mapScale,
                Settings.CellRotationZ);

            ApplyCell(cell, snapshot);
        }

        ApplyContentSize(layoutRect, mapScale);
    }

    private FullMapCellUI GetOrCreateCell(Vector2Int coord)
    {
        if (_cellByCoord.TryGetValue(coord, out FullMapCellUI cell) && cell != null)
            return cell;

        cell = Instantiate(_cellPrefab, _cellRoot);
        cell.name = $"FullMapCell_{FormatCellObjectName(coord)}";

        _cellByCoord[coord] = cell;
        _cellPool.Add(cell);
        return cell;
    }

    private void ApplyCell(FullMapCellUI cell, SectorMapCellSnapshot snapshot)
    {
        bool isCurrent = snapshot.coord == _latestSnapshot.currentSectorCoord;
        bool isInactive = snapshot.isLocked || !snapshot.isRevealed;
        bool showUndiscoveredIcon = ShouldShowUndiscoveredIcon(snapshot);

        Sprite backgroundSprite = Settings.GetBackgroundSprite(isCurrent, isInactive);
        Sprite roomIconSprite = Settings.GetRoomIcon(
            snapshot.roomType,
            snapshot.isStartSector,
            showUndiscoveredIcon);

        string roomLabel = Settings.ShowRoomLabel
            ? FormatRoomLabel(snapshot)
            : string.Empty;

        cell.SetCell(
            backgroundSprite,
            roomIconSprite,
            Settings.RoomLabelColor,
            roomLabel,
            Settings.ShowRoomLabel,
            Settings.ShowCellBorder,
            GetCellAlpha(snapshot));
    }

    private Vector2 CalculateCellPosition(Vector2Int coord, int minX, int maxX, int minY, int maxY)
    {
        int xIndex = coord.x - minX;
        int yIndex = Settings.InvertZOnScreen
            ? maxY - coord.y
            : coord.y - minY;

        Vector2 padding = Settings.ContentPadding;
        Vector2 stride = Settings.CellSize + Settings.CellSpacing;
        Vector2 rowOffset = Settings.RowOffset;

        return new Vector2(
            padding.x + xIndex * stride.x + yIndex * rowOffset.x,
            -padding.y - yIndex * stride.y + yIndex * rowOffset.y);
    }

    private Rect CalculateLayoutRect(
        SectorMapCellSnapshot[] cells,
        int minX,
        int maxX,
        int minY,
        int maxY)
    {
        Vector2 halfSize = Settings.CellSize * 0.5f;
        bool initialized = false;
        float xMin = 0f;
        float xMax = 0f;
        float yMin = 0f;
        float yMax = 0f;

        for (int i = 0; i < cells.Length; i++)
        {
            Vector2 position = CalculateCellPosition(cells[i].coord, minX, maxX, minY, maxY);

            float cellXMin = position.x - halfSize.x;
            float cellXMax = position.x + halfSize.x;
            float cellYMin = position.y - halfSize.y;
            float cellYMax = position.y + halfSize.y;

            if (!initialized)
            {
                xMin = cellXMin;
                xMax = cellXMax;
                yMin = cellYMin;
                yMax = cellYMax;
                initialized = true;
                continue;
            }

            xMin = Mathf.Min(xMin, cellXMin);
            xMax = Mathf.Max(xMax, cellXMax);
            yMin = Mathf.Min(yMin, cellYMin);
            yMax = Mathf.Max(yMax, cellYMax);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private float CalculateAutoScale(Rect layoutRect)
    {
        if (!Settings.AutoFitToRoot || _cellRoot == null)
            return 1f;

        Vector2 available = _cellRoot.rect.size - Settings.FitPadding * 2f;

        if (available.x <= 0f || available.y <= 0f ||
            layoutRect.width <= 0f || layoutRect.height <= 0f)
        {
            return 1f;
        }

        float scaleX = available.x / layoutRect.width;
        float scaleY = available.y / layoutRect.height;
        float scale = Mathf.Min(scaleX, scaleY);

        return Mathf.Clamp(
            scale,
            Settings.MinAutoScale,
            Settings.MaxAutoScale);
    }

    private Vector2 CalculateCenterOffset(Rect layoutRect, float mapScale)
    {
        if (!Settings.AutoFitToRoot)
            return Vector2.zero;

        return -layoutRect.center * mapScale;
    }

    private static Vector2 TransformBoardPosition(Vector2 rawPosition, float mapScale, Vector2 centerOffset)
    {
        return rawPosition * mapScale + centerOffset;
    }

    private void ApplyContentSize(Rect layoutRect, float mapScale)
    {
        if (_cellRoot == null || Settings.AutoFitToRoot)
            return;

        Vector2 padding = Settings.ContentPadding;
        _cellRoot.sizeDelta = new Vector2(
            layoutRect.width * mapScale + padding.x * 2f,
            layoutRect.height * mapScale + padding.y * 2f);
    }

    private void HideAllCells()
    {
        for (int i = 0; i < _cellPool.Count; i++)
        {
            if (_cellPool[i] != null)
                _cellPool[i].SetMissing();
        }
    }

    private static void CalculateBounds(
        SectorMapCellSnapshot[] cells,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        minX = int.MaxValue;
        maxX = int.MinValue;
        minY = int.MaxValue;
        maxY = int.MinValue;

        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int coord = cells[i].coord;
            minX = Mathf.Min(minX, coord.x);
            maxX = Mathf.Max(maxX, coord.x);
            minY = Mathf.Min(minY, coord.y);
            maxY = Mathf.Max(maxY, coord.y);
        }
    }

    private float GetCellAlpha(SectorMapCellSnapshot snapshot)
    {
        return snapshot.isRevealed && !snapshot.isLocked
            ? Settings.ActiveCellAlpha
            : Settings.InactiveCellAlpha;
    }

    private bool ShouldShowUndiscoveredIcon(SectorMapCellSnapshot snapshot)
    {
        return !snapshot.isStartSector && !snapshot.isRevealed;
    }

    private string FormatRoomLabel(SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isStartSector)
            return Settings.StartSectorLabelText;

        return FormatCoordinateLabel(snapshot.coord);
    }

    private string FormatCellObjectName(Vector2Int coord)
    {
        string label = FormatCoordinateLabel(coord);

        if (!string.IsNullOrWhiteSpace(label))
            return label;

        return $"{coord.x}_{coord.y}";
    }

    private string FormatCoordinateLabel(Vector2Int coord)
    {
        Vector2Int origin = Settings.CoordinateLabelOriginCoord;
        int xIndex = coord.x - origin.x;
        int yIndex = coord.y - origin.y;

        if (xIndex < 0 || yIndex < 0)
            return string.Empty;

        return $"{FormatAlphabetIndex(xIndex)}{yIndex + 1}";
    }

    private static string FormatAlphabetIndex(int index)
    {
        if (index < 0)
            return string.Empty;

        index += 1;
        string result = string.Empty;

        while (index > 0)
        {
            int remainder = (index - 1) % 26;
            result = (char)('A' + remainder) + result;
            index = (index - 1) / 26;
        }

        return result;
    }

    private void ApplyOpacityLabelText()
    {
        if (_opacityLabelText == null || _settings == null)
            return;

        _opacityLabelText.text = _settings.OpacityLabelText;
    }

    private void SetMiniMapVisible(bool visible)
    {
        if (_miniMapOpacity != null)
        {
            if (visible)
                _miniMapOpacity.ApplyImmediate(_miniMapVisibleAlpha);
            else
                _miniMapOpacity.ApplyImmediate(0f);
        }

        if (_miniMapCanvasGroup != null)
        {
            _miniMapCanvasGroup.alpha = visible ? _miniMapVisibleAlpha : 0f;
            _miniMapCanvasGroup.interactable = visible;
            _miniMapCanvasGroup.blocksRaycasts = visible;
        }
    }
}
