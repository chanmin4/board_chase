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
    [SerializeField] private SectorOccupancyRulesSO _rules;
    [SerializeField] private SectorOccupancyEventChannelSO _changedChannel;
    [SerializeField] private SectorOwner _owner = SectorOwner.Neutral;
    [SerializeField] private SectorSpecialState _specialState = SectorSpecialState.None;

    private float _sampleTimer;
    private float _judgeTimer;
    private float _playerRatio;
    private float _virusRatio;
    private SectorOwner _candidateOwner = SectorOwner.Neutral;
    private float _contestElapsed;

    public SectorOccupancySnapshot CurrentSnapshot => BuildSnapshot();
    public SectorSpecialState SpecialState => _specialState;
    private void Awake()
    {
        if (!_paint) _paint = GetComponent<SectorPaint>();
    }

    private void OnEnable()
    {
        Publish();
    }

    private void Update()
    {
        if (_rules == null || _paint == null)
            return;

        _sampleTimer += Time.deltaTime;
        _judgeTimer += Time.deltaTime;

        if (_sampleTimer >= _rules.sampleInterval)
        {
            _sampleTimer = 0f;
            SampleRatios();
        }

        if (_judgeTimer >= _rules.judgeInterval)
        {
            float judgeDelta = _judgeTimer;
            _judgeTimer = 0f;
            Judge(judgeDelta);
            Publish();
        }
    }

    private void SampleRatios()
    {
        float playerAbs = _paint.GetCoverageRatio(MaskRenderManager.PaintChannel.Vaccine);
        float virusAbs = _paint.GetCoverageRatio(MaskRenderManager.PaintChannel.Virus);
        float sum = playerAbs + virusAbs;

        _playerRatio = sum > 0.00001f ? playerAbs / sum : 0f;
        _virusRatio = sum > 0.00001f ? virusAbs / sum : 0f;
    }

    private void Judge(float deltaTime)
    {
        SectorOwner dominant = GetDominantOwner();

        if (dominant == SectorOwner.Neutral || dominant == _owner)
        {
            ResetContest();
            return;
        }

        if (_candidateOwner != dominant)
        {
            _candidateOwner = dominant;
            _contestElapsed = 0f;
        }

        _contestElapsed += deltaTime;

        if (_contestElapsed >= _rules.captureHoldSeconds)
        {
            _owner = dominant;
            ResetContest();
        }
    }

    private SectorOwner GetDominantOwner()
    {
        if (_playerRatio <= 0f && _virusRatio <= 0f)
            return SectorOwner.Neutral;

        return _playerRatio >= _rules.captureThreshold ? SectorOwner.Player : SectorOwner.Virus;
    }

    private void ResetContest()
    {
        _candidateOwner = SectorOwner.Neutral;
        _contestElapsed = 0f;
    }

    private SectorOccupancySnapshot BuildSnapshot()
    {
        return new SectorOccupancySnapshot
        {
            sector = _paint != null ? _paint.Runtime : null,
            owner = _owner,
            dominantOwner = GetDominantOwner(),
            contestState = _candidateOwner == SectorOwner.Player ? SectorContestState.PlayerCapturing :
                           _candidateOwner == SectorOwner.Virus ? SectorContestState.VirusCapturing :
                           SectorContestState.None,
            specialState = _specialState,
            playerRatio = _playerRatio,
            virusRatio = _virusRatio,
            contestElapsed = _contestElapsed,
            contestRequired = _rules != null ? _rules.captureHoldSeconds : 5f
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
        ResetContest();
        Publish();
    }
    public void ResetToNeutral()
    {
        _owner = SectorOwner.Neutral;
        _specialState = SectorSpecialState.None;
        _playerRatio = 0f;
        _virusRatio = 0f;
        ResetContest();
        Publish();
    }
}
