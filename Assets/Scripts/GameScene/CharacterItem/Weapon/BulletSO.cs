using UnityEngine;

public abstract class BulletSO : ScriptableObject
{
    [Header("Prefab")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Flight")]
    [Min(0.1f)]
    [SerializeField] private float speed = 18f;
    [Tooltip("탄환 충돌 판정 반경 총알이 벽/충돌 레이어에 얼마나 두껍게 맞는지 담당")]
    [Min(0.001f)]
    [SerializeField] private float castRadius = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float maxLifetime = 2f;

    [Min(0f)]
    [SerializeField] private float spawnOffset = 0.12f;

    [Header("Collision")]
    [Tooltip("no damage but mask for stop bullet")]
    [SerializeField] private LayerMask impactMask = 0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    public GameObject BulletPrefab => bulletPrefab;
    public float Speed => speed;
    public float CastRadius => castRadius;
    public float MaxLifetime => maxLifetime;
    public float SpawnOffset => spawnOffset;
    public LayerMask ImpactMask => impactMask;
    public QueryTriggerInteraction TriggerInteraction => triggerInteraction;
}
