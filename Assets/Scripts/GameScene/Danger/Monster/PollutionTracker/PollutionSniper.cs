using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IPlayerSurvivalDamage
{
    void ApplySurvivalDamage(float amount);
}

[DisallowMultipleComponent]
//[RequireComponent(typeof(SphereCollider))]
public class PollutionSniper : MonoBehaviour
{
    // ───────── 주입(프리팹 저장 X) ─────────
    BoardGrid _board;
    Transform _player;
    SurvivalDirector _director;

    // ───────── HP / 히트 ─────────
    [Header("HP / Hit")]
    public int maxHP = 3;
    public int damagePerHit = 1;               // 디스크 등 충돌 시 깎이는 양
    public LayerMask hitByLayers;              // 스나이퍼에 데미지 주는 레이어
    public float bodyRadius = 0.5f;
    public float groundY = 0.2f;

    // ───────── 조준(속도 제한) ─────────
    [Header("Aim (speed-limited tracking)")]
    [Tooltip("조준선이 따라갈 목표 위치의 관성(지연) 정도(초당) — 높일수록 타깃을 빨리 따라감")]
    public float aimTargetFollowSpeed = 6f;
    [Tooltip("조준선 자체의 최대 회전 속도(도/초) — 낮을수록 더 쉽게 빗나감")]
    public float aimAngularSpeedDeg = 180f;

    // ───────── 공격 타이밍 ─────────
    [Header("Attack Timing")]
    [Tooltip("조준(미리보기) 시간")]
    public float aimPreviewTime = 3.0f;        // 요구사항: 3초
    [Tooltip("레이저 표시 시간(순간 발사 느낌이면 0.15~0.3 추천)")]
    public float fireShowDuration = 0.25f;
    [Tooltip("발사 후 대기")]
    public float cooldown = 3.0f;

    // ───────── 레이저/오염 파라미터 ─────────
    [Header("Laser / Contamination")]
    [Tooltip("레이저 최대 길이(보드 밖으로 뻗지 않음)")]
    public float beamLength = 100f;
    [Tooltip("플레이어 히트 시 생존게이지 감소량(한 번 발사마다)")]
    public float survivalDamageOnHit = 4f;
    [Tooltip("오염 살포 간격(미터)")]
    public float contamStepMeters = 0.6f;
    [Tooltip("오염 살포 반경(미터)")]
    public float contamRadiusMeters = 0.7f;
    [Tooltip("플레이어/환경 히트 판정 레이어(플레이어 포함)")]
    public LayerMask hitLayers;

    // ───────── 라인(조준/레이저 표시) ─────────
    [Header("Line (Preview/Laser)")]
    public LineRenderer line;                  // 비우면 자동 생성
    public float previewLineWidth = 0.08f;     // 굵은 프리뷰
    public float fireLineWidth = 0.10f;        // 굵은 레이저
    public Color previewColor = new Color(0.2f, 1f, 0.6f, 0.95f);
    public Color fireColor    = new Color(1f, 0.2f, 0.2f, 1f);

    // ───────── HP UI (막대형) ─────────
    public enum HpUiMode { ScreenTopRight, WorldBillboard, WorldFlat }

    [Header("HP UI Mode")]
    public HpUiMode hpUiMode = HpUiMode.WorldBillboard;

    [Header("Screen (Top-Right HUD)")]
    public Vector2 screenMarginTR = new Vector2(18f, 18f);
    public Vector2 hudBarSizePx = new Vector2(220f, 18f);
    public float hudInnerPadPx = 2f;
    public int hudSortingOrder = 1000;

    [Header("World Bar")]
    public Vector2 worldBarSizeM = new Vector2(0.7f, 0.1f);
    public float worldBarInnerPadM = 0.01f;
    public float worldLift = 0.7f;
    public float worldScale = 1.0f;
    public float worldFlatAngleDeg = 0f; // 0=세움, 90=눕힘
    float BoardY => _board ? _board.origin.y : 0f;

    // ───────── 내부 상태 ─────────
    int _hp;

    enum State { Aiming, Firing, Cooldown }
    State _state;
    float _timer;

    // 조준 상태
    Vector3 _smoothedTarget;     // 플레이어를 지연 추적하는 타깃 위치
    Vector3 _aimDir = Vector3.forward; // 현재 조준 방향(정규화)

    // 라인 캐시(일직선)
    readonly List<Vector3> _line = new();

