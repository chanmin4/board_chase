/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy
 * Primary responsibility: Implements the focused NamedPatternResult responsibility and collaborates with its role group.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

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
    [Header("UI Broadcasting")]
    [SerializeField] private NamedPatternDurationEventChannelSO _durationEventChannel;
    [Header("Runtime")]
    public bool patternReady;
    public bool prepareFinished;
    public bool activeFinished;
    public bool resolved;
    public bool sequenceFinished;
    public NamedPatternResult result = NamedPatternResult.None;
    [Header("Runtime UI")]
    [SerializeField, ReadOnly] private float _activeDuration;
    [SerializeField, ReadOnly] private float _activeRemaining;
    [SerializeField, ReadOnly] private int _objectiveRequiredCount;
    [SerializeField, ReadOnly] private int _objectiveCompletedCount;
    [Header("Runtime Debug")]
    [SerializeField, ReadOnly] private bool _scheduleRunning;
    [SerializeField, ReadOnly] private float _scheduleTimer;
    [SerializeField, ReadOnly] private bool _patternActive;
    
    public bool ScheduleRunning => _scheduleRunning;
    public float ScheduleTimer => _scheduleTimer;
    public bool PatternActive => _patternActive;
    public bool ShowObjectiveCounter =>
    _config != null && _config.ShowObjectiveCounter && _objectiveRequiredCount > 0;

    public int ObjectiveRequiredCount => _objectiveRequiredCount;
    public int ObjectiveCompletedCount => _objectiveCompletedCount;
    public int ObjectiveRemainingCount => Mathf.Max(0, _objectiveRequiredCount - _objectiveCompletedCount);
    // Purpose: Processes this component's per-frame runtime work.
    private void Update()
    {
        TickSchedule();
        TickPatternDurationUI();
    }

    // Purpose: Begins the first pattern schedule workflow.
    public void StartFirstPatternSchedule()
    {
        float delay = _config != null ? _config.FirstPatternDelay : 30f;
        StartSchedule(DifficultyRuntime.ApplyNamedPatternFirstDelay(delay));
    }

    // Purpose: Begins the repeat pattern schedule workflow.
    public void StartRepeatPatternSchedule()
    {
        if (_config == null)
            return;

        if (!_config.RepeatPattern)
            return;

        StartSchedule(DifficultyRuntime.ApplyNamedPatternRepeatDelay(_config.RepeatPatternDelay));
    }
    // Purpose: Begins the pattern active timer workflow.
    public void StartPatternActiveTimer(float duration = -1f)
    {
        if (activeFinished || resolved || sequenceFinished)
            return;

        _activeDuration = duration > 0f ? duration : PatternActiveDuration;
        _activeRemaining = _activeDuration;
        _patternActive = true;

        PublishPatternDurationUI(true);
    }
    // Purpose: Stops the active schedule behavior.
    public void StopSchedule()
    {
        _scheduleRunning = false;
        _scheduleTimer = 0f;
    }

    // Purpose: Begins the pattern workflow.
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
        _objectiveRequiredCount = _config != null && _config.ShowObjectiveCounter
        ? _config.ObjectiveRequiredCount
        : 0;

        _objectiveCompletedCount = 0;
  
        StopSchedule();
        _patternActive = false;
        _activeDuration = PatternActiveDuration;
        _activeRemaining = _activeDuration;

        PublishPatternDurationUI(true);
    }

    // Purpose: Performs the mark prepare finished workflow for this component.
    public void MarkPrepareFinished()
    {
        prepareFinished = true;
    }

    // Purpose: Performs the mark active finished workflow for this component.
    public void MarkActiveFinished(NamedPatternResult patternResult)
    {
        result = patternResult;
        activeFinished = true;
        _patternActive = false;
        PublishPatternDurationUI(false);
         Debug.Log(
        $"[NamedPatternController] Active finished. result={result}, " +
        $"activeFinished={activeFinished}, sequenceFinished={sequenceFinished}",
        this);
    }

    // Purpose: Performs the mark resolved workflow for this component.
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

    // Purpose: Updates the pattern ready state.
    public void SetPatternReady(bool ready)
    {
        patternReady = ready;

        if (ready)
            StopSchedule();
    }

    // Purpose: Begins the schedule workflow.
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

    // Purpose: Processes one update step for schedule.
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
                return DifficultyRuntime.ApplyNamedPatternActiveDuration(_config.PatternActiveDuration);

            return 20f;
        }
    }
    // Purpose: Processes one update step for pattern duration ui.
    private void TickPatternDurationUI()
    {
        if (!_patternActive)
            return;

        _activeRemaining = Mathf.Max(0f, _activeRemaining - Time.deltaTime);
        PublishPatternDurationUI(true);
    }

    // Purpose: Publishes the pattern duration ui notification.
    private void PublishPatternDurationUI(bool visible)
    {
        if (_durationEventChannel == null)
            return;

        string patternName = _config != null ? _config.DisplayName : "Named Pattern";
        float duration = _activeDuration > 0f ? _activeDuration : PatternActiveDuration;

        _durationEventChannel.RaiseEvent(new NamedPatternDurationSnapshot(
            visible,
            patternName,
            visible ? _activeRemaining : 0f,
            duration,
            visible && ShowObjectiveCounter,
            ObjectiveRemainingCount,
            _objectiveRequiredCount,
            _objectiveCompletedCount));
    }
    // Purpose: Updates the objective progress state.
    public void SetObjectiveProgress(int completedCount, int requiredCount)
    {
        _objectiveRequiredCount = Mathf.Max(0, requiredCount);
        _objectiveCompletedCount = Mathf.Clamp(completedCount, 0, _objectiveRequiredCount);

        if (_patternActive)
            PublishPatternDurationUI(true);
    }
    // Purpose: Performs the force stop pattern workflow for this component.
    public void ForceStopPattern()
    {
        StopSchedule();

        patternReady = false;
        prepareFinished = false;
        activeFinished = false;
        resolved = false;
        sequenceFinished = false;
        result = NamedPatternResult.None;

        _patternActive = false;
        _activeDuration = 0f;
        _activeRemaining = 0f;

        _objectiveRequiredCount = 0;
        _objectiveCompletedCount = 0;

        PublishPatternDurationUI(false);
    }
}
