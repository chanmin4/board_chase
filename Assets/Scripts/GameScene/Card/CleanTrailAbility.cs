using UnityEngine;
using System.Collections;

/// 디스크가 지나간 경로를 청소하고(오염0) 같은/다른 반지름으로 플레이어 색을 칠함.
/// r(반지름) 튜닝을 위해 청소/페인트 반지름에 각각 Mul/Add 노브 제공.
public class CleanTrailAbility : CardAbility
{
    [Header("Radius Tuning (meters)")]
    [Tooltip("청소 반지름 배수 (1=원래 r)")]
    public float clearRadiusMul = 1f;
    [Tooltip("청소 반지름 추가/감소 값(미터, 음수 가능)")]
    public float clearRadiusAddWorld = 0f;

    [Tooltip("페인트 반지름 배수 (1=원래 r)")]
    public float paintRadiusMul = 0.5f;
    [Tooltip("페인트 반지름 추가/감소 값(미터, 음수 가능)")]
    public float paintRadiusAddWorld = 0f;

    [Header("Card Radius (tiles)")]
    [Tooltip("카드 데이터 추가 반지름(타일 단위)")]
    public float extraRadiusTilesOverride = -1f; // <0 이면 CardData.radiusTiles 사용

    SurvivalDirector director;
    Transform player;

    Collider diskCol;
    float extraRadiusTiles;         // 실제 사용값
    Coroutine co;

    public override void Activate(Transform playerTf, SurvivalDirector dir, CardData data)
    {
        StopNow();

        player   = playerTf;
        director = dir;

        extraRadiusTiles = (extraRadiusTilesOverride >= 0f) ? extraRadiusTilesOverride : Mathf.Max(0f, data.radiusTiles);

        if (!player || !director)
        {
            Debug.LogWarning("[CleanTrail] refs missing");
            return;
        }

        diskCol = player.GetComponent<Collider>();

        IsRunning = true;
        co = StartCoroutine(CleanLoop(data.duration));
    }

    IEnumerator CleanLoop(float duration)
    {
        float t = 0f;
        var wait = new WaitForSeconds(0.02f);

        while (t < duration && IsRunning)
        {
            if (director && player)
            {
                float addWorld = director.board ? director.board.tileSize * extraRadiusTiles : extraRadiusTiles;

                if (diskCol is CapsuleCollider cap)
                {
                    GetCapsuleWorld(cap, out Vector3 a, out Vector3 b, out float rad);
                    float rBase = rad + addWorld;

                    // 구간을 따라 연속 스탬프
                    float len  = Vector3.Distance(a, b);
                    float step = Mathf.Max(rBase * 0.6f, 0.01f);
                    int   n    = Mathf.Max(1, Mathf.CeilToInt(len / step));

                    for (int i = 0; i <= n; i++)
                    {
                        float u = (n == 0) ? 0f : (i / (float)n);
                        Vector3 p = Vector3.Lerp(a, b, u);

                        float rClear = Mathf.Max(0.01f, rBase * clearRadiusMul + clearRadiusAddWorld);
                        float rPaint = Mathf.Max(0.01f, rBase * paintRadiusMul + paintRadiusAddWorld);

                        // 1) 청소(오염 0)
                        director.ClearCircleWorld(p, rClear);

                        // 2) 플레이어 색 덮어쓰기(해당 영역 오염 0 유지)
                        director.PaintPlayerCircleWorld(p, rPaint, applyBoardClean: false, clearPollutionMask: true);
                    }
                }
                else if (diskCol is SphereCollider sph)
                {
                    float rBase = GetSphereRadiusWorld(sph) + addWorld;
                    float rClear = Mathf.Max(0.01f, rBase * clearRadiusMul + clearRadiusAddWorld);
                    float rPaint = Mathf.Max(0.01f, rBase * paintRadiusMul + paintRadiusAddWorld);

                    director.ClearCircleWorld(player.position, rClear);
                    director.PaintPlayerCircleWorld(player.position, rPaint, false, true);
                }
                else if (diskCol is BoxCollider box)
                {
                    Vector3 e = Vector3.Scale(box.size * 0.5f, player.lossyScale);
                    float rBase = Mathf.Sqrt(e.x * e.x + e.z * e.z) + addWorld;
                    float rClear = Mathf.Max(0.01f, rBase * clearRadiusMul + clearRadiusAddWorld);
                    float rPaint = Mathf.Max(0.01f, rBase * paintRadiusMul + paintRadiusAddWorld);

                    director.ClearCircleWorld(player.position, rClear);
                    director.PaintPlayerCircleWorld(player.position, rPaint, false, true);
                }
            }

            t += 0.02f;
            yield return wait;
        }

        StopNow();
    }

    public override void StopNow()
    {
        if (!IsRunning) return;
        IsRunning = false;
        if (co != null) StopCoroutine(co);
        co = null;
    }

    void OnDisable() => StopNow();

    // ===== Helpers =====
    static void GetCapsuleWorld(CapsuleCollider cap, out Vector3 worldA, out Vector3 worldB, out float worldRadius)
    {
        Vector3 cLocal    = cap.center;
        Vector3 axisLocal = cap.direction == 0 ? Vector3.right :
                            cap.direction == 1 ? Vector3.up    :
                            Vector3.forward;

        Vector3 s = cap.transform.lossyScale;

        // 프로젝트 기존 방식 그대로 유지(최대 축)
        float radiusScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        worldRadius = cap.radius * radiusScale;

        float axisScale = cap.direction == 0 ? Mathf.Abs(s.x) :
                          cap.direction == 1 ? Mathf.Abs(s.y) :
                                               Mathf.Abs(s.z);

        float worldHeight = Mathf.Max(0f, cap.height * axisScale - 2f * worldRadius);

        Vector3 aLocal = cLocal + axisLocal * (worldHeight * 0.5f / axisScale);
        Vector3 bLocal = cLocal - axisLocal * (worldHeight * 0.5f / axisScale);

        worldA = cap.transform.TransformPoint(aLocal);
        worldB = cap.transform.TransformPoint(bLocal);
    }

    static float GetSphereRadiusWorld(SphereCollider sph)
    {
        Vector3 s = sph.transform.lossyScale;
        float uniform = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        return sph.radius * uniform;
    }
}
