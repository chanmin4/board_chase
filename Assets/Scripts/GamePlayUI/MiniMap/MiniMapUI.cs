using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapUI : MonoBehaviour
{
    [Serializable]
    private struct CellSlot
    {
        public Vector2Int offsetFromCurrent;
        public MiniMapCellUI cell;
    }

    [Serializable]
    private struct CoordinateLabelSlot
    {
        [Tooltip("현재 섹터 기준 오프셋입니다. X축 라벨이면 -1/0/1, Z축 라벨이면 -1/0/1로 넣습니다.")]
        public int offsetFromCurrent;

        [Tooltip("라벨 박스 전체 CanvasGroup입니다. 없는 좌표일 때 박스까지 숨기려면 넣어주세요.")]
        public CanvasGroup group;

        [Tooltip("A, B, C 또는 1, 2, 3이 표시될 텍스트입니다.")]
        public TMP_Text labelText;
    }

    [Header("Events")]
    [SerializeField] private NamedSectorTimerSnapshotEventChannelSO _namedTimerSnapshotChannel;
    [SerializeField] private SectorMapSnapshotEventChannelSO _mapSnapshotChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestMapSnapshotChannel;

    [Header("Settings")]
    [SerializeField] private MiniMapUISettingsSO _settings;

    [Header("Cells")]
    [SerializeField] private CellSlot[] _slots;

    [Header("Coordinate Labels")]
    [Tooltip("이 좌표가 A1이 됩니다. x00 z00을 A1로 쓸 거면 (0, 0).")]
    [SerializeField] private Vector2Int _coordinateLabelOriginCoord = Vector2Int.zero;

    [Tooltip("X 좌표가 몇 칸 단위로 알파벳 1칸씩 증가하는지입니다. 보통 1.")]
    [SerializeField, Min(1)] private int _xLabelCoordStep = 1;

    [Tooltip("Z 좌표가 몇 칸 단위로 숫자 1칸씩 증가하는지입니다. z00, z01, z02면 1. z00, z02, z04면 2.")]
    [SerializeField, Min(1)] private int _zLabelCoordStep = 1;

    [Tooltip("아래쪽 X축 라벨입니다. 왼쪽=-1, 가운데=0, 오른쪽=1 순서로 넣으면 됩니다.")]
    [SerializeField] private CoordinateLabelSlot[] _xAxisLabels;

    [Tooltip("왼쪽 Z축 라벨입니다. 아래=-1, 가운데=0, 위=1 순서로 넣으면 됩니다.")]
    [SerializeField] private CoordinateLabelSlot[] _zAxisLabels;

    [Tooltip("현재 미니맵 3x3 안에 실제 섹터가 없는 좌표 라벨은 숨깁니다.")]
    [SerializeField] private bool _hideCoordinateLabelWhenNoVisibleCell = true;

    [Header("Colors")]
    [SerializeField] private Color _playerColor = new Color(0.2f, 0.75f, 1f, 0.85f);
    [SerializeField] private Color _neutralColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
    [SerializeField] private Color _virusColor = new Color(0.9f, 0.15f, 0.25f, 0.85f);
    [SerializeField] private Color _lockedColor = new Color(0.15f, 0.15f, 0.15f, 0.75f);

    [Header("Icons")]
    [SerializeField] private Sprite _lockedIcon;
    [SerializeField] private Sprite _namedIcon;
    [SerializeField] private Sprite _bossIcon;

    [Header("Opacity")]
    [SerializeField] private UICanvasGroupOpacity _uicanvasGroupOpacity;
    [SerializeField] private Slider _alphaSlider;

    private readonly Dictionary<Vector2Int, SectorMapCellSnapshot> _cellByCoord = new();

    private NamedSectorTimerSnapshot _latestNamedTimerSnapshot;
    private bool _hasNamedTimerSnapshot;
    private Vector2Int _currentMapCenter;
    private bool _hasCurrentMapCenter;
    private Vector2Int _latestSnapshotCurrentCoord;
    private bool _hasLatestSnapshotCurrentCoord;
    private void OnEnable()
    {
        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised += OnMapSnapshotChanged;

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.AddListener(OnAlphaSliderChanged);

        if (_namedTimerSnapshotChannel != null)
            _namedTimerSnapshotChannel.OnEventRaised += OnNamedTimerSnapshotChanged;

        ClearCoordinateLabels();
        ApplyInitialAlpha();

        if (_requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.RaiseEvent();
    }

    private void OnDisable()
    {
        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised -= OnMapSnapshotChanged;

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.RemoveListener(OnAlphaSliderChanged);

        if (_namedTimerSnapshotChannel != null)
            _namedTimerSnapshotChannel.OnEventRaised -= OnNamedTimerSnapshotChanged;
    }

    private void ApplyInitialAlpha()
    {
        if (_uicanvasGroupOpacity == null)
            return;

        float alpha = _uicanvasGroupOpacity.Settings != null
            ? _uicanvasGroupOpacity.Settings.DefaultAlpha
            : 0.6f;

        if (_alphaSlider != null)
            _alphaSlider.SetValueWithoutNotify(alpha);

        _uicanvasGroupOpacity.ApplyImmediate(alpha);
    }

    private void OnAlphaSliderChanged(float alpha)
    {
        _uicanvasGroupOpacity?.SetAlpha(alpha);
    }

    private void OnMapSnapshotChanged(SectorMapSnapshot snapshot)
    {
        _latestSnapshotCurrentCoord = snapshot.currentSectorCoord;
        _hasLatestSnapshotCurrentCoord = true;

        CacheSnapshotCells(snapshot);

        Vector2Int displayCenter = ResolveDisplayCenterCoord(snapshot.currentSectorCoord);

        _currentMapCenter = displayCenter;
        _hasCurrentMapCenter = true;

        RefreshCoordinateLabels(displayCenter);
        RefreshCells(displayCenter);
    }

    private void CacheSnapshotCells(SectorMapSnapshot snapshot)
    {
        _cellByCoord.Clear();

        if (snapshot.cells == null)
            return;

        for (int i = 0; i < snapshot.cells.Length; i++)
            _cellByCoord[snapshot.cells[i].coord] = snapshot.cells[i];
    }

    private void RefreshCells(Vector2Int currentCoord)
    {
        if (_slots == null)
            return;

        for (int i = 0; i < _slots.Length; i++)
        {
            CellSlot slot = _slots[i];

            if (slot.cell == null)
                continue;

            Vector2Int targetCoord = currentCoord + slot.offsetFromCurrent;

            if (!_cellByCoord.TryGetValue(targetCoord, out SectorMapCellSnapshot cellSnapshot))
            {
                slot.cell.SetMissing();
                continue;
            }

            ApplyCell(slot.cell, cellSnapshot);
        }
    }

    private void ApplyCell(MiniMapCellUI cellUI, SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isLocked)
        {
            cellUI.SetLocked(GetLockedColor(), GetLockedIcon());
            return;
        }

        Color backgroundColor = GetBackgroundColor(snapshot);
        Sprite iconSprite = GetIcon(snapshot);

        bool showIcon = iconSprite != null;
        bool showRatios = snapshot.isOpened && !HasNamedOrBoss(snapshot);

        string playerRatioText = showRatios
            ? $"{Mathf.RoundToInt(snapshot.playerRatio * 100f)}"
            : string.Empty;

        string virusRatioText = showRatios
            ? $"{Mathf.RoundToInt(snapshot.virusRatio * 100f)}"
            : string.Empty;

        bool showJudgeTime = ShouldShowJudgeTime(snapshot);
        string judgeTimeText = showJudgeTime
            ? FormatJudgeTime(snapshot)
            : string.Empty;

        float visibleAlpha = GetCellAlpha(snapshot);

        cellUI.SetOpened(
            backgroundColor,
            iconSprite,
            playerRatioText,
            virusRatioText,
            showIcon,
            showRatios,
            judgeTimeText,
            showJudgeTime,
            visibleAlpha);

        ApplyNamedTimer(cellUI, snapshot);
    }

    private void ApplyNamedTimer(MiniMapCellUI cellUI, SectorMapCellSnapshot snapshot)
    {
        if (!_hasNamedTimerSnapshot || _latestNamedTimerSnapshot.sector == null)
        {
            cellUI.SetSpecialTimer(string.Empty, false);
            return;
        }

        if (snapshot.coord != _latestNamedTimerSnapshot.sector.coord)
        {
            cellUI.SetSpecialTimer(string.Empty, false);
            return;
        }

        bool show =
            _latestNamedTimerSnapshot.phase == NamedSectorPhase.WaitingForReservation ||
            _latestNamedTimerSnapshot.phase == NamedSectorPhase.Reserved ||
            _latestNamedTimerSnapshot.phase == NamedSectorPhase.DefeatedCooldown;

        if (!show)
        {
            cellUI.SetSpecialTimer(string.Empty, false);
            return;
        }

        string text = $"{Mathf.CeilToInt(_latestNamedTimerSnapshot.remainingSeconds)}s";
        cellUI.SetSpecialTimer(text, true);
    }

    private void RefreshCoordinateLabels(Vector2Int currentCoord)
    {
        RefreshAxisLabels(_xAxisLabels, currentCoord, isXAxis: true);
        RefreshAxisLabels(_zAxisLabels, currentCoord, isXAxis: false);
    }

    private void RefreshAxisLabels(CoordinateLabelSlot[] labels, Vector2Int currentCoord, bool isXAxis)
    {
        if (labels == null)
            return;

        for (int i = 0; i < labels.Length; i++)
        {
            CoordinateLabelSlot label = labels[i];

            int coordValue = isXAxis
                ? currentCoord.x + label.offsetFromCurrent
                : currentCoord.y + label.offsetFromCurrent;

            bool hasVisibleCell = isXAxis
                ? HasAnyVisibleCellAtX(coordValue, currentCoord)
                : HasAnyVisibleCellAtZ(coordValue, currentCoord);

            bool visible = !_hideCoordinateLabelWhenNoVisibleCell || hasVisibleCell;

            string text = visible
                ? isXAxis ? FormatXLabel(coordValue) : FormatZLabel(coordValue)
                : string.Empty;

            visible = visible && !string.IsNullOrWhiteSpace(text);
            SetCoordinateLabel(label, text, visible);
        }
    }

    private bool HasAnyVisibleCellAtX(int coordX, Vector2Int currentCoord)
    {
        if (_slots == null)
            return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            Vector2Int targetCoord = currentCoord + _slots[i].offsetFromCurrent;

            if (targetCoord.x == coordX && _cellByCoord.ContainsKey(targetCoord))
                return true;
        }

        return false;
    }

    private bool HasAnyVisibleCellAtZ(int coordZ, Vector2Int currentCoord)
    {
        if (_slots == null)
            return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            Vector2Int targetCoord = currentCoord + _slots[i].offsetFromCurrent;

            if (targetCoord.y == coordZ && _cellByCoord.ContainsKey(targetCoord))
                return true;
        }

        return false;
    }

    private string FormatXLabel(int coordX)
    {
        int diff = coordX - _coordinateLabelOriginCoord.x;

        if (diff < 0 || diff % Mathf.Max(1, _xLabelCoordStep) != 0)
            return string.Empty;

        int index = diff / Mathf.Max(1, _xLabelCoordStep);
        return FormatAlphabetIndex(index);
    }

    private string FormatZLabel(int coordZ)
    {
        int diff = coordZ - _coordinateLabelOriginCoord.y;

        if (diff < 0 || diff % Mathf.Max(1, _zLabelCoordStep) != 0)
            return string.Empty;

        int number = diff / Mathf.Max(1, _zLabelCoordStep) + 1;
        return number.ToString();
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

    private static void SetCoordinateLabel(CoordinateLabelSlot label, string text, bool visible)
    {
        if (label.labelText != null)
            label.labelText.text = visible ? text : string.Empty;

        if (label.group == null)
            return;

        label.group.alpha = visible ? 1f : 0f;
        label.group.interactable = false;
        label.group.blocksRaycasts = false;
    }

    private void ClearCoordinateLabels()
    {
        ClearAxisLabels(_xAxisLabels);
        ClearAxisLabels(_zAxisLabels);
    }

    private static void ClearAxisLabels(CoordinateLabelSlot[] labels)
    {
        if (labels == null)
            return;

        for (int i = 0; i < labels.Length; i++)
            SetCoordinateLabel(labels[i], string.Empty, false);
    }

    private Color GetBackgroundColor(SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isLocked)
            return GetLockedColor();

        switch (snapshot.owner)
        {
            case SectorOwner.Player:
                return GetPlayerColor();

            case SectorOwner.Virus:
                return GetVirusColor();

            default:
                return GetNeutralColor();
        }
    }

    private float GetCellAlpha(SectorMapCellSnapshot snapshot)
    {
        if ((snapshot.specialState & SectorSpecialState.NamedReserved) != 0)
            return _settings != null ? _settings.NamedReservedCellAlpha : 0.45f;

        return 1f;
    }

    private Color GetPlayerColor()
    {
        return _settings != null ? _settings.PlayerColor : _playerColor;
    }

    private Color GetNeutralColor()
    {
        return _settings != null ? _settings.NeutralColor : _neutralColor;
    }

    private Color GetVirusColor()
    {
        return _settings != null ? _settings.VirusColor : _virusColor;
    }

    private Color GetLockedColor()
    {
        return _settings != null ? _settings.LockedColor : _lockedColor;
    }

    private Sprite GetLockedIcon()
    {
        return _settings != null ? _settings.LockedIcon : _lockedIcon;
    }

    private Sprite GetNamedIcon()
    {
        return _settings != null ? _settings.NamedIcon : _namedIcon;
    }

    private Sprite GetBossIcon()
    {
        return _settings != null ? _settings.BossIcon : _bossIcon;
    }

    private Sprite GetIcon(SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isLocked)
            return GetLockedIcon();

        if ((snapshot.specialState & SectorSpecialState.BossActive) != 0)
            return GetBossIcon();

        if ((snapshot.specialState & SectorSpecialState.NamedActive) != 0)
            return GetNamedIcon();

        return null;
    }

    private bool HasNamedOrBoss(SectorMapCellSnapshot snapshot)
    {
        return
            (snapshot.specialState & SectorSpecialState.NamedActive) != 0 ||
            (snapshot.specialState & SectorSpecialState.BossActive) != 0;
    }

    private bool ShouldShowJudgeTime(SectorMapCellSnapshot snapshot)
    {
        return snapshot.isOpened &&
            snapshot.contestState != SectorContestState.None &&
            snapshot.contestRequired > 0f;
    }

    private string FormatJudgeTime(SectorMapCellSnapshot snapshot)
    {
        float remaining = Mathf.Max(0f, snapshot.contestRequired - snapshot.contestElapsed);
        return $"{remaining:0.0}s";
    }

    private void OnNamedTimerSnapshotChanged(NamedSectorTimerSnapshot snapshot)
    {
        _latestNamedTimerSnapshot = snapshot;
        _hasNamedTimerSnapshot = true;

        if (!_hasLatestSnapshotCurrentCoord)
            return;

        Vector2Int displayCenter = ResolveDisplayCenterCoord(_latestSnapshotCurrentCoord);

        _currentMapCenter = displayCenter;
        _hasCurrentMapCenter = true;

        RefreshCoordinateLabels(displayCenter);
        RefreshCells(displayCenter);
    }
    private Vector2Int ResolveDisplayCenterCoord(Vector2Int snapshotCurrentCoord)
    {
        if (TryGetNamedBattleSourceCoord(out Vector2Int namedSourceCoord))
            return namedSourceCoord;

        return snapshotCurrentCoord;
    }

    private bool TryGetNamedBattleSourceCoord(out Vector2Int coord)
    {
        coord = default;

        if (!_hasNamedTimerSnapshot || _latestNamedTimerSnapshot.sector == null)
            return false;

        if (!ShouldPinMiniMapToNamedSource(_latestNamedTimerSnapshot.phase))
            return false;

        coord = _latestNamedTimerSnapshot.sector.coord;
        return true;
    }

    private static bool ShouldPinMiniMapToNamedSource(NamedSectorPhase phase)
    {
        switch (phase)
        {
            case NamedSectorPhase.EnteringBattle:
            case NamedSectorPhase.Battle:
            case NamedSectorPhase.RewardPending:
            case NamedSectorPhase.EndingBattle:
                return true;

            default:
                return false;
        }
    }
}