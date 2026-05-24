using UnityEngine;

public enum BulletAmmoType
{
    Attack,
    Paint,
    Special
}

public abstract class BulletSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string bulletId = "bullet_default";
    [SerializeField] private string displayName = "Bullet";

    [Header("Ammo Type")]
    [SerializeField] private BulletAmmoType ammoType = BulletAmmoType.Special;

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Prefab")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Flight")]
    [Min(0.1f)] [SerializeField] private float speed = 18f;
    [Min(0.001f)] [SerializeField] private float castRadius = 0.08f;
    [Min(0.01f)] [SerializeField] private float maxLifetime = 2f;
    [Min(0f)] [SerializeField] private float spawnOffset = 0.12f;

    [Header("Collision")]
    [SerializeField] private LayerMask impactMask = 0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Stat Modifiers")]
    [Tooltip("Bullet bonus on top of current player stats. Example: AttackDamage FlatAdd +5, AttackDamage PercentAdd 0.2.")]
    [SerializeField] private PlayerStatModifier[] statModifiers;

    public string BulletId => bulletId;
    public string DisplayName => displayName;
    public virtual BulletAmmoType AmmoType => ammoType;
    public Sprite Icon => icon;

    public GameObject BulletPrefab => bulletPrefab;
    public float Speed => speed;
    public float CastRadius => castRadius;
    public float MaxLifetime => maxLifetime;
    public float SpawnOffset => spawnOffset;
    public LayerMask ImpactMask => impactMask;
    public QueryTriggerInteraction TriggerInteraction => triggerInteraction;

    public PlayerStatModifier[] StatModifiers => statModifiers;
}