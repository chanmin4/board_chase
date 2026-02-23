using UnityEngine;

public class InkBullet : MonoBehaviour
{
    [Header("Refs")]
    public PlayerDisk owner;
    public BoardPaintSystem paint;

    [Header("Flight")]
    public Vector3 targetWorld;
    public float speed = 18f;
    public float maxLife = 2.0f;
    public float castRadius = 0.12f;

    [Header("Hit Masks")]
    public LayerMask damageMask; // 몬스터 레이어
    public LayerMask blockMask;  // 벽/장애물 레이어
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Damage / Paint")]
    public float directDamage = 25f;
    public float paintRadiusWorld = 0.6f;
    public bool clearOtherMask = true;
    public bool paintOnMiss = true;

    float _life;
    Vector3 _dir;


    public void Init(PlayerDisk disk, Vector3 target, float spd, float dmg, float paintRadius, bool clearOther,
                     LayerMask dmgMask, LayerMask blkMask)
    {
        owner = disk;
        targetWorld = target;
        speed = Mathf.Max(0.1f, spd);
        directDamage = Mathf.Max(0f, dmg);
        paintRadiusWorld = Mathf.Max(0.05f, paintRadius);
        clearOtherMask = clearOther;
        damageMask = dmgMask;
        blockMask = blkMask;

        paint = (disk != null) ? disk.paintSystem : FindAnyObjectByType<BoardPaintSystem>();

        Vector3 from = transform.position;
        Vector3 to = targetWorld; to.y = from.y; // 수평으로 날려도 되고, 원하면 제거
        _dir = (to - from);
        if (_dir.sqrMagnitude < 1e-6f) _dir = transform.forward;
        _dir.y = 0f;
        _dir.Normalize();
        transform.forward = _dir;
    }

void Awake()
{
    // 실수로 무기/플레이어 자식으로 붙어도, 총알은 월드에서 독립적으로 날아가야 함
     if (transform.parent != null && transform.parent.GetComponentInParent<PlayerDisk>() != null)
        transform.SetParent(null, true);
}
    void Update()
    {
        _life += Time.deltaTime;
        if (_life > maxLife)
        {
            if (paintOnMiss) DoPaint(targetWorld);
            Destroy(gameObject);
            return;
        }

        Vector3 pos = transform.position;
        float step = speed * Time.deltaTime;

        // 목표까지 남은 거리
        Vector3 flatTarget = targetWorld; flatTarget.y = pos.y;
        float distToTarget = (flatTarget - pos).magnitude;
        float travel = Mathf.Min(step, distToTarget);

        // 이번 프레임 이동 구간에 대해 히트 체크(데미지/블록 각각 캐스트 후 더 가까운 것 선택)
        bool hasDamage = Physics.SphereCast(pos, castRadius, _dir, out var hitDmg, travel, damageMask, triggerInteraction);
        bool hasBlock  = Physics.SphereCast(pos, castRadius, _dir, out var hitBlk, travel, blockMask,  triggerInteraction);

        if (hasDamage || hasBlock)
        {
            RaycastHit hit = default;
            bool isDamageHit = false;

            if (hasDamage && hasBlock)
            {
                if (hitDmg.distance <= hitBlk.distance) { hit = hitDmg; isDamageHit = true; }
                else { hit = hitBlk; isDamageHit = false; }
            }
            else if (hasDamage) { hit = hitDmg; isDamageHit = true; }
            else { hit = hitBlk; isDamageHit = false; }

            // 데미지
            if (isDamageHit && directDamage > 0f)
            {
                var dmgable = hit.collider.GetComponentInParent<IInkDamageable>();
                dmgable?.ApplyInkDamage(directDamage, hit.point, owner ? owner.gameObject : gameObject);
            }

            // 잉크 스플래시
            DoPaint(hit.point);

            Destroy(gameObject);
            return;
        }

        // 실제 이동
        transform.position = pos + _dir * travel;

        // 목표 도착(아무것도 안 맞음)
        if (distToTarget <= 0.05f)
        {
            if (paintOnMiss) DoPaint(targetWorld);
            Destroy(gameObject);
        }
    }

    void DoPaint(Vector3 point)
{
    if (owner != null)
    {
        point.y = owner.GroundY; // ★ 페인트는 항상 바닥 기준
        owner.TryPaintPlayerCircle(point, paintRadiusWorld, clearOtherMask);
    }
    else if (paint != null)
    {
        point.y = 0f; // 필요하면 board origin y로
        paint.TryStampCircleNow(BoardPaintSystem.PaintChannel.Player, point, paintRadiusWorld, clearOtherMask);
    }
}

}
