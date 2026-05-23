using UnityEngine;

public enum BulletAmmoType
{
    Attack,
    Paint,
    Special
}

/// <summary>
/// Base definition for every bullet item.
/// Owns shared bullet data and the ammo type used by HUD/loadout rules.
/// AttackBulletSO and PaintBulletSO override AmmoType so default bullet assets cannot be misconfigured.
/// </summary>
public abstract class BulletSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string bulletId = "bullet_default";
    [SerializeField] private string displayName = "Bullet";

    [Header("Ammo Type")]
    [Tooltip("Used by loadout slot rules and HUD type icon. Attack/Paint subclasses override this.")]
    [SerializeField] private BulletAmmoType ammoType = BulletAmmoType.Special;

    [Header("UI")]
    [SerializeField] private Sprite icon;

    [Header("Prefab")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Flight")]
    [Min(0.1f)]
    [SerializeField] private float speed = 18f;

    [Min(0.001f)]
    [SerializeField] private float castRadius = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float maxLifetime = 2f;

    [Min(0f)]
    [SerializeField] private float spawnOffset = 0.12f;

    [Header("Collision")]
    [SerializeField] private LayerMask impactMask = 0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

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
}