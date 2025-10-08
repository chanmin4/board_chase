using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class PollutionBoss : MonoBehaviour
{
    // ───────── Bounds/Spawn ─────────
    [Header("Bounds/Spawn")]
    public float groundY = 0.2f;
    public float bossRadius = 0.6f;
    public float wallPaddingWorld = 0.35f;

    // ───────── HP ─────────
    [Header("HP")]
    public int maxHP = 12;
    public int damagePerHit = 1;
    public LayerMask damageByLayers;

    // ───────── Rocket ─────────
    [Header("Rocket")]
    [Tooltip("첫 발사 지연(초)")] public float firstSpawnDelay = 2f;
    [Tooltip("발사 간격(초)")]   public float spawnInterval   = 7.5f;
    [Tooltip("로켓 생존 시간(초)")] public float rocketLifetime  = 5f;
    [Tooltip("유도 속도(m/s)")]   public float homingSpeed     = 7f;
    [Tooltip("보스 중심에서 Y 추가 오프셋")] public float rocketSpawnYOffset = 0f;

    // ───────── HP UI (막대형) ─────────
    public enum HpUiMode { ScreenTopRight, WorldBillboard, WorldFlat }

    [Header("HP UI Mode")]
    public HpUiMode hpUiMode = HpUiMode.WorldFlat;   // 기본: 월드에서 눕힌 막대

    [Header("Screen (Top-Right HUD)")]
    public Vector2 screenMarginTR = new Vector2(18f, 18f); // 우상단 여백(px)
    public Vector2 hudBarSizePx   = new Vector2(260f, 22f);// 전체 크기(px)
    public float   hudInnerPadPx  = 2f;                    // 내부 패딩(px)
    public int     hudSortingOrder = 1000;                 // 정렬 우선순위

    [Header("World Placement (Zone follower 스타일)")]
    [Tooltip("보스 기준 각도(시계 방향, 0=+X)")] public float worldAngleDeg = 45f;
    [Tooltip("보스 반경 * 배수 + 추가미터 = 바 중심까지 거리")]
    public float radialDistanceMul = 1.10f;
    [Tooltip("추가 미터 오프셋")] public float radialExtraMeters = 0.30f;
    [Tooltip("보스 중심에서 위로 띄우기(m)")] public float worldLift = 0.60f;

    [Header("World Bar Size")]
    [Tooltip("월드 막대 크기 (미터)")] public Vector2 worldBarSizeM = new Vector2(0.80f, 0.12f);
    [Tooltip("월드 막대 내부 패딩 (미터)")] public float worldBarInnerPadM = 0.01f;
    [Tooltip("WorldSpace 캔버스 전체 배율(1 = 그대로)")] public float worldCanvasScale = 1.0f;
    [Tooltip("WorldFlat일 때 X축 회전 (90=바닥에 눕힘)")] public float worldFlatAngleDeg = 90f;

    

    // ───────── 주입(스포너가 Setup으로만 넣음) ─────────
    BoardGrid _board;
    Transform _player;
    SurvivalDirector _director;
    HomingRocket _rocketPrefab;

    // ───────── 내부 상태 ─────────
    int _hp;
    float _timer;
    bool _firstDone;
    HomingRocket _activeRocket;
    float BoardY => _board ? _board.origin.y : 0f;

    // UI refs
    Transform _uiRoot;
    RectTransform _uiRT;
    Image _uiFill;

    // ───────── Setup (스포너가 호출) ─────────
    public void Setup(BoardGrid board, Transform player, SurvivalDirector director, HomingRocket rocketPrefab)
    {
        _board        = board;
        _player       = player;
        _director     = director;
        _rocketPrefab = rocketPrefab;

        var p = transform.position;
        p.y = BoardY + groundY;          // 기존: groundY
        transform.position = p;

        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = Mathf.Max(bossRadius, 0.1f);

        _hp = Mathf.Max(1, maxHP);
        EnsureHPUI_Bar();   // 막대형 UI 생성
        UpdateHPUIFill();   // 초기 반영

        _timer = 0f; _firstDone = false;
    }

    void Update()
    {
        // 로켓 발사 루프
        if (_rocketPrefab != null)
        {
            _timer += Time.deltaTime;

            if (!_firstDone)
            {
                if (_timer >= firstSpawnDelay)
                {
                    _timer = 0f;
                    FireRocket();
                    _firstDone = true;
                }
            }
            else
            {
                while (_timer >= spawnInterval)
                {
                    _timer -= spawnInterval;
                    if (_activeRocket) { _activeRocket.Explode(); _activeRocket = null; }
                    FireRocket();
                }
            }
        }

        UpdateHPUIPose_Bar();
    }

    // ───────── Rocket ─────────
    void FireRocket()
    {
        // 보스 '정중앙'에서 바로 출발
        Vector3 p = transform.position;
        p.y += rocketSpawnYOffset;

        var r = Instantiate(_rocketPrefab, p, Quaternion.identity);
        _activeRocket = r;

        // 낙하 제거: 현재 높이 고정
        r.startHeight  = p.y;
        r.groundY      = p.y;
        r.fallDuration = 0f;

        // 타깃/유도 설정
        Transform tgt = _player ? _player : (_director ? _director.player : null);

        try
        {
            r.Setup(_director, Mathf.Max(0.1f, rocketLifetime), tgt, homingSpeed, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PollutionBoss] HomingRocket.Setup 실패: {e.Message}");
        }
    }

    // ───────── Damage / Hit ─────────
    void OnTriggerEnter(Collider other)
    {
        if (damageByLayers.value == 0)
        {
            Hit(damagePerHit);
            return;
        }
        if ((damageByLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            Hit(damagePerHit);
        }
    }

    void Hit(int dmg)
    {
        if (_hp <= 0) return;

        _hp = Mathf.Max(0, _hp - Mathf.Max(1, dmg));
        UpdateHPUIFill();

        if (_hp <= 0) { Die(); return; }
        TeleportInsideWalls();
    }

    void TeleportInsideWalls()
    {
        if (_board == null) return;

        var rect = new Rect(
            _board.origin.x,
            _board.origin.z,
            _board.width  * _board.tileSize,
            _board.height * _board.tileSize
        );

        float pad = wallPaddingWorld + bossRadius;
        float minX = rect.xMin + pad;
        float maxX = rect.xMax - pad;
        float minZ = rect.yMin + pad;
        float maxZ = rect.yMax - pad;

        Vector3 pos = new Vector3(
    UnityEngine.Random.Range(minX, maxX),
    BoardY + groundY,             // 기존: groundY
    UnityEngine.Random.Range(minZ, maxZ)
);
        transform.position = pos;


        UpdateHPUIPose_Bar();
    }

    void Die()
    {
        if (_activeRocket) { _activeRocket.Explode(); _activeRocket = null; }
        if (_uiRoot) Destroy(_uiRoot.gameObject);
        Destroy(gameObject);
    }

    // ───────── HP UI (막대형) ─────────
    void EnsureHPUI_Bar()
    {
        if (_uiRoot) return;

        if (hpUiMode == HpUiMode.ScreenTopRight)
        {
            // 화면 우상단 HUD
            var root = new GameObject("BossHP_Screen",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = hudSortingOrder;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _uiRoot = root.transform;
            _uiRT = (RectTransform)_uiRoot;
            _uiRT.sizeDelta = hudBarSizePx;
            _uiRT.anchorMin = new Vector2(1, 1);
            _uiRT.anchorMax = new Vector2(1, 1);
            _uiRT.pivot     = new Vector2(1, 1);
            _uiRT.anchoredPosition = new Vector2(-screenMarginTR.x, -screenMarginTR.y);

            BuildBarVisual(_uiRT, /*px*/ hudInnerPadPx);
        }
        else
        {
            // 월드-스페이스 막대
            var root = new GameObject("BossHP_World",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));

            _uiRoot = root.transform;
            _uiRoot.SetParent(transform, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = hudSortingOrder;
            if (!canvas.worldCamera && Camera.main) canvas.worldCamera = Camera.main;

            _uiRT = (RectTransform)_uiRoot;
            _uiRT.sizeDelta  = worldBarSizeM;                // 월드 단위 크기
            _uiRT.localScale = Vector3.one * worldCanvasScale;

            if (hpUiMode == HpUiMode.WorldFlat)
                _uiRoot.localRotation = Quaternion.Euler(worldFlatAngleDeg, 0f, 0f);
            else
                _uiRoot.localRotation = Quaternion.identity;

            BuildWorldBarVisual(_uiRT, /*meters*/ worldBarInnerPadM);
            UpdateHPUIPose_Bar();
        }
    }

    // 스크린/월드 공통 막대 시각(스크린용: 단위 px 기준 패딩)
    void BuildBarVisual(RectTransform parent, float innerPadPx)
    {
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        bgRT.SetParent(parent, false);
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var frt = (RectTransform)fill.transform;
        frt.SetParent(parent, false);
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(innerPadPx, innerPadPx);
        frt.offsetMax = new Vector2(-innerPadPx, -innerPadPx);

        _uiFill = fill.GetComponent<Image>();
        _uiFill.type = Image.Type.Filled;
        _uiFill.fillMethod = Image.FillMethod.Horizontal;
        _uiFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _uiFill.color = new Color(0.95f, 0.25f, 0.25f, 0.95f);
    }

    // 월드용 막대 시각(단위 m 기준 패딩)
    void BuildWorldBarVisual(RectTransform parent, float innerPadM)
    {
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        bgRT.SetParent(parent, false);
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var frt = (RectTransform)fill.transform;
        frt.SetParent(parent, false);
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(innerPadM, innerPadM);
        frt.offsetMax = new Vector2(-innerPadM, -innerPadM);

        _uiFill = fill.GetComponent<Image>();
        _uiFill.type = Image.Type.Filled;
        _uiFill.fillMethod = Image.FillMethod.Horizontal;
        _uiFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _uiFill.color = new Color(0.95f, 0.25f, 0.25f, 0.95f);
    }

    void UpdateHPUIFill()
    {
        if (!_uiFill) return;
        float t = Mathf.Clamp01((float)_hp / Mathf.Max(1, maxHP));
        _uiFill.fillAmount = t;
    }

    void UpdateHPUIPose_Bar()
    {
        if (!_uiRoot) return;

        if (hpUiMode == HpUiMode.ScreenTopRight)
            return;

        // 월드 모드: 보스 중심 기준 각도/반경/높이
        float a = Mathf.Deg2Rad * worldAngleDeg;
        Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
        float dist = bossRadius * Mathf.Max(0f, radialDistanceMul) + Mathf.Max(0f, radialExtraMeters);

        Vector3 pos = transform.position + dir * dist + Vector3.up * Mathf.Max(worldLift, 0f);
        _uiRoot.position = pos;

        if (hpUiMode == HpUiMode.WorldFlat)
        {
            _uiRoot.rotation = Quaternion.Euler(worldFlatAngleDeg, 0f, 0f); // 눕힘
        }
        else // Billboard
        {
            var cam = Camera.main;
            if (cam)
            {
                Vector3 fwd = cam.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                _uiRoot.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }

        // 실시간 크기 반영(튜닝 편의)
        if (_uiRT)
        {
            if (hpUiMode == HpUiMode.ScreenTopRight)
                _uiRT.sizeDelta = hudBarSizePx;
            else
                _uiRT.sizeDelta = worldBarSizeM;
        }
    }
}
