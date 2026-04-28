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
    [SerializeField] private float _maxChargeTime = 1f;

    [Header("Radius")]
    [Min(0f)]
    [SerializeField] private float _baseRadius = 2f;
    [Min(0f)]
    [SerializeField] private float _maxRadius = 5f;

    [Header("Damage")]
    [SerializeField] private bool _applyDamage = true;
    [Min(0f)]
    [SerializeField] private float _baseDamage = 5f;
    [Min(0f)]
    [SerializeField] private float _maxDamage = 15f;

    [Header("Knockback")]
    [SerializeField] private bool _applyKnockback = true;
    [Min(0f)]
    [SerializeField] private float _baseKnockbackDistance = 2f;
    [Min(0f)]
    [SerializeField] private float _maxKnockbackDistance = 5f;
    [Min(0f)]
    [SerializeField] private float _knockbackDuration = 0.2f;

    public LayerMask HitMask => _hitMask;
    public QueryTriggerInteraction TriggerInteraction => _triggerInteraction;
    public float MaxChargeTime => _maxChargeTime;
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
