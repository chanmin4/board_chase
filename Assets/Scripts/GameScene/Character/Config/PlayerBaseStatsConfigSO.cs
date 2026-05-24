using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerBaseStatsConfig",
    menuName = "VSplatter/Player/Base Stats Config")]
public class PlayerBaseStatsConfigSO : ScriptableObject
{
    [Header("Weapon Base")]
    [Min(0f)] [SerializeField] private float attackDamage = 10f;
    [Min(0.1f)] [SerializeField] private float maxRange = 12f;
    [Min(0.01f)] [SerializeField] private float attackShotsPerSecond = 2f;
    [Min(0.01f)] [SerializeField] private float paintShotsPerSecond = 1f;
    [Min(0.01f)] [SerializeField] private float reloadDurationSeconds = 1.2f;
    [Min(1)] [SerializeField] private int magazineSize = 6;

    [Header("Paint Base")]
    [Min(0.01f)] [SerializeField] private float paintRadius = 1.25f;
    [SerializeField] private int paintPriority = 0;
    [SerializeField, Range(0f, 1f)] private float occupationWinThreshold = 0.5f;

    [Header("Movement")]
    [Min(0f)] [SerializeField] private float moveSpeed = 8f;

    [Header("Dash")]
    [Min(0f)] [SerializeField] private float dashCooldownSeconds = 3f;

    [Header("Survival")]
    [Min(1f)] [SerializeField] private float maxHealth = 100f;

    [Header("Shockwave")]
    [Min(0f)] [SerializeField] private float shockwaveCooldownSeconds = 0f;

    public float AttackDamage => attackDamage;
    public float MaxRange => maxRange;
    public float AttackShotsPerSecond => attackShotsPerSecond;
    public float PaintShotsPerSecond => paintShotsPerSecond;
    public float ReloadDurationSeconds => reloadDurationSeconds;
    public int MagazineSize => magazineSize;

    public float PaintRadius => paintRadius;
    public int PaintPriority => paintPriority;
    public float OccupationWinThreshold => occupationWinThreshold;

    public float MoveSpeed => moveSpeed;
    public float DashCooldownSeconds => dashCooldownSeconds;
    public float MaxHealth => maxHealth;
    public float ShockwaveCooldownSeconds => shockwaveCooldownSeconds;
}