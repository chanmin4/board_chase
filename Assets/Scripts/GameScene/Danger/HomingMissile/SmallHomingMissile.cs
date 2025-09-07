using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SmallHomingMissile : MonoBehaviour
{
    [Header("Refs")]
    [NonSerialized]public SurvivalDirector director;
    [NonSerialized] public Transform target;                 // 보통 PlayerDisk
    [NonSerialized] public SurvivalGauge gauge;              // 선택(없으면 패널티 생략)

    [Header("Movement")]
    public float moveSpeed = 6f;             // 느린 추적 속도
    public float turnLerp = 10f;             // 방향 전환 민감도
    public float groundY = 1.0f;            // ★ 바닥과 분리(회전벽 피함, 디폴트 1)

    [Header("Lifetime / Explosion")]
    public float lifetime = 4f;              // 사이클 길이만큼 세팅됨
    public float hitRadiusWorld = 2.0f;  // 플레이어 맞췄을 때 오염(“크게”)
    public float timeoutRadiusWorld = 0.8f;  // 수명 만료 시 오염(“작게”)
    public float gaugePenaltyOnHit = 1.2f;  // 맞으면 게이지 감소량(프로젝트 단위에 맞춰 조절)
    public LayerMask playerMask;             // Player 레이어(또는 Tag) 충돌 체크

    [Header("Hit Check")]
    [Tooltip("수평(XZ) 거리로만 플레이어 히트 판정(높이 무시). Y를 띄워도 맞으면 터집니다.")]
    public bool planarHitCheck = true;
    public float planarHitRadius = 0.45f;    // 디스크 중심과의 수평 히트 반경

    [Header("FX (optional)")]
    public ParticleSystem trailFx;
    public ParticleSystem explodeFx;
    public AudioSource explodeSfx;

    SphereCollider trigger;                   // 보조용(있어도 되고 없어도 됨)
    float t;

    void Awake()
    {
        // 보조용 트리거(없으면 생성). 회전벽과 겹쳐도 OnTriggerEnter는 Player만 처리.
        trigger = GetComponent<SphereCollider>();
        if (!trigger)
        {
            trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.35f;
        }
    }

    public void Setup(SurvivalDirector dir, Transform tgt, float lifeSeconds,
                      float speed, float hitR, float timeoutR,
                      SurvivalGauge gaugeRef = null, float yHeight = 1.0f)
    {
        director = dir;
        target = tgt;
        lifetime = Mathf.Max(0.1f, lifeSeconds);
        moveSpeed = speed;
        hitRadiusWorld = hitR;
        timeoutRadiusWorld = timeoutR;
        gauge = gaugeRef;
        groundY = yHeight;
        var p = transform.position; p.y = groundY; transform.position = p;
    }

    void Update()
    {
        t += Time.deltaTime;

        // 이동(지면 고정 + XZ 추적)
        if (target)
        {
            Vector3 here = transform.position;
            Vector3 want = target.position; want.y = here.y;

            Vector3 dir = (want - here);
            float d2 = dir.sqrMagnitude;
            if (d2 > 0.0001f)
            {
                dir.Normalize();
                Vector3 fwd = Vector3.Slerp(transform.forward, dir, 1f - Mathf.Exp(-turnLerp * Time.deltaTime));
                transform.forward = new Vector3(fwd.x, 0f, fwd.z).normalized;
            }
            transform.position = new Vector3(
                here.x + transform.forward.x * moveSpeed * Time.deltaTime,
                groundY,
                here.z + transform.forward.z * moveSpeed * Time.deltaTime
            );
        }

        // 수평 히트 판정(높이 무시) — 중앙 회전벽을 피하려고 Y를 띄워도 디스크 맞으면 터짐
        if (planarHitCheck && target)
        {
            Vector2 a = new Vector2(transform.position.x, transform.position.z);
            Vector2 b = new Vector2(target.position.x, target.position.z);
            if ((a - b).sqrMagnitude <= planarHitRadius * planarHitRadius)
            {
                if (gauge && gaugePenaltyOnHit > 0f) gauge.Add(-gaugePenaltyOnHit);
                Explode(hitRadiusWorld);
                return;
            }
        }

        // 수명 종료 → 작은 오염 생성 후 파괴
        if (t >= lifetime)
        {
            Explode(timeoutRadiusWorld);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 보조용: 레이어/태그가 플레이어면 즉시 히트
        bool isPlayer = ((playerMask.value != 0) && ((playerMask.value & (1 << other.gameObject.layer)) != 0))
                        || other.CompareTag("Player");

        if (!isPlayer) return;

        if (gauge && gaugePenaltyOnHit > 0f) gauge.Add(-gaugePenaltyOnHit);
        Explode(hitRadiusWorld);
    }

    void Explode(float radiusW)
    {
        if (director) director.ContaminateCircleWorld(transform.position, radiusW);
        if (explodeFx) Instantiate(explodeFx, transform.position, Quaternion.identity);
        if (explodeSfx) explodeSfx.Play();
        Destroy(gameObject);
    }
}
