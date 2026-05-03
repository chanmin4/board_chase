using UnityEngine;

[CreateAssetMenu(
    fileName = "VSplatterShockwaveConfig",
    menuName = "Game/Player/VSplatter Shockwave Config")]
public class VSplatterShockwaveConfigSO : ScriptableObject
{
    [Header("Physics")]
    [SerializeField] private LayerMask _hitMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Charge")]
    [Min(0.01f)]
    [SerializeField] private float _maxChargeTime = 3f;

    [Header("Cooldown")]
    [Min(0f)]
    [SerializeField] private float _cooldownSeconds = 5f;

    [Header("Radius")]
    [Min(0f)]
    [SerializeField] private float _baseRadius = 5f;
    [Min(0f)]
    [SerializeField] private float _maxRadius = 10f;

    [Header("Damage")]
    [SerializeField] private bool _applyDamage = false;
    [Min(0f)]
    [SerializeField] private float _baseDamage = 0f;
    [Min(0f)]
    [SerializeField] private float _maxDamage = 0f;

    [Header("Knockback")]
    [SerializeField] private bool _applyKnockback = true;
    [Min(0f)]
    [SerializeField] private float _baseKnockbackDistance = 5f;
    [Min(0f)]
    [SerializeField] private float _maxKnockbackDistance = 10f;
    [Min(0f)]
    [SerializeField] private float _knockbackDuration = 0.3f;

    public LayerMask HitMask => _hitMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public float MaxChargeTime => _maxChargeTime;
    public float CooldownSeconds => _cooldownSeconds;
    public float BaseRadius => _baseRadius;
    public float MaxRadius => _maxRadius;
    public bool ApplyDamage => _applyDamage;
    public float BaseDamage => _baseDamage;
    public float MaxDamage => _maxDamage;
    public bool ApplyKnockback => _applyKnockback;
    public float BaseKnockbackDistance => _baseKnockbackDistance;
    public float MaxKnockbackDistance => _maxKnockbackDistance;
    public float KnockbackDuration => _knockbackDuration;
}
