using UnityEngine;

[CreateAssetMenu(
    fileName = "Mutarus_ChargeAttack_Config",
    menuName = "ScriptableObject/GameScene/Enemy/Definition_Config/NamedEnemy/Mutarus/Charge Attack Config")]
public class MutarusChargeAttackConfigSO : NamedAttackConfigSO
{
    [Header("Charge")]
    [SerializeField, Min(1)] private int _chargeCount = 1;
    [SerializeField, Min(0.1f)] private float _chargeDistance = 8f;
    [SerializeField, Min(0.1f)] private float _chargeSpeed = 14f;
    [SerializeField, Min(0f)] private float _startDelay = 0.15f;
    [SerializeField, Min(0f)] private float _pauseBetweenCharges = 0.25f;
    [SerializeField] private bool _reAimBeforeEachCharge = true;

    [Header("Prediction")]
    [SerializeField, Range(0f, 2f)] private float _predictionFactor = 0.65f;
    [SerializeField, Min(0f)] private float _maxPredictionSeconds = 0.75f;
    [SerializeField, Range(0f, 45f)] private float _randomAngleDeg = 8f;

    public override EnemyAttackBehaviorType AttackBehaviorType => EnemyAttackBehaviorType.Charge;

    public int ChargeCount => Mathf.Max(1, _chargeCount);
    public float ChargeDistance => Mathf.Max(0.1f, _chargeDistance);
    public float ChargeSpeed => DifficultyRuntime.ApplyEnemyChaseMoveSpeed(_chargeSpeed);
    public float StartDelay => Mathf.Max(0f, _startDelay);
    public float PauseBetweenCharges => Mathf.Max(0f, _pauseBetweenCharges);
    public bool ReAimBeforeEachCharge => _reAimBeforeEachCharge;
    public float PredictionFactor => _predictionFactor;
    public float MaxPredictionSeconds => Mathf.Max(0f, _maxPredictionSeconds);
    public float RandomAngleDeg => Mathf.Max(0f, _randomAngleDeg);
}
