using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IPlayerSurvivalDamage
{
    void ApplySurvivalDamage(float amount);
}

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class PollutionSniper : MonoBehaviour
{
    // ───────── 주입되는 씬 레퍼런스(프리팹 저장 X) ─────────
    BoardGrid _board;
    Transform _player;
    SurvivalDirector _director;

    // ───────── HP / 히트 ─────────
    [Header("HP / Hit")]
    public int maxHP = 3;
    public int damagePerHit = 1;                  // 디스크 등 충돌 시 깎이는 양
    public LayerMask hitByLayers;                 // 보스에 데미지 주는 레이어
    public float bossRadius = 0.5f;
    public float groundY = 0.2f;

    // ───────── 공격 파라미터 ─────────
    [Header("Attack")]
    public float aimPreviewTime = 2.0f;           // 미리보기 시간(초)
    public float cooldown = 4.0f;                 // 다음 조준까지 대기
    public int   maxBounces = 3;                  // 튕김 횟수(최대 3 요청, 원하면 늘려도 됨)
    public float beamLength = 100f;               // 충분히 큰 직선 길이
    public float playerDamage = 1f;               // 맞으면 생존 게이지 깎는 양(양수면 감소)
    public LayerMask playerLayers;                // 플레이어 레이어

    [Tooltip("미리보기에서 '첫 튕김 전 구간만' 표시할지")]
    public bool previewOnlyFirstSegment = false;

    // ───────── 라인 렌더링(경로 표시) ─────────
    [Header("Line Preview")]
    public LineRenderer line;                     // 비워두면 런타임 생성
    public float lineWidth = 0.035f;
    public Color previewColor = new Color(0.2f, 1f, 0.6f, 0.9f);
    public Color fireColor    = new Color(1f, 0.2f, 0.2f, 1f);
    public GameObject bounceMarkerPrefab;         // 선택(없으면 안 그림)
    public float bounceMarkerScale = 0.1f;

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

    // ───────── 내부 상태 ─────────
    int _hp;
    enum State { IdleCooldown, Aiming, Firing }
    State _state;
    float _timer;

    // 경로 캐시
    readonly List<Vector3> _points = new();
    readonly List<Transform> _tempMarkers = new();

    // UI refs
    Transform _uiRoot;
    RectTransform _uiRT;
    Image _uiFill;

    // ───────── 외부에서 호출 ─────────
    public void Setup(BoardGrid board, Transform player, SurvivalDirector director)
    {
        _board = board;
        _player = player;
        _director = director;

        var p = transform.position; p.y = groundY; transform.position = p;

        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = Mathf.Max(0.05f, bossRadius);

        _hp = Mathf.Max(1, maxHP);
        EnsureHPUI();
        UpdateHPUIFill();

        EnsureLine();
        SetLineActive(false);

        _state = State.Aiming; // 첫 상태: 조준 시작
        _timer = 0f;
    }

    void Update()
    {
        _timer += Time.deltaTime;

        switch (_state)
        {
            case State.Aiming:
                // 경로 갱신(미리보기)
                BuildPathPreview();

                if (_timer >= aimPreviewTime)
                {
                    _timer = 0f;
                    SetLineColor(fireColor);
                    Fire(); // 데미지 판정도 즉시 수행
                    _state = State.IdleCooldown;
                }
                break;

            case State.IdleCooldown:
                // 라인 끄고 쿨다운
                SetLineActive(false);
                if (_timer >= cooldown)
                {
                    _timer = 0f;
                    _state = State.Aiming;
                }
                break;

            case State.Firing:
                // (만약 발사 연출을 길게 주면 여기에 유지 로직)
                _state = State.IdleCooldown;
                break;
        }

        UpdateHPUIPose();
    }

    // ───────── 경로 계산(사각 보드에서 반사) ─────────
    void BuildPathPreview()
    {
        if (!_board) return;

        _points.Clear();
        Vector3 start = transform.position;
        Vector3 dir = GetInitialDir();
        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.right;
        dir.y = 0f; dir.Normalize();

        _points.Add(start);

        Rect r = new Rect(_board.origin.x, _board.origin.z,
                          _board.width * _board.tileSize, _board.height * _board.tileSize);

        Vector3 curPos = start;
        Vector3 curDir = dir;

        int bounces = Mathf.Max(0, maxBounces);
        float remaining = beamLength;

        for (int i = 0; i <= bounces; i++)
        {
            // 세그먼트마다 보드 외곽과의 교차점 계산
            if (!RayIntersectRect(curPos, curDir, r, remaining, out var hitPoint, out var reflected, out float dist))
            {
                // 더 이상 보드 벽에 닿지 않으면 straight
                _points.Add(curPos + curDir * remaining);
                break;
            }

            _points.Add(hitPoint);

            remaining -= dist;
            if (i == bounces) break;

            // 반사 시작점 약간 안쪽으로 밀기(부동소수 재교차 방지)
            curPos = hitPoint + reflected * 0.001f;
            curDir = reflected;
        }

        // 프리뷰 표시 범위: 전체 or 첫 튕김 전까지
        int countToShow = _points.Count;
        if (previewOnlyFirstSegment && _points.Count >= 2) countToShow = 2;

        SetLineActive(true);
        SetLineColor(previewColor);
        ApplyLine(_points, countToShow);

        // 바운스 마커
        RefreshMarkers(countToShow);
    }

    // 초기 방향: 플레이어 쪽으로(수평)
    Vector3 GetInitialDir()
    {
        if (_player)
        {
            Vector3 v = _player.position - transform.position;
            v.y = 0f;
            if (v.sqrMagnitude > 1e-6f) return v.normalized;
        }
        return Vector3.right;
    }

    // 보드 사각형과 레이 교차 → 최근접 에지 hit 및 반사 방향
    bool RayIntersectRect(Vector3 p, Vector3 d, Rect rect, float maxDist,
                          out Vector3 hit, out Vector3 reflectDir, out float traveled)
    {
        hit = Vector3.zero; reflectDir = d; traveled = 0f;

        // 수직선/수평선 네 면과의 t 구하기
        float tMin = float.PositiveInfinity;
        Vector3 normal = Vector3.zero;

        // X면
        if (Mathf.Abs(d.x) > 1e-6f)
        {
            // 좌
            float tL = (rect.xMin - p.x) / d.x;
            float zL = p.z + d.z * tL;
            if (tL > 1e-6f && tL <= maxDist && zL >= rect.yMin && zL <= rect.yMax && tL < tMin)
            { tMin = tL; normal = Vector3.left; }
            // 우
            float tR = (rect.xMax - p.x) / d.x;
            float zR = p.z + d.z * tR;
            if (tR > 1e-6f && tR <= maxDist && zR >= rect.yMin && zR <= rect.yMax && tR < tMin)
            { tMin = tR; normal = Vector3.right; }
        }
        // Z면
        if (Mathf.Abs(d.z) > 1e-6f)
        {
            // 아래
            float tB = (rect.yMin - p.z) / d.z;
            float xB = p.x + d.x * tB;
            if (tB > 1e-6f && tB <= maxDist && xB >= rect.xMin && xB <= rect.xMax && tB < tMin)
            { tMin = tB; normal = Vector3.back; }
            // 위
            float tT = (rect.yMax - p.z) / d.z;
            float xT = p.x + d.x * tT;
            if (tT > 1e-6f && tT <= maxDist && xT >= rect.xMin && xT <= rect.xMax && tT < tMin)
            { tMin = tT; normal = Vector3.forward; }
        }

        if (float.IsPositiveInfinity(tMin))
            return false;

        traveled = tMin;
        hit = p + d * tMin;
        reflectDir = Vector3.Reflect(d, normal);
        reflectDir.y = 0f; reflectDir.Normalize();
        hit.y = groundY;
        return true;
        }

    // ───────── 발사(데미지 판정) ─────────
    void Fire()
    {
        // 발사 순간엔 전체 경로를 그려주고(색상 바꿈), 라인 킵(연출 원하면 유지시간 추가 가능)
        BuildPathPreview();
        SetLineColor(fireColor);
        SetLineActive(true);
        ApplyDamageAlongPath();
    }

    void ApplyDamageAlongPath()
    {
        if (_points.Count < 2) return;

        // 세그먼트마다 레이캐스트 진행
        for (int i = 0; i < _points.Count - 1; i++)
        {
            Vector3 a = _points[i];
            Vector3 b = _points[i + 1];
            Vector3 dir = (b - a);
            float dist = dir.magnitude;
            if (dist <= 1e-4f) continue;
            dir /= dist;

            var hits = Physics.RaycastAll(a + Vector3.up * 0.01f, dir, dist, playerLayers, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                foreach (var h in hits)
                {
                    // 플레이어 쪽으로 데미지 전달 시도
                    TryDamagePlayer(h.collider);
                }
            }
        }
    }

    void TryDamagePlayer(Collider col)
    {
        // 1) SurvivalDirector가 있으면 감소 시도(메서드명이 다를 수 있으니 try/catch)
        try
        {
            if (_director != null) {
                // 프로젝트에 맞게 바꿔도 됨(예: director.ModifySurvival(-playerDamage))
                var m = typeof(SurvivalDirector).GetMethod("ModifySurvival",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (m != null) m.Invoke(_director, new object[] { -Mathf.Abs(playerDamage) });
            }
        }
        catch { /* 무시 */ }

        // 2) 맞은 콜라이더 계층에서 인터페이스 찾기
        var d = col.GetComponentInParent<IPlayerSurvivalDamage>();
        if (d != null) d.ApplySurvivalDamage(Mathf.Abs(playerDamage));
    }

    // ───────── 라인 표시 유틸 ─────────
    void EnsureLine()
    {
        if (line) return;
        var go = new GameObject("SniperLine");
        go.transform.SetParent(transform, false);
        line = go.AddComponent<LineRenderer>();
        line.positionCount = 0;
        line.startWidth = line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        line.material.SetColor("_BaseColor", previewColor);
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
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
        if (line.material && line.material.HasProperty("_BaseColor"))
            line.material.SetColor("_BaseColor", c);
    }

    void ApplyLine(List<Vector3> pts, int countToShow)
    {
        if (!line) return;
        int n = Mathf.Clamp(countToShow, 0, pts.Count);
        line.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            var p = pts[i]; p.y = groundY + 0.02f;
            line.SetPosition(i, p);
        }
        line.startWidth = line.endWidth = lineWidth;
    }

    void RefreshMarkers(int countToShow)
    {
        // 기존 마커 파괴
        foreach (var t in _tempMarkers) if (t) Destroy(t.gameObject);
        _tempMarkers.Clear();

        if (!bounceMarkerPrefab) return;
        // segment 끝점들 중 시작점 제외, 마지막 점 포함(튕김 지점 표시)
        for (int i = 1; i < countToShow; i++)
        {
            var m = Instantiate(bounceMarkerPrefab, _points[i] + Vector3.up * 0.02f, Quaternion.identity, transform);
            m.transform.localScale = Vector3.one * bounceMarkerScale;
            _tempMarkers.Add(m.transform);
        }
    }

    // ───────── HP / HIT / UI ─────────
    void OnTriggerEnter(Collider other)
    {
        if (hitByLayers.value == 0)
        {
            Hit(damagePerHit); // 디버그 편의
            return;
        }
        if ((hitByLayers.value & (1 << other.gameObject.layer)) != 0)
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
    }

    void Die()
    {
        SetLineActive(false);
        foreach (var t in _tempMarkers) if (t) Destroy(t.gameObject);
        _tempMarkers.Clear();
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
