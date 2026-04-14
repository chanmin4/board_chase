using System;
using UnityEngine;

public enum SectorOwner { Neutral, Player, Virus }
public enum SectorContestState { None, PlayerCapturing, VirusCapturing }

[Flags]
public enum SectorSpecialState
{
    None = 0,
    NamedActive = 1 << 0,
    BossActive = 1 << 1,
    MonsterLocked = 1 << 2
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
        float playerAbs = ComputeRatio(_paint.vaccineBuffer, _paint.textureWidth, _paint.textureHeight);
        float virusAbs = ComputeRatio(_paint.virusBuffer, _paint.textureWidth, _paint.textureHeight);
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

    private float ComputeRatio(Color32[] buffer, int width, int height)
    {
        if (buffer == null || width <= 0 || height <= 0)
            return 0f;

        int stride = Mathf.Max(1, _rules != null ? _rules.sampleStride : 1);
        long sum = 0;
        long samples = 0;

        for (int y = 0; y < height; y += stride)
        {
            int row = y * width;
            for (int x = 0; x < width; x += stride)
            {
                sum += buffer[row + x].a;
                samples++;
            }
        }

        return samples > 0 ? Mathf.Clamp01(sum / (255f * samples)) : 0f;
    }
}
