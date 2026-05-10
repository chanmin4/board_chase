using System;
using System.Collections.Generic;
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

    [Header("Events")]
    [SerializeField] private NamedSectorTimerSnapshotEventChannelSO _namedTimerSnapshotChannel;
    [SerializeField] private SectorMapSnapshotEventChannelSO _mapSnapshotChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestMapSnapshotChannel;
    [Header("Settings")]
    [SerializeField] private MiniMapUISettingsSO _settings;
    [Header("Cells")]
    [SerializeField] private CellSlot[] _slots;

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
    private void OnEnable()
    {
        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised += OnMapSnapshotChanged;

        if (_requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.RaiseEvent();

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.AddListener(OnAlphaSliderChanged);
        if (_namedTimerSnapshotChannel != null)
        _namedTimerSnapshotChannel.OnEventRaised += OnNamedTimerSnapshotChanged;
        ApplyInitialAlpha();
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
        _currentMapCenter = snapshot.currentSectorCoord;
        _hasCurrentMapCenter = true;

        CacheSnapshotCells(snapshot);
        RefreshCells(snapshot.currentSectorCoord);
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
        bool showRatio = snapshot.isOpened && !HasNamedOrBoss(snapshot);

        string ratioText = showRatio
            ? $"{Mathf.RoundToInt(snapshot.playerRatio * 100f)}%"
            : string.Empty;

        bool showJudgeTime = ShouldShowJudgeTime(snapshot);
        string judgeTimeText = showJudgeTime
            ? FormatJudgeTime(snapshot)
            : string.Empty;

        float visibleAlpha = GetCellAlpha(snapshot);

        cellUI.SetOpened(
            backgroundColor,
            iconSprite,
            ratioText,
            showIcon,
            showRatio,
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

        if (_hasCurrentMapCenter)
            RefreshCells(_currentMapCenter);
    }
}
