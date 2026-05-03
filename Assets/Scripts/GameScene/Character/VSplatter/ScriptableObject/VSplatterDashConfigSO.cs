using UnityEngine;

[CreateAssetMenu(
    fileName = "VSplatterDashConfig",
    menuName = "Game/Player/VSplatter Dash Config")]
public class VSplatterDashConfigSO : ScriptableObject
{
    [Header("Dash")]
    [Min(0f)]
    [SerializeField] private float _dashSpeed = 20f;
    [Min(0.01f)]
    [SerializeField] private float _dashDuration = 0.4f;
    [Min(0f)]
    [SerializeField] private float _cooldownSeconds = 3f;
    [SerializeField] private bool _useForwardWhenNoMovementInput = true;
    [SerializeField] private bool _rotateTowardDashDirection = true;

    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public float CooldownSeconds => _cooldownSeconds;
    public bool UseForwardWhenNoMovementInput => _useForwardWhenNoMovementInput;
    public bool RotateTowardDashDirection => _rotateTowardDashDirection;
}
