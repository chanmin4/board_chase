using UnityEngine;

[DisallowMultipleComponent]
public class PollutionGunner : MonoBehaviour
{
    // ====== Settings 주입 ======
    [System.Serializable]
    public struct Settings
    {
        // Movement
        public float moveSpeed;           // 평상 이동(지금은 후퇴만 쓰지만 확장용)
        public float retreatSpeed;        // 후퇴 속도
        public float retreatDuration;     // 발사 후 후퇴 시간(기본: 2초)
        public float turnSpeed;           // 회전 속도(deg/s)
        public float groundY;             // 보드 Y + 오프셋

        // Missile
        public SmallHomingMissile missilePrefab;
        public float missileLifetime;
        public float missileSpeed;
        public float hitRadiusWorld;
        public float timeoutRadiusWorld;
        public float gaugePenaltyOnHit;
        public float fireHeightOffset;    // 발사 위치 Y(보드 origin.y + offset)

        // Board clamp
        public float edgeMarginWorld;     // 보드 안쪽 마진(경계 뚫고 나가지 않게)
        public float postFireDelay;

        // ★ Hit/HP
        public LayerMask killByLayers;   // 여기에 맞으면 히트 처리
        public int hitsToKill;         // 필요 히트 수(HP)

    }

    [HideInInspector] public Settings config;

    // 주입
    public void ApplySettings(Settings s)
    {
        config = s; // 그대로 보관해서 나머지 로직은 config.* 사용

        // InkEater 스타일: 즉시 적용이 필요한 것만 로컬에도 캐시
        killByLayers = s.killByLayers;
    }


    // ====== Refs ======
    public SurvivalDirector director;
    public BoardGrid board;
    public SurvivalGauge gauge;
    public Transform targetPlayer;

    // 내부 상태
    float stateTimer;
    enum State { PreFireWait, Fire, Retreat }
    State state;
    Vector3 retreatTarget;
    int _hp;
    LayerMask killByLayers;
    void Awake()
    {
        if (!director) director = FindAnyObjectByType<SurvivalDirector>();
        if (!board) board = FindAnyObjectByType<BoardGrid>();
        if (!gauge) gauge = (director ? director.gauge : FindAnyObjectByType<SurvivalGauge>());
        if (!targetPlayer) targetPlayer = (director && director.player) ? director.player
                                 : GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    void GetBoardAxes(out Vector3 right, out Vector3 forward)
    {
        right = Vector3.ProjectOnPlane(board.transform.right, Vector3.up).normalized;
        forward = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up).normalized;
    }
    float BoardUSize() => board.width * board.tileSize;
    float BoardVSize() => board.height * board.tileSize;
    Vector3 UVToWorld(float u, float v, float addY)
    {
        GetBoardAxes(out var r, out var f);
        var w = board.origin + r * u + f * v;
        w.y = board.origin.y + addY;
        return w;
    }

    Vector2 WorldToUV(Vector3 world)
    {
        GetBoardAxes(out var r, out var f);
        Vector3 rel = world - board.origin;
        float u = Vector3.Dot(rel, r);
        float v = Vector3.Dot(rel, f);
        return new Vector2(u, v);
    }

    // ★ ADD: 현재 플레이어 위치 기준, 보드 내부(마진 내)에서 가장 먼 코너 반환
    Vector3 GetFarthestCornerFromPlayer(float margin, float addY)
    {
        float U = BoardUSize();
        float V = BoardVSize();

        float m = Mathf.Clamp(margin, 0f, Mathf.Min(U, V) * 0.49f);

        // 플레이어의 보드 로컬 좌표
        Vector2 puv = WorldToUV(targetPlayer.position);

        // 플레이어가 보드 중앙보다 왼쪽이면 오른쪽 코너, 위쪽이면 아래쪽 코너를 선택
        float uGoal = (puv.x < U * 0.5f) ? (U - m) : m;
        float vGoal = (puv.y < V * 0.5f) ? (V - m) : m;

        return UVToWorld(uGoal, vGoal, addY);
    }

