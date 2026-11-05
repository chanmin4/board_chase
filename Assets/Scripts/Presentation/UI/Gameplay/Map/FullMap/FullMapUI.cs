/*
 * Role hierarchy: Presentation > UI > Gameplay > Map > FullMap
 * Primary responsibility: Presents FullMapUI state and handles its user-facing UI interaction.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class FullMapUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const string StringTable = "UI_Map";
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

    private readonly Dictionary<Vector2Int, FullMapCellUI> _cellByCoord = new();
    private readonly List<FullMapCellUI> _cellPool = new();

    private SectorMapSnapshot _latestSnapshot;
    private bool _hasSnapshot;
    private bool _isOpen;
    private Coroutine _localizationRefreshRoutine;
    private RectTransform _mapContent;
    private Vector2 _panOffset;
    private Vector2 _dragPosition;
    private bool _dragging;
    private bool _pendingMapRefresh;
    private readonly Dictionary<Vector2Int, SectorMapCellSnapshot> _presentedCells = new();
    private readonly HashSet<Vector2Int> _visibleCoords = new();
    private Vector2 _lastCenterOffset;
    private bool _hasLayout;

    private void LateUpdate()
    {
        if (!_isOpen || !_pendingMapRefresh) return;
        _pendingMapRefresh = false;
        RefreshMap();
    }

    private FullMapUISettingsSO Settings => _settings;

    // Purpose: Initializes Inspector defaults for this component.
    private void Reset()
    {
        _rootCanvasGroup = GetComponent<CanvasGroup>();
        _uicanvasGroupOpacity = GetComponent<UICanvasGroupOpacity>();
    }

    // Purpose: Caches the component references required before runtime initialization.
    private void Awake()
    {
        if (_rootCanvasGroup == null)
            _rootCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (_cellRoot == null)
            _cellRoot = transform as RectTransform;

        EnsureMapViewport();
        ApplyOpacityLabelText();
    }

    // Purpose: Activates runtime subscriptions and refreshes the enabled state.
    private void OnEnable()
    {
        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised += OnMapSnapshotChanged;

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.AddListener(OnAlphaSliderChanged);

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        RequestLocalizationRefresh();

        ApplyOpacityLabelText();

        if (_startHidden)
            SetVisible(false, immediate: true);
        else
            SetVisible(_isOpen, immediate: true);

        SetMiniMapVisible(!_isOpen);
    }

    // Purpose: Releases active subscriptions and transient state while disabled.
    private void OnDisable()
    {
        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised -= OnMapSnapshotChanged;

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.RemoveListener(OnAlphaSliderChanged);

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;

        if (_localizationRefreshRoutine != null)
        {
            StopCoroutine(_localizationRefreshRoutine);
            _localizationRefreshRoutine = null;
        }

        SetMiniMapVisible(true);
    }

    // Purpose: Updates the open state.
    public void SetOpen(bool open)
    {
        bool changed = _isOpen != open;
        if (open && changed) _panOffset = Vector2.zero;
        if (!open) _dragging = false;
        _isOpen = open;
        SetVisible(_isOpen, immediate: false);
        SetMiniMapVisible(!_isOpen);

        if (_isOpen && (changed || !_hasSnapshot))
            RequestSnapshotAndRefresh();
    }

    private void EnsureMapViewport()
    {
        if (_cellRoot == null || _mapContent != null) return;
        // Keep the authored panel rectangle. Never grow it to the map's bounds.
        if (_cellRoot.GetComponent<RectMask2D>() == null)
            _cellRoot.gameObject.AddComponent<RectMask2D>();
        Image hitArea = _cellRoot.GetComponent<Image>();
        if (hitArea == null)
        {
            hitArea = _cellRoot.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
        }
        hitArea.raycastTarget = true;
        _mapContent = (RectTransform)new GameObject("PannableMapContent", typeof(RectTransform)).transform;
        _mapContent.gameObject.layer = _cellRoot.gameObject.layer;
        _mapContent.SetParent(_cellRoot, false);
        _mapContent.anchorMin = _mapContent.anchorMax = _mapContent.pivot = Vector2.one * 0.5f;
        _mapContent.sizeDelta = Vector2.zero;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = _isOpen && eventData.button == PointerEventData.InputButton.Right &&
            _cellRoot != null && RectTransformUtility.RectangleContainsScreenPoint(
                _cellRoot, eventData.position, eventData.pressEventCamera) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _cellRoot, eventData.position, eventData.pressEventCamera, out _dragPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || eventData.button != PointerEventData.InputButton.Right) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _cellRoot, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
        _panOffset += local - _dragPosition;
        _dragPosition = local;
        _mapContent.anchoredPosition = _panOffset;
    }

    public void OnEndDrag(PointerEventData eventData) => _dragging = false;

    // Purpose: Updates the visible state.
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

    // Purpose: Resolves the visible alpha needed by this component.
    private float ResolveVisibleAlpha()
    {
        float minimumVisibleAlpha = _settings != null
            ? _settings.MinimumVisibleAlpha
            : 0f;

        if (_alphaSlider != null)
            return Mathf.Clamp(_alphaSlider.value, minimumVisibleAlpha, 1f);

        return Mathf.Clamp(1f, minimumVisibleAlpha, 1f);
    }

    // Purpose: Responds to the alpha slider changed callback.
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

    // Purpose: Responds to the map snapshot changed callback.
    private void OnMapSnapshotChanged(SectorMapSnapshot snapshot)
    {
        _latestSnapshot = snapshot;
        _hasSnapshot = true;

        _pendingMapRefresh = true;
    }

    // Purpose: Requests snapshot and refresh from the owning service.
    private void RequestSnapshotAndRefresh()
    {
        if (_requestSnapshotWhenOpened && _requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.RaiseEvent();

        RefreshMap();
    }

    // Purpose: Refreshes the displayed or cached map.
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
        EnsureMapViewport();
        // A readable local map, not a whole-stage thumbnail. The current room
        // always starts at the viewport centre; distant rooms are reached by pan.
        float mapScale = 1f;
        Vector2 centerOffset = -CalculateCellPosition(
            _latestSnapshot.currentSectorCoord, minX, maxX, minY, maxY);
        _mapContent.anchoredPosition = _panOffset;
        bool layoutChanged = !_hasLayout || centerOffset != _lastCenterOffset;
        _hasLayout = true;
        _lastCenterOffset = centerOffset;
        _visibleCoords.Clear();

        for (int i = 0; i < cells.Length; i++)
        {
            SectorMapCellSnapshot snapshot = cells[i];
            FullMapCellUI cell = GetOrCreateCell(snapshot.coord);
            _visibleCoords.Add(snapshot.coord);

            Vector2 rawPosition = CalculateCellPosition(snapshot.coord, minX, maxX, minY, maxY);
            Vector2 position = TransformBoardPosition(rawPosition, mapScale, centerOffset);

            if (layoutChanged || !_presentedCells.ContainsKey(snapshot.coord)) cell.ApplyLayout(
                position,
                mapScale,
                Settings.CellRotationZ);

            if (layoutChanged || !_presentedCells.TryGetValue(snapshot.coord, out var previous) ||
                !SamePresentation(previous, snapshot)) ApplyCell(cell, snapshot);
            _presentedCells[snapshot.coord] = snapshot;
        }

        foreach (var pair in _cellByCoord)
        {
            if (_visibleCoords.Contains(pair.Key) || !_presentedCells.ContainsKey(pair.Key)) continue;
            pair.Value.SetMissing();
            _presentedCells.Remove(pair.Key);
        }

    }

    private static bool SamePresentation(SectorMapCellSnapshot a, SectorMapCellSnapshot b) =>
        a.isExtraction == b.isExtraction && a.hasPublicRaidMarker == b.hasPublicRaidMarker &&
        a.isLocked == b.isLocked && a.isRevealed == b.isRevealed &&
        a.isStartSector == b.isStartSector && a.roomType == b.roomType && a.raidRole == b.raidRole;

    // Purpose: Returns the current or create cell.
    private FullMapCellUI GetOrCreateCell(Vector2Int coord)
    {
        if (_cellByCoord.TryGetValue(coord, out FullMapCellUI cell) && cell != null)
            return cell;

        cell = Instantiate(_cellPrefab, _mapContent);
        RectTransform cellRect = (RectTransform)cell.transform;
        cellRect.anchorMin = cellRect.anchorMax = cellRect.pivot = Vector2.one * 0.5f;
        cellRect.sizeDelta = Settings.CellSize;
        cell.name = $"FullMapCell_{FormatCellObjectName(coord)}";

        _cellByCoord[coord] = cell;
        _cellPool.Add(cell);
        return cell;
    }

    // Purpose: Applies the requested cell change.
    private void ApplyCell(FullMapCellUI cell, SectorMapCellSnapshot snapshot)
    {
        bool isCurrent = snapshot.coord == _latestSnapshot.currentSectorCoord;
        bool isExtraction = snapshot.isExtraction;
        bool isInactive = !isExtraction && !snapshot.hasPublicRaidMarker && (snapshot.isLocked || !snapshot.isRevealed);
        bool showUndiscoveredIcon = !isExtraction && !snapshot.hasPublicRaidMarker && ShouldShowUndiscoveredIcon(snapshot);

        Sprite backgroundSprite = Settings.GetBackgroundSprite(isCurrent, isInactive);
        Sprite roomIconSprite = Settings.GetRoomIcon(
            snapshot.roomType,
            snapshot.isStartSector,
            showUndiscoveredIcon);
        if (isExtraction || snapshot.hasPublicRaidMarker || snapshot.isRevealed)
            roomIconSprite = Settings.GetLandmarkIcon(snapshot.raidRole, isExtraction) ?? roomIconSprite;
        roomIconSprite = SectorMapIconPolicy.SharedIcon(snapshot, Settings.SectorIcons, roomIconSprite);

        string roomLabel = isExtraction
            ? Localize("Map_Extract", "Extract")
            : Settings.ShowRoomLabel ? FormatRoomLabel(snapshot) : string.Empty;
        if (snapshot.hasPublicRaidMarker && !isExtraction)
            roomLabel = RaidMapLabels.ForRole(snapshot.raidRole);
        bool showRoomLabel = isExtraction || snapshot.hasPublicRaidMarker || Settings.ShowRoomLabel;
        Color roomLabelColor = isExtraction
            ? new Color(0.2f, 1f, 0.55f, 1f)
            : Settings.RoomLabelColor;

        cell.SetCell(
            backgroundSprite,
            roomIconSprite,
            roomLabelColor,
            roomLabel,
            showRoomLabel,
            Localize("Map_Player", Settings.PlayerLabelText),
            isCurrent,
            Settings.PlayerLabelColor,
            Settings.ShowCellBorder,
            GetCellAlpha(snapshot));
    }

    // Purpose: Calculates cell position from the current inputs.
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

    // Purpose: Calculates layout rect from the current inputs.
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

    // Purpose: Calculates auto scale from the current inputs.
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

    // Purpose: Calculates center offset from the current inputs.
    private Vector2 CalculateCenterOffset(Rect layoutRect, float mapScale)
    {
        if (!Settings.AutoFitToRoot)
            return Vector2.zero;

        return -layoutRect.center * mapScale;
    }

    // Purpose: Performs the transform board position workflow for this component.
    private static Vector2 TransformBoardPosition(Vector2 rawPosition, float mapScale, Vector2 centerOffset)
    {
        return rawPosition * mapScale + centerOffset;
    }

    // Purpose: Applies the requested content size change.
    private void ApplyContentSize(Rect layoutRect, float mapScale)
    {
        if (_cellRoot == null || Settings.AutoFitToRoot)
            return;

        Vector2 padding = Settings.ContentPadding;
        _cellRoot.sizeDelta = new Vector2(
            layoutRect.width * mapScale + padding.x * 2f,
            layoutRect.height * mapScale + padding.y * 2f);
    }

    // Purpose: Hides the all cells UI or visual state.
    private void HideAllCells()
    {
        _presentedCells.Clear();
        for (int i = 0; i < _cellPool.Count; i++)
        {
            if (_cellPool[i] != null)
                _cellPool[i].SetMissing();
        }
    }

    // Purpose: Calculates bounds from the current inputs.
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

    // Purpose: Returns the current cell alpha.
    private float GetCellAlpha(SectorMapCellSnapshot snapshot)
    {
        return snapshot.hasPublicRaidMarker || (snapshot.isRevealed && !snapshot.isLocked)
            ? Settings.ActiveCellAlpha
            : Settings.InactiveCellAlpha;
    }

    // Purpose: Performs the should show undiscovered icon workflow for this component.
    private bool ShouldShowUndiscoveredIcon(SectorMapCellSnapshot snapshot)
    {
        return !snapshot.isStartSector && !snapshot.isRevealed;
    }

    // Purpose: Performs the format room label workflow for this component.
    private string FormatRoomLabel(SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isStartSector)
            return Localize("Map_Start", Settings.StartSectorLabelText);

        return FormatCoordinateLabel(snapshot.coord);
    }

    // Purpose: Performs the format cell object name workflow for this component.
    private string FormatCellObjectName(Vector2Int coord)
    {
        string label = FormatCoordinateLabel(coord);

        if (!string.IsNullOrWhiteSpace(label))
            return label;

        return $"{coord.x}_{coord.y}";
    }

    // Purpose: Performs the format coordinate label workflow for this component.
    private string FormatCoordinateLabel(Vector2Int coord)
    {
        Vector2Int origin = Settings.CoordinateLabelOriginCoord;
        int xIndex = coord.x - origin.x;
        int yIndex = coord.y - origin.y;

        if (xIndex < 0 || yIndex < 0)
            return string.Empty;

        return $"{FormatAlphabetIndex(xIndex)}{yIndex + 1}";
    }

    // Purpose: Performs the format alphabet index workflow for this component.
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

    // Purpose: Applies the requested opacity label text change.
    private void ApplyOpacityLabelText()
    {
        if (_opacityLabelText == null || _settings == null)
            return;

        _opacityLabelText.text = Localize("Map_Opacity", _settings.OpacityLabelText);
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        _presentedCells.Clear();
        RequestLocalizationRefresh();
    }

    // Purpose: Refreshes code-generated map labels after the selected table is available.
    private void RequestLocalizationRefresh()
    {
        if (_localizationRefreshRoutine != null)
            StopCoroutine(_localizationRefreshRoutine);

        _localizationRefreshRoutine = StartCoroutine(RefreshLocalizedTextWhenReady());
    }

    private IEnumerator RefreshLocalizedTextWhenReady()
    {
        yield return UIRuntimeLocalization.EnsureTableLoaded(StringTable, "Map_Opacity");

        if (isActiveAndEnabled)
        {
            ApplyOpacityLabelText();

            if (_hasSnapshot)
                RefreshMap();
        }

        _localizationRefreshRoutine = null;
    }

    private static string Localize(string key, string fallback)
    {
        return UIRuntimeLocalization.Get(StringTable, key, fallback);
    }

    // Purpose: Updates the mini map visible state.
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
