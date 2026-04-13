using UnityEngine;

public abstract class BulletSO : ScriptableObject
{
    [Header("Prefab")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Flight")]
    [Min(0.1f)]
    [SerializeField] private float speed = 18f;

    [Min(0.001f)]
    [SerializeField] private float castRadius = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float maxLifetime = 2f;

    [Header("Collision")]
    [SerializeField] private LayerMask blockHitMask;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    public GameObject BulletPrefab => bulletPrefab;
    public float Speed => speed;
    public float CastRadius => castRadius;
    public float MaxLifetime => maxLifetime;
    public LayerMask BlockHitMask => blockHitMask;
    public QueryTriggerInteraction TriggerInteraction => triggerInteraction;
}
