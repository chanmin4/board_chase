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
    [SerializeField] private SectorMapSnapshotEventChannelSO _mapSnapshotChangedChannel;
    [SerializeField] private VoidEventChannelSO _requestMapSnapshotChannel;

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

    private void OnEnable()
    {
        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised += OnMapSnapshotChanged;

        if (_requestMapSnapshotChannel != null)
            _requestMapSnapshotChannel.RaiseEvent();

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.AddListener(OnAlphaSliderChanged);

        ApplyInitialAlpha();
    }

    private void OnDisable()
    {
        if (_mapSnapshotChangedChannel != null)
            _mapSnapshotChangedChannel.OnEventRaised -= OnMapSnapshotChanged;

        if (_alphaSlider != null)
            _alphaSlider.onValueChanged.RemoveListener(OnAlphaSliderChanged);
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
            cellUI.SetLocked(_lockedColor, _lockedIcon);
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

        cellUI.SetOpened(
            backgroundColor,
            iconSprite,
            ratioText,
            showIcon,
            showRatio,
            judgeTimeText,
            showJudgeTime); 
     }

    private Color GetBackgroundColor(SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isLocked)
            return _lockedColor;

        switch (snapshot.owner)
        {
            case SectorOwner.Player:
                return _playerColor;

            case SectorOwner.Virus:
                return _virusColor;

            default:
                return _neutralColor;
        }
    }

    private Sprite GetIcon(SectorMapCellSnapshot snapshot)
    {
        if (snapshot.isLocked)
            return _lockedIcon;

        if ((snapshot.specialState & SectorSpecialState.BossActive) != 0)
            return _bossIcon;

        if ((snapshot.specialState & SectorSpecialState.NamedActive) != 0)
            return _namedIcon;

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
}
