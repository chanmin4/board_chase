using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SectorRules", menuName = "Game/Sector/Sector Rules")]
public class SectorRulesSO : ScriptableObject
{
    [Header("Occupancy")]
    [Tooltip("SectorOccupancy가 백신/바이러스 점유 비율을 샘플링하는 간격입니다.")]
    [FormerlySerializedAs("sampleInterval")]
    [SerializeField, Min(0.01f)] private float _occupancySampleInterval = 0.25f;

    [Tooltip("SectorOccupancy가 현재 소유/우세 상태를 이벤트로 발행하는 간격입니다.")]
    [FormerlySerializedAs("judgeInterval")]
    [SerializeField, Min(0.01f)] private float _occupancyPublishInterval = 0.25f;

    [Tooltip("해당 비율을 초과해야 소유 판정됩니다. 0.5면 50:50은 Neutral, 51:49는 Player/Virus입니다.")]
    [FormerlySerializedAs("captureThreshold")]
    [SerializeField, Range(0.01f, 0.99f)] private float _dominanceThreshold = 0.5f;

    [Header("Normal Battle Judge")]
    [SerializeField] private bool _useTimedNormalBattleJudge = true;
    [SerializeField, Min(0f)] private float _successCountdownSeconds = 5f;
    [SerializeField, Min(0f)] private float _failureCountdownSeconds = 5f;
    [SerializeField] private SectorOwner _neutralJudgeResult = SectorOwner.Neutral;

    [Header("Normal Battle Completion")]
    [Tooltip("If true, NormalBattle rooms are cleared when their finite enemy encounter is fully defeated.")]
    [SerializeField] private bool _completeNormalBattleOnEnemyClear = true;

    [Header("Normal Battle Resolve WIP")]
    [Tooltip("WIP. If false, NormalBattle clear/fail does not force 100% vaccine/virus coating.")]
    [SerializeField] private bool _applyNormalBattleResolveCoatingEffects = false;
    [SerializeField] private bool _fillCompletedSectorWithVaccine = true;
    [SerializeField] private bool _fillFailedSectorWithVirus = true;
    [SerializeField] private bool _clearPaintMasksOnBattleResolve = true;

    public float OccupancySampleInterval => _occupancySampleInterval;
    public float OccupancyPublishInterval => _occupancyPublishInterval;
    public float DominanceThreshold => _dominanceThreshold;

    public bool UseTimedNormalBattleJudge => _useTimedNormalBattleJudge;
    public float SuccessCountdownSeconds => _successCountdownSeconds;
    public float FailureCountdownSeconds => _failureCountdownSeconds;
    public SectorOwner NeutralJudgeResult => _neutralJudgeResult;

    public bool CompleteNormalBattleOnEnemyClear => _completeNormalBattleOnEnemyClear;
    public bool ApplyNormalBattleResolveCoatingEffects => _applyNormalBattleResolveCoatingEffects;
    public bool FillCompletedSectorWithVaccine => _fillCompletedSectorWithVaccine;
    public bool FillFailedSectorWithVirus => _fillFailedSectorWithVirus;
    public bool ClearPaintMasksOnBattleResolve => _clearPaintMasksOnBattleResolve;
}
