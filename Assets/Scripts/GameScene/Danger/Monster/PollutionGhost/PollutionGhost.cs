using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PollutionGhost : MonoBehaviour
{
    [System.Serializable]
    public struct GhostSettings
    {
        // Movement / Bounds
        public float speed;
        public bool randomizeSpeed;
        public Vector2 speedRange;
        public float radiusWorld;
        public float groundY;

        // Lifetime / Timeout
        public float lifetime;
        public bool dropOnTimeout;
        public float contamRadiusWorld;

        // Path Contam
        public bool enablePathContam;
        public float pathContamStartDelay;
        public float pathContamInterval;
        public float pathContamRadius;
        public int maxPathContamDrops;

        // Hit
        public LayerMask killByLayers;
         public int hitsToKill;
    }

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
    public LayerMask killByLayers;         // 여기에 닿으면 데미지(예: Player, Disk, Projectile)
    public int hitsToKill = 1;   // 스포너에서 주입
    
    [SerializeField] string animSpeedParam = "Speed";
    Animator anim;
    int animSpeedHash;
    Vector3 prevPos; // 이동속도 계산용 (수평)

    [Header("Fx (optional)")]
    public ParticleSystem killFx;
    public ParticleSystem timeoutFx;
    public AudioSource killSfx;

    Vector3 velocity;       // 현재 이동 방향 * 속도

    float lifeClock;        // 생존 시간
    float pathClock;        // 경로 오염용 시계
    bool  pathStarted;
    float nextDropTimer;    // 다음 드롭까지 남은 시간
    int   droppedCount;
    float BoardY => board ? board.origin.y : 0f;
    int _hp;                     // 런타임 현재 HP
    public void ApplySettings(GhostSettings s)
    {
        speed = s.speed; randomizeSpeed = s.randomizeSpeed; speedRange = s.speedRange;
        radiusWorld = s.radiusWorld; groundY = s.groundY;
        lifetime = s.lifetime; dropOnTimeout = s.dropOnTimeout; contamRadiusWorld = s.contamRadiusWorld;
        enablePathContam = s.enablePathContam;
        pathContamStartDelay = s.pathContamStartDelay; pathContamInterval = s.pathContamInterval;
        pathContamRadius = s.pathContamRadius; maxPathContamDrops = s.maxPathContamDrops;
        killByLayers = s.killByLayers;
        hitsToKill = Mathf.Max(1, s.hitsToKill);  
    }

    void OnEnable()
    {
        // 페인트 시스템 캐시
        _paint = FindAnyObjectByType<BoardPaintSystem>();

        // Y 정렬
        if (board)
        {
            var p = transform.position;
            p.y = board.origin.y + groundY;
            transform.position = p;
        }

        // 속도(랜덤 옵션이면 여기서만 한 번 랜덤)
        float spd = randomizeSpeed ? UnityEngine.Random.Range(speedRange.x, speedRange.y) : speed;
        float ang = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        velocity = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * spd;

        // 런타임 상태 초기화
        prevPos = transform.position;
        lifeClock = 0f;
        pathClock = 0f;
        pathStarted = false;
        nextDropTimer = Mathf.Max(0.05f, pathContamInterval);
        droppedCount = 0;
        _hp = Mathf.Max(1, hitsToKill);
    }


    void Awake()
    {

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

        // ===== 수명 만료 처리 =====
        if (lifeClock >= lifetime)
        {
            Timeout();
        }
    }

    void OnCollisionEnter(Collision other)
    {
        int l = other.collider.gameObject.layer;
        if ((killByLayers.value & (1 << l)) == 0) return;

        // 미충족: 기존처럼 HP만 감소, 물리 충돌로 튕김
        TakeHit();
    }


    void TakeHit()
    {
        _hp--;
        if (_hp <= 0)
        {
            Die();
        }
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
