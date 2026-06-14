using System;
using UnityEngine;

public enum SectorOwner { Neutral, Player, Virus }
public enum SectorContestState { None, PlayerCapturing, VirusCapturing }

[Flags]
public enum SectorSpecialState
{
    None = 0,
    NamedReserved = 1 << 0,
    NamedActive = 1 << 1,
    BossActive = 1 << 2,
    MonsterLocked = 1 << 3
}

[Serializable]
public struct SectorOccupancySnapshot
{
    public SectorRuntime sector;
    public SectorOwner owner;
    public SectorOwner dominantOwner;
    public SectorContestState contestState;
    public SectorSpecialState specialState;
    public float playerRatio;
    public float virusRatio;
    public float contestElapsed;
    public float contestRequired;
}

[DisallowMultipleComponent]
public class SectorOccupancy : MonoBehaviour
{
    [SerializeField] private SectorPaint _paint;
    [SerializeField] private SectorRulesSO _rules;
    [SerializeField] private SectorOccupancyEventChannelSO _changedChannel;
    [SerializeField] private SectorOwner _owner = SectorOwner.Neutral;
    [SerializeField] private SectorSpecialState _specialState = SectorSpecialState.None;

    private float _sampleTimer;
    private float _publishTimer;
    private float _playerRatio;
    private float _virusRatio;

    public SectorOccupancySnapshot CurrentSnapshot => BuildSnapshot();
    public SectorSpecialState SpecialState => _specialState;

    private void Awake()
    {
        if (_paint == null)
            _paint = GetComponent<SectorPaint>();
    }

    private void OnEnable()
    {
        RefreshNow();
    }

    private void Update()
    {
        if (_rules == null || _paint == null)
            return;

        _sampleTimer += Time.deltaTime;
        _publishTimer += Time.deltaTime;

        if (_sampleTimer >= _rules.OccupancySampleInterval)
        {
            _sampleTimer = 0f;
            SampleRatios();
            ApplyImmediateOwner();
        }

        if (_publishTimer >= _rules.OccupancyPublishInterval)
        {
            _publishTimer = 0f;
            Publish();
        }
    }

    public void RefreshNow()
    {
        if (_paint != null)
        {
            SampleRatios();
            ApplyImmediateOwner();
        }

        Publish();
    }

    private void SampleRatios()
    {
        float playerAbs = _paint.GetCoverageRatio(PaintChannel.Vaccine);
        float virusAbs = _paint.GetCoverageRatio(PaintChannel.Virus);
        float sum = playerAbs + virusAbs;

        _playerRatio = sum > 0.00001f ? playerAbs / sum : 0f;
        _virusRatio = sum > 0.00001f ? virusAbs / sum : 0f;
    }

    private void ApplyImmediateOwner()
    {
        SectorOwner nextOwner = GetDominantOwner();

        if (_owner == nextOwner)
            return;

        _owner = nextOwner;
        Publish();
    }

    private SectorOwner GetDominantOwner()
    {
        if (_playerRatio <= 0f && _virusRatio <= 0f)
            return SectorOwner.Neutral;

        float threshold = _rules != null ? _rules.DominanceThreshold : 0.5f;

        if (_playerRatio > threshold)
            return SectorOwner.Player;

        if (_virusRatio > threshold)
            return SectorOwner.Virus;

        return SectorOwner.Neutral;
    }

    private SectorOccupancySnapshot BuildSnapshot()
    {
        SectorOwner dominantOwner = GetDominantOwner();

        return new SectorOccupancySnapshot
        {
            sector = _paint != null ? _paint.Runtime : null,
            owner = _owner,
            dominantOwner = dominantOwner,
            contestState = dominantOwner == SectorOwner.Player ? SectorContestState.PlayerCapturing :
                           dominantOwner == SectorOwner.Virus ? SectorContestState.VirusCapturing :
                           SectorContestState.None,
            specialState = _specialState,
            playerRatio = _playerRatio,
            virusRatio = _virusRatio,
            contestElapsed = 0f,
            contestRequired = 0f
        };
    }

    private void Publish()
    {
        if (_changedChannel != null)
            _changedChannel.RaiseEvent(BuildSnapshot());
    }

    public void SetSpecialState(SectorSpecialState specialState)
    {
        _specialState = specialState;
        Publish();
    }

    public void AddSpecialState(SectorSpecialState specialState)
    {
        _specialState |= specialState;
        Publish();
    }

    public void RemoveSpecialState(SectorSpecialState specialState)
    {
        _specialState &= ~specialState;
        Publish();
    }

    public void ForceOwnerAndRatios(SectorOwner owner, float playerRatio, float virusRatio)
    {
        _owner = owner;
        _playerRatio = Mathf.Clamp01(playerRatio);
        _virusRatio = Mathf.Clamp01(virusRatio);
        Publish();
    }

    public void ResetToNeutral()
    {
        _owner = SectorOwner.Neutral;
        _specialState = SectorSpecialState.None;
        _playerRatio = 0f;
        _virusRatio = 0f;
        Publish();
    }
}