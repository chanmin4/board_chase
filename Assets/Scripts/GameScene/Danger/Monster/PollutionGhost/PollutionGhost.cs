using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PollutionGhost : MonoBehaviour
{
    [Header("Refs")]
    [NonSerialized] public SurvivalDirector director;
    [NonSerialized] public BoardGrid board;
    [NonSerialized] public Transform player; // 주로 PlayerDisk
    [NonSerialized]BoardPaintSystem _paint;

    [Header("Movement")]
    public float speed = 3.5f;             // 이동 속도 (유닛/초)
    public float radiusWorld = 0.35f;      // 벽과 반사 계산용 반경
    public float groundY = 0.2f;           // 바닥 높이
    public bool  randomizeSpeed = false;
    public Vector2 speedRange = new Vector2(2.0f, 5.0f);

    [Header("Lifetime / Fail Rule")]
    public float lifetime = 8f;            // 이 시간 안에 못 죽이면 실패 처리
    [Tooltip("기존: 수명 만료시 마지막 위치에 오염 생성할지")]
    public bool dropOnTimeout = false;     // 원래 동작을 옵션으로 유지
    public float contamRadiusWorld = 1.4f; // (타임아웃 전용) 오염 반경

    [Header("Path Contamination (경로 오염)")]
    [Tooltip("스폰 후 일정 지연 뒤부터, 이동 경로에 주기적으로 오염을 생성")]
    public bool enablePathContam = true;
    [Tooltip("스폰 후 몇 초 뒤부터 경로 오염을 시작할지")]
    public float pathContamStartDelay = 2.0f;
    [Tooltip("경로 오염 생성 주기(초)")]
    [Min(0.05f)] public float pathContamInterval = 1.0f;
    [Tooltip("경로 오염의 반경(월드 단위)")]
    public float pathContamRadius = 1.0f;
    [Tooltip("경로 오염 최대 드롭 개수 (0=무제한)")]
    public int maxPathContamDrops = 0;

    [Header("Hit Rule")]
    public LayerMask killByLayers;         // 여기에 닿으면 즉사(예: Player, Disk, Projectile)
    public bool planarHitCheck = false;    // 수평거리로도 판정하고 싶으면 켜기
    public float planarHitRadius = 0.45f;

    [SerializeField] string animSpeedParam = "Speed";
    Animator anim;
    int animSpeedHash;
    Vector3 prevPos; // 이동속도 계산용 (수평)

    [Header("Fx (optional)")]
    public ParticleSystem killFx;
    public ParticleSystem timeoutFx;
    public AudioSource killSfx;

    MeshCollider trigger; // 내부 관통용: isTrigger = true
    Vector3 velocity;       // 현재 이동 방향 * 속도

    float lifeClock;        // 생존 시간
    float pathClock;        // 경로 오염용 시계
    bool  pathStarted;
    float nextDropTimer;    // 다음 드롭까지 남은 시간
    int   droppedCount;
    float BoardY => board ? board.origin.y : 0f;

    // ===== 초기화(스포너가 호출) =====
    public void Setup(SurvivalDirector dir, BoardGrid bd, Transform ply, float spd, float life, float contamR)
    {
        director = dir; board = bd; player = ply;
        speed = randomizeSpeed ? UnityEngine.Random.Range(speedRange.x, speedRange.y) : spd;
        lifetime = life;
        contamRadiusWorld = contamR; // (타임아웃 전용 기본값으로 재활용 가능)
        _paint = FindAnyObjectByType<BoardPaintSystem>();
        if (!_paint) Debug.LogWarning("[Ghost] BoardPaintSystem not found.");
        // 랜덤 방향으로 시작
        float ang = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        velocity = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * speed;

        // 높이 고정
        var p = transform.position;
        p.y = BoardY + groundY;          // 기존: groundY
        transform.position = p;
        
        prevPos = transform.position;
        if (anim) anim.SetFloat(animSpeedHash, 0f);
        // 경로 오염 타이머 초기화
        lifeClock = 0f; pathClock = 0f; pathStarted = false;
        nextDropTimer = pathContamInterval;
        droppedCount = 0;
    }

    void Awake()
    {
        trigger = GetComponentInChildren<MeshCollider>();
        if (!trigger) trigger = gameObject.AddComponent<MeshCollider>();
        trigger.isTrigger = true;                                // 내부 오브젝트는 관통

        anim = GetComponentInChildren<Animator>(true);
        if (anim) animSpeedHash = Animator.StringToHash(animSpeedParam);
    }

    void Update()
    {
        // ===== 이동 =====
        Vector3 pos = transform.position;
        pos += velocity * Time.deltaTime;
        pos.y = BoardY + groundY; 

        // 외벽 반사 (보드 외곽 Rect 기준)
        if (board)
        {
            var r = board.GetWallOuterRectXZ();
            float minX = r.xMin + radiusWorld;
            float maxX = r.xMax - radiusWorld;
            float minZ = r.yMin + radiusWorld;
            float maxZ = r.yMax - radiusWorld;

            // X 축 반사
            if (pos.x < minX) { pos.x = minX; velocity.x = Mathf.Abs(velocity.x); }
            else if (pos.x > maxX) { pos.x = maxX; velocity.x = -Mathf.Abs(velocity.x); }

            // Z 축 반사
            if (pos.z < minZ) { pos.z = minZ; velocity.z = Mathf.Abs(velocity.z); }
            else if (pos.z > maxZ) { pos.z = maxZ; velocity.z = -Mathf.Abs(velocity.z); }
        }

        // 회전(진행방향으로)
        if (velocity.sqrMagnitude > 0.0001f)
            transform.forward = new Vector3(velocity.x, 0f, velocity.z).normalized;

        transform.position = pos;
        if (anim)
        {
            Vector3 delta = transform.position - prevPos;
            delta.y = 0f;
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);
            float planarSpeed = delta.magnitude / dt;     // m/s
            anim.SetFloat(animSpeedHash, planarSpeed);  // 0.1 임계로 Idle/Move 전환
        }
        prevPos = transform.position;
        // ===== 경로 오염 생성 로직 =====
        if (enablePathContam && director)
        {
            lifeClock += Time.deltaTime;
            if (!pathStarted && lifeClock >= pathContamStartDelay)
            {
                pathStarted = true;
                nextDropTimer = 0f; // 시작 즉시 첫 드롭(원하면 pathContamInterval로 바꿔도 됨)
            }

            if (pathStarted && (maxPathContamDrops == 0 || droppedCount < maxPathContamDrops))
            {
                nextDropTimer -= Time.deltaTime;
                if (nextDropTimer <= 0f)
                {
                    Vector3 dropPos = transform.position;
                    dropPos.y = BoardY + groundY;
                    float r = Mathf.Max(0.01f, pathContamRadius);

                    if (_paint)
                        _paint.EnqueueCircle(BoardPaintSystem.PaintChannel.Enemy, dropPos, r);
                    else if (director)
                        director.ContaminateCircleWorld(dropPos, r); // 폴백

                    droppedCount++;
                    nextDropTimer = pathContamInterval;
                }
            }

        }
        else
        {
            lifeClock += Time.deltaTime; // 경로오염 비활성이어도 수명은 흐름
        }

        // ===== 수평 거리 히트(선택) =====
        if (planarHitCheck && player)
        {
            Vector2 a = new Vector2(transform.position.x, transform.position.z);
            Vector2 b = new Vector2(player.position.x,   player.position.z);
            if ((a - b).sqrMagnitude <= planarHitRadius * planarHitRadius)
            {
                Die();
                return;
            }
        }

        // ===== 수명 만료 처리 =====
        if (lifeClock >= lifetime)
        {
            Timeout();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (killByLayers.value != 0 && ((killByLayers.value & (1 << other.gameObject.layer)) != 0))
        {
            Die();
            return;
        }
        // 내부 오브젝트는 관통: 아무 것도 하지 않음
    }

    void Die()
    {
        if (killFx) Instantiate(killFx, transform.position, Quaternion.identity);
        if (killSfx) killSfx.Play();
        MobSpawnManager.Instance?.ReportMobKilled(MobType.Ghost);
        Destroy(gameObject);
    }

    void Timeout()
    {
        if (dropOnTimeout)
        {
            Vector3 pos = transform.position; pos.y = BoardY + groundY;
            float r = Mathf.Max(0.01f, (pathContamRadius > 0f ? pathContamRadius : contamRadiusWorld));
            if (_paint) _paint.EnqueueCircle(BoardPaintSystem.PaintChannel.Enemy, pos, r);
            else if (director) director.ContaminateCircleWorld(pos, r);
        }
        if (timeoutFx) Instantiate(timeoutFx, transform.position, Quaternion.identity);
        MobSpawnManager.Instance?.ReportMobKilled(MobType.Ghost);
        Destroy(gameObject);
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radiusWorld);

        // 경로 오염 반경 시각화
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, pathContamRadius));
    }
#endif
}