    // UI refs
    Transform _uiRoot;
    RectTransform _uiRT;
    Image _uiFill;

    // ───────── 외부에서 호출 ─────────
    public void Setup(BoardGrid board, Transform player, SurvivalDirector director)
    {
        _board = board; _player = player; _director = director;
        if (_director == null) _director = FindAnyObjectByType<SurvivalDirector>();

        var p = transform.position;
        p.y = BoardY + groundY;          // 기존: groundY
        transform.position = p;

        foreach (var c in GetComponentsInChildren<Collider>(true)) {
            if (c is MeshCollider mc) mc.convex = true;
            c.isTrigger = true;
        }
        _hp = Mathf.Max(1, maxHP);
        EnsureHPUI();
        UpdateHPUIFill();

        EnsureLine();
        SetLineActive(false);

        // 초기 조준 타깃
        _smoothedTarget = _player ? _player.position : transform.position + transform.forward;
_smoothedTarget.y = BoardY + groundY;  // 기존: groundY

        // 초기 에임 방향
        Vector3 initDir = (_smoothedTarget - transform.position); initDir.y = 0f;
        _aimDir = initDir.sqrMagnitude > 1e-6f ? initDir.normalized : Vector3.forward;

        _state = State.Aiming;
        _timer = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 타깃 위치 ‘관성’ 추적(속도 조절형 조준 핵심 ①)
        if (_player)
        {
           Vector3 desired = _player ? _player.position : _smoothedTarget;
desired.y = BoardY + groundY;    // 기존: groundY
            float follow = 1f - Mathf.Exp(-Mathf.Max(0.001f, aimTargetFollowSpeed) * dt);
            _smoothedTarget = Vector3.Lerp(_smoothedTarget, desired, follow);
        }

        // 에임 방향 ‘최대 회전 속도’ 제한(속도 조절형 조준 핵심 ②)
        {
            Vector3 toTgt = _smoothedTarget - transform.position; toTgt.y = 0f;
            if (toTgt.sqrMagnitude > 1e-6f)
            {
                var curRot = Quaternion.LookRotation(_aimDir.sqrMagnitude < 1e-6f ? Vector3.forward : _aimDir, Vector3.up);
                var dstRot = Quaternion.LookRotation(toTgt.normalized, Vector3.up);
                float maxStep = Mathf.Max(1f, aimAngularSpeedDeg) * dt;
                var next = Quaternion.RotateTowards(curRot, dstRot, maxStep);
                _aimDir = next * Vector3.forward;
            }
        }

        _timer += dt;

        if (_state == State.Aiming)
        {
            // 조준선(프리뷰) 갱신/표시
            BuildStraightLine(_line, preview: true);
            ApplyLine(_line);
            SetLineColor(previewColor);
            SetLineWidth(previewLineWidth);
            SetLineActive(true);

            if (_timer >= aimPreviewTime)
            {
                StartFire();
            }
        }
        else if (_state == State.Firing)
        {
            // 발사 표시 유지
            if (_timer >= fireShowDuration)
            {
                SetLineActive(false);
                _state = State.Cooldown;
                _timer = 0f;
            }
        }
        else // Cooldown
        {
            if (_timer >= cooldown)
            {
                _state = State.Aiming;
                _timer = 0f;
            }
        }

        UpdateHPUIPose();
    }

    // ───────── 발사 시작(오염 살포 + 히트 판정) ─────────
    void StartFire()
    {
        _timer = 0f;
        _state = State.Firing;

        // 최종 조준선 계산(현재 _aimDir 기준 일직선)
        BuildStraightLine(_line, preview: false);

        // 오염 퍼뜨리기(경로 따라 일정 간격 원형 살포)
        SpreadContaminationAlong(_line);

        // 플레이어 히트 판정 → 생존게이지 감소
        TryHitPlayerAlong(_line);

        // 발사 라인 표시
        ApplyLine(_line);
        SetLineColor(fireColor);
        SetLineWidth(fireLineWidth);
        SetLineActive(true);
    }