    void OnEnable()
    {
        state = State.PreFireWait;
        stateTimer = (config.postFireDelay > 0f) ? config.postFireDelay : 2f;

        _hp = Mathf.Max(1, config.hitsToKill);
        // Y 정렬
        if (board)
        {
            var p = transform.position;
            p.y = board.origin.y + config.groundY;
            transform.position = p;
        }
    }

    void Update()
    {
        if (!board || !targetPlayer) return;

        switch (state)
        {
            case State.PreFireWait:           // ★ NEW: 가만히(또는 미세 조정 가능)
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    state = State.Fire;
                }
                break;

            case State.Fire:
                DoFireOnce();
                state = State.Retreat;
                stateTimer = config.retreatDuration > 0f ? config.retreatDuration : 2f;

                // ★ ADD: 이번 후퇴에서 향할 목표(보드 내부·플레이어에서 가장 먼 코너)
                retreatTarget = GetFarthestCornerFromPlayer(config.edgeMarginWorld, config.groundY);
                break;

            case State.Retreat:
                {
                    Vector3 here = transform.position;

                    // ★ 변경: “플레이어 반대 방향”이 아니라 계산된 가장 먼 코너로 이동
                    Vector3 to = retreatTarget - here;
                    to.y = 0f;

                    if (to.sqrMagnitude > 1e-6f)
                    {
                        Vector3 dir = to.normalized;
                        Quaternion toRot = Quaternion.LookRotation(dir, Vector3.up);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRot, config.turnSpeed * Time.deltaTime);

                        Vector3 next = here + dir * (config.retreatSpeed * Time.deltaTime);

                        // 항상 보드 내부 + Y 고정
                        next = ClampInsideBoard(next, board, config.edgeMarginWorld, config.groundY);
                        transform.position = next;
                    }
                    else
                    {
                        // 거의 도달했다면 타이머를 빠르게 소진
                        stateTimer = Mathf.Min(stateTimer, 0.1f);
                    }

                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        state = State.PreFireWait;
                        stateTimer = (config.postFireDelay > 0f) ? config.postFireDelay : 2f;
                    }
                    break;
                }

        }

    }

    void DoFireOnce()
    {
        if (!config.missilePrefab || !director) return;

        Vector3 p = transform.position;
        float baseY = board ? board.origin.y : p.y;
        p.y = baseY + config.fireHeightOffset;

        var m = Instantiate(config.missilePrefab, p, Quaternion.identity, transform);
        // SmallHomingMissile 설정
        var hm = m.GetComponent<SmallHomingMissile>();
        if (hm)
        {
            hm.Setup(director, targetPlayer,
                     config.missileLifetime,
                     config.missileSpeed,
                     config.hitRadiusWorld,
                     config.timeoutRadiusWorld,
                     gauge,
                     p.y);
            hm.gaugePenaltyOnHit = config.gaugePenaltyOnHit;
            // 시야 디버프 트리거는 나중에 이벤트 연결 시 여기서 호출하면 됨
        }
    }

    // 보드 내부 클램핑(회전 보드 지원: board.right/forward 투영)
    static Vector3 ClampInsideBoard(Vector3 world, BoardGrid board, float margin, float addY)
    {
        Vector3 right = Vector3.ProjectOnPlane(board.transform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up).normalized;

        float U = board.width * board.tileSize;
        float V = board.height * board.tileSize;

        Vector3 rel = world - board.origin;
        float u = Vector3.Dot(rel, right);
        float v = Vector3.Dot(rel, forward);

        float m = Mathf.Clamp(margin, 0f, Mathf.Min(U, V) * 0.49f);
        u = Mathf.Clamp(u, m, U - m);
        v = Mathf.Clamp(v, m, V - m);

        Vector3 clamped = board.origin + right * u + forward * v;
        clamped.y = board.origin.y + addY;
        return clamped;
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
        if (_hp <= 0) Die();
    }


    void Die()
    {
        // 필요 시 이펙트/사운드 추가 가능
        MobSpawnManager.Instance?.ReportMobKilled(MobType.Gunner);
        Destroy(gameObject);
    }

}
