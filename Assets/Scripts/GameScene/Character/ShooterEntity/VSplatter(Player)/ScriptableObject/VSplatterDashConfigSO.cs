using UnityEngine;

[CreateAssetMenu(
    fileName = "VSplatterDashConfig",
    menuName = "Game/Player/VSplatter Dash Config")]
public class VSplatterDashConfigSO : ScriptableObject
{
    [Header("Dash")]
    [Min(0f)]
    [SerializeField] private bool _useMoveSpeedBasedDash = true;

    [Tooltip("Dash speed = current player move speed * this multiplier.")]
    [Min(0f)]
    [SerializeField] private float _dashSpeedMultiplier = 2.5f;

    [Tooltip("Used only when move-speed based dash is disabled or PlayerStatsRuntime is missing.")]
    [Min(0f)]
    [SerializeField] private float _fixedDashSpeed = 20f;
    [Min(0.01f)]
    [SerializeField] private float _dashDuration = 0.4f;

    [Min(0f)]
    [SerializeField] private float _cooldownSeconds = 3f;

    [SerializeField] private bool _useForwardWhenNoMovementInput = true;
    [SerializeField] private bool _rotateTowardDashDirection = true;

    [Header("Visual Lean")]
    [SerializeField] private bool _useDashVisualLean = true;

    [Tooltip("대쉬 중 시각용 몸통 Pivot에 더할 Local Euler 회전값. 보통 X=15~20, 반대로 기울면 X를 음수로.")]
    [SerializeField] private Vector3 _dashVisualLeanEuler = new Vector3(18f, 0f, 0f);

    [Min(0f)]
    [SerializeField] private float _dashVisualLeanInSpeed = 18f;

    [Min(0f)]
    [SerializeField] private float _dashVisualLeanOutSpeed = 12f;

    public bool UseMoveSpeedBasedDash => _useMoveSpeedBasedDash;
    public float DashSpeedMultiplier => _dashSpeedMultiplier;
    public float FixedDashSpeed => _fixedDashSpeed;
    public float DashDuration => _dashDuration;
    public float CooldownSeconds => _cooldownSeconds;
    public bool UseForwardWhenNoMovementInput => _useForwardWhenNoMovementInput;
    public bool RotateTowardDashDirection => _rotateTowardDashDirection;

    public bool UseDashVisualLean => _useDashVisualLean;
    public Vector3 DashVisualLeanEuler => _dashVisualLeanEuler;
    public float DashVisualLeanInSpeed => _dashVisualLeanInSpeed;
    public float DashVisualLeanOutSpeed => _dashVisualLeanOutSpeed;
}