    // ───────── 직선 경로 구성 ─────────
    void BuildStraightLine(List<Vector3> outPts, bool preview)
    {
        outPts.Clear();

        Vector3 start = transform.position;
        start.y = BoardY + groundY + 0.02f;
        Vector3 dir = _aimDir.sqrMagnitude > 1e-6f ? _aimDir.normalized : Vector3.forward;

        // 보드 외곽 사각형에 부딪히거나, 최대 길이에서 종료
        Vector3 end = start + dir * Mathf.Max(1f, beamLength);

        if (_board)
        {
            Rect r = new Rect(_board.origin.x, _board.origin.z,
                              _board.width * _board.tileSize, _board.height * _board.tileSize);
            // 보드에 클램프
            if (!RayIntersectRect(start, dir, r, beamLength, out var hit, out _, out _))
                end = start + dir * Mathf.Max(1f, beamLength);
            else
                end = hit;
        }

        end.y = BoardY + groundY + 0.02f; 

        outPts.Add(start);
        outPts.Add(end);
    }

    // 보드 사각형과 레이 교차(가장 가까운 에지 히트)
    bool RayIntersectRect(Vector3 p, Vector3 d, Rect rect, float maxDist,
                          out Vector3 hit, out Vector3 reflectDir, out float traveled)
    {
        hit = Vector3.zero; reflectDir = d; traveled = 0f;
        float tMin = float.PositiveInfinity;

        // X면들
        if (Mathf.Abs(d.x) > 1e-6f)
        {
            float tL = (rect.xMin - p.x) / d.x; float zL = p.z + d.z * tL;
            if (tL > 1e-6f && tL <= maxDist && zL >= rect.yMin && zL <= rect.yMax && tL < tMin) { tMin = tL; }
            float tR = (rect.xMax - p.x) / d.x; float zR = p.z + d.z * tR;
            if (tR > 1e-6f && tR <= maxDist && zR >= rect.yMin && zR <= rect.yMax && tR < tMin) { tMin = tR; }
        }
        // Z면들
        if (Mathf.Abs(d.z) > 1e-6f)
        {
            float tB = (rect.yMin - p.z) / d.z; float xB = p.x + d.x * tB;
            if (tB > 1e-6f && tB <= maxDist && xB >= rect.xMin && xB <= rect.xMax && tB < tMin) { tMin = tB; }
            float tT = (rect.yMax - p.z) / d.z; float xT = p.x + d.x * tT;
            if (tT > 1e-6f && tT <= maxDist && xT >= rect.xMin && xT <= rect.xMax && tT < tMin) { tMin = tT; }
        }

        if (float.IsPositiveInfinity(tMin)) return false;

        traveled = tMin;
        hit = p + d * tMin;
        hit.y = BoardY + groundY + 0.02f;
        reflectDir = d;
        return true;
    }

    // ───────── 오염 살포 ─────────
    void SpreadContaminationAlong(List<Vector3> path)
    {
        if (_director == null || path.Count < 2) return;

       Vector3 a = path[0]; a.y = BoardY + groundY; // 기존: groundY
Vector3 b = path[1]; b.y = BoardY + groundY; // 기존: groundY

        float len = Vector3.Distance(a, b);
        float step = Mathf.Max(0.05f, contamStepMeters);
        float r = Mathf.Max(0.05f, contamRadiusMeters);

        // 시작~끝 구간을 일정 간격으로 샘플링
        int n = Mathf.Max(1, Mathf.CeilToInt(len / step));
        for (int i = 0; i <= n; i++)
        {
            float t = (n == 0) ? 0f : (float)i / n;
            Vector3 p = Vector3.Lerp(a, b, t);
            _director.ContaminateCircleWorld(p, r);
        }
    }

