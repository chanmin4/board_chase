using UnityEngine;

[CreateAssetMenu(
    fileName = "VSplatterDashConfig",
    menuName = "Game/Player/VSplatter Dash Config")]
public class VSplatterDashConfigSO : ScriptableObject
{
    [Header("Dash")]
    [Min(0f)]
    [SerializeField] private float _dashSpeed = 14f;
    [Min(0.01f)]
    [SerializeField] private float _dashDuration = 0.18f;
    [SerializeField] private bool _useForwardWhenNoMovementInput = true;
    [SerializeField] private bool _rotateTowardDashDirection = true;

    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public bool UseForwardWhenNoMovementInput => _useForwardWhenNoMovementInput;
    public bool RotateTowardDashDirection => _rotateTowardDashDirection;
}
