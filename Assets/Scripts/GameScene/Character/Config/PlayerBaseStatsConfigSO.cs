using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerBaseStatsConfig",
    menuName = "VSplatter/Player/Base Stats Config")]
public class PlayerBaseStatsConfigSO : ScriptableObject
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Dash")]
    [Min(0f)]
    [SerializeField] private float dashCooldownSeconds = 3f;

   [Header("Survival")]
    [Min(1f)]
    [SerializeField] private float maxHealth = 100f;



    [Header("Occupation")]
    [SerializeField, Range(0f, 1f)] private float occupationWinThreshold = 0.5f;

    [Header("Shockwave")]
    [Min(0f)]
    [SerializeField] private float shockwaveCooldownSeconds = 0f;

    public float MoveSpeed => moveSpeed;
    public float DashCooldownSeconds => dashCooldownSeconds;
    public float MaxHealth => maxHealth;
    public float OccupationWinThreshold => occupationWinThreshold;
    public float ShockwaveCooldownSeconds => shockwaveCooldownSeconds;
}
