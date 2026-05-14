using UnityEngine;

public enum NamedPatternResult
{
    None,
    PlayerSucceeded,
    PlayerFailed
}

public class NamedPatternController : MonoBehaviour
{
    [Header("Schedule")]
    [SerializeField] private NamedPatternConfigSO _config;
    public NamedPatternConfigSO Config => _config;
    [Header("Runtime")]
    public bool patternReady;
    public bool prepareFinished;
    public bool activeFinished;
    public bool resolved;
    public bool sequenceFinished;
    public NamedPatternResult result = NamedPatternResult.None;

    [Header("Runtime Debug")]
    [SerializeField, ReadOnly] private bool _scheduleRunning;
    [SerializeField, ReadOnly] private float _scheduleTimer;
    [SerializeField, ReadOnly] private bool _patternActive;

    public bool ScheduleRunning => _scheduleRunning;
    public float ScheduleTimer => _scheduleTimer;
    public bool PatternActive => _patternActive;

    private void Update()
    {
        TickSchedule();
    }

    public void StartFirstPatternSchedule()
    {
        float delay = _config != null ? _config.FirstPatternDelay : 30f;
        StartSchedule(delay);
    }

    public void StartRepeatPatternSchedule()
    {
        if (_config == null)
            return;

        if (!_config.RepeatPattern)
            return;

        StartSchedule(_config.RepeatPatternDelay);
    }
    public void StopSchedule()
    {
        _scheduleRunning = false;
        _scheduleTimer = 0f;
    }

    public void BeginPattern()
    {
        // Pattern duration starts elsewhere when PatternActive state begins.
        // This only resets sequence flags and consumes patternReady.
        patternReady = false;
        prepareFinished = false;
        activeFinished = false;
        resolved = false;
        sequenceFinished = false;
        result = NamedPatternResult.None;

        _patternActive = true;
        StopSchedule();
    }

    public void MarkPrepareFinished()
    {
        prepareFinished = true;
    }

    public void MarkActiveFinished(NamedPatternResult patternResult)
    {
        result = patternResult;
        activeFinished = true;
         Debug.Log(
        $"[NamedPatternController] Active finished. result={result}, " +
        $"activeFinished={activeFinished}, sequenceFinished={sequenceFinished}",
        this);
    }

    public void MarkResolved()
    {
        resolved = true;
        sequenceFinished = true;
        _patternActive = false;

        Debug.Log(
            $"[NamedPatternController] Resolved. result={result}, " +
            $"resolved={resolved}, sequenceFinished={sequenceFinished}",
            this);

        // 여기서 StartRepeatPatternSchedule() 호출 금지.
        // 상위 AliveSM이 sequenceFinished를 읽고 Combat으로 빠진 뒤,
        // Pattern_Mutarus OnStateExit 액션에서 repeat schedule을 시작해야 한다.
    }

    public void SetPatternReady(bool ready)
    {
        patternReady = ready;

        if (ready)
            StopSchedule();
    }

    private void StartSchedule(float delay)
    {
        patternReady = false;
        prepareFinished = false;
        activeFinished = false;
        resolved = false;
        sequenceFinished = false;
        result = NamedPatternResult.None;

        _patternActive = false;
        _scheduleTimer = Mathf.Max(0f, delay);
        _scheduleRunning = true;

        if (_scheduleTimer <= 0f)
            SetPatternReady(true);
    }

    private void TickSchedule()
    {
        if (!_scheduleRunning)
            return;

        if (_patternActive || patternReady)
            return;

        _scheduleTimer -= Time.deltaTime;

        if (_scheduleTimer > 0f)
            return;

        SetPatternReady(true);
    }
    public float PatternActiveDuration
    {
        get
        {
            if (_config != null)
                return _config.PatternActiveDuration;

            return 20f;
        }
    }
}