    // ───────── 플레이어 히트 판정 ─────────
    void TryHitPlayerAlong(List<Vector3> path)
    {
        if (path.Count < 2) return;

        Vector3 a = path[0];
        Vector3 b = path[1];
        Vector3 dir = (b - a);
        float dist = dir.magnitude;
        if (dist <= 1e-4f) return;
        dir /= dist;

        var hits = Physics.RaycastAll(a, dir, dist, hitLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        foreach (var h in hits)
        {
            // 플레이어면 생존게이지 감소
            ApplySurvivalDamageOnce(h.collider, Mathf.Abs(survivalDamageOnHit));
            // 관통 여부에 따라 break할지 선택 가능; 여기선 관통 허용(여러 히트 가능)
        }
    }

    void ApplySurvivalDamageOnce(Collider col, float amount)
    {
        // 1) SurvivalDirector에 전달
        try
        {
            if (_director != null)
            {
                var m = typeof(SurvivalDirector).GetMethod("ModifySurvival",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (m != null) m.Invoke(_director, new object[] { -Mathf.Abs(amount) });
            }
        }
        catch { /* ignore */ }

        // 2) 인터페이스 훅
        var d = col.GetComponentInParent<IPlayerSurvivalDamage>();
        if (d != null) d.ApplySurvivalDamage(Mathf.Abs(amount));
    }

    // ───────── 라인 유틸 ─────────
    void EnsureLine()
    {
        if (line) return;

        var go = new GameObject("SniperLine");
        go.transform.SetParent(transform, false);

        line = go.AddComponent<LineRenderer>();
        line.positionCount = 0;

        // 파이프라인 무관 기본 쉐이더
        var shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        line.material = mat;

        line.startColor = line.endColor = previewColor;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.textureMode = LineTextureMode.Stretch;
        line.sortingOrder = 1000;
    }

    void SetLineActive(bool on)
    {
        if (!line) return;
        line.enabled = on;
        if (!on) line.positionCount = 0;
    }

    void SetLineColor(Color c)
    {
        if (!line) return;
        line.startColor = line.endColor = c;
    }

    void SetLineWidth(float w)
    {
        if (!line) return;
        line.startWidth = line.endWidth = w;
    }

    void ApplyLine(List<Vector3> pts)
    {
        if (!line) return;
        if (pts.Count < 2) { line.positionCount = 0; return; }
        line.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++) line.SetPosition(i, pts[i]);
    }

    // ───────── HP / HIT / UI ─────────
    void OnTriggerEnter(Collider other)
    {
        if (hitByLayers.value == 0) { Hit(damagePerHit); return; }
        if ((hitByLayers.value & (1 << other.gameObject.layer)) != 0) Hit(damagePerHit);
    }

    void Hit(int dmg)
    {
        if (_hp <= 0) return;
        _hp = Mathf.Max(0, _hp - Mathf.Max(1, dmg));
        UpdateHPUIFill();
        if (_hp <= 0) { Die(); return; }
    }

    void Die()
    {
        SetLineActive(false);
        if (_uiRoot) Destroy(_uiRoot.gameObject);
        Destroy(gameObject);
    }

    // ─── HP UI 생성/갱신(막대형) ───
    void EnsureHPUI()
    {
        if (_uiRoot) return;

        if (hpUiMode == HpUiMode.ScreenTopRight)
        {
            var root = new GameObject("SniperHP_Screen",
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
            _uiRT.pivot = new Vector2(1, 1);
            _uiRT.anchoredPosition = new Vector2(-screenMarginTR.x, -screenMarginTR.y);

            BuildBar(_uiRT, hudInnerPadPx);
        }
        else
        {
            var root = new GameObject("SniperHP_World",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _uiRoot = root.transform;
            _uiRoot.SetParent(transform, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = hudSortingOrder;
            if (!canvas.worldCamera && Camera.main) canvas.worldCamera = Camera.main;

            _uiRT = (RectTransform)_uiRoot;
            _uiRT.sizeDelta = worldBarSizeM;
            _uiRT.localScale = Vector3.one * worldScale;

            _uiRoot.localRotation = (hpUiMode == HpUiMode.WorldFlat)
                ? Quaternion.Euler(worldFlatAngleDeg, 0f, 0f)
                : Quaternion.identity;

            BuildBarWorld(_uiRT, worldBarInnerPadM);
        }
    }

    void BuildBar(RectTransform parent, float innerPadPx)
    {
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        bgRT.SetParent(parent, false);
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

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

    void BuildBarWorld(RectTransform parent, float innerPadM)
    {
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        bgRT.SetParent(parent, false);
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

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
        if (_uiFill) _uiFill.fillAmount = Mathf.Clamp01((float)_hp / Mathf.Max(1, maxHP));
    }

    void UpdateHPUIPose()
    {
        if (!_uiRoot) return;
        if (hpUiMode == HpUiMode.ScreenTopRight) return;

        Vector3 head = transform.position + Vector3.up * Mathf.Max(worldLift, 0f);
        _uiRoot.position = head;

        if (hpUiMode == HpUiMode.WorldBillboard)
        {
            var cam = Camera.main;
            if (cam)
            {
                Vector3 fwd = cam.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                _uiRoot.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
        }
        else
        {
            _uiRoot.localRotation = Quaternion.Euler(worldFlatAngleDeg, 0f, 0f);
        }
    }
}
