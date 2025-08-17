// DiskLauncher.cs (교체/수정)
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class DiskLauncher : MonoBehaviour
{
    [Header("힘→속도")]
    public float powerScale   = 0.16f;
    public float minStopSpeed = 0.25f;

    [Header("스냅 설정")]
    public LayerMask tileMask;              // ← Tile 레이어만 체크
    public float snapSearchRadius = 5f;   // ← 타일 탐색 반경
    public float snapYOffset = 0f;          // ← 센터에 살짝 높이 보정이 필요하면 사용

    Rigidbody rb;
    bool launched;
    public bool IsMoving => launched;

    public event Action<TileCenter> OnStoppedOnTile;

    void Awake(){ rb = GetComponent<Rigidbody>(); }

    public void Launch(Vector3 dir, float pull)
    {
        dir.y = 0f; dir.Normalize();
        rb.linearVelocity = dir * (pull * powerScale);
        launched = true;
    }

    void FixedUpdate()
    {
        if (!launched) return;

        if (rb.linearVelocity.magnitude < minStopSpeed)
        {
            launched = false;
            rb.linearVelocity = Vector3.zero;
            SnapAndReport();
        }
    }

    void SnapAndReport()
    {
        TileCenter best = FindNearestTileCenter();

        if (best != null)
        {
            var p = best.transform.position;
            transform.position = new Vector3(p.x, p.y + snapYOffset, p.z);  // ← 스냅!
            OnStoppedOnTile?.Invoke(best);
        }
        else
        {
            Debug.LogWarning("[DiskLauncher] 근처 TileCenter를 찾지 못했습니다. tileMask/Collider/Index 확인!");
            OnStoppedOnTile?.Invoke(null);
        }
    }

  TileCenter FindNearestTileCenter()
{
    // 1) 우선 충돌 기반 탐색
    var hits = Physics.OverlapSphere(
        transform.position,
        snapSearchRadius,
        tileMask,
        QueryTriggerInteraction.Collide
    );

    TileCenter best = null; float bestD = float.MaxValue;

    foreach (var h in hits)
    {
        var tc = h.GetComponent<TileCenter>() ?? h.GetComponentInChildren<TileCenter>() ?? h.GetComponentInParent<TileCenter>();
        if (!tc) continue;
        float d = (tc.transform.position - transform.position).sqrMagnitude;
        if (d < bestD) { bestD = d; best = tc; }
    }

    if (best != null) return best;

    // 2) 실패 시: 레지스트리에서 전수 검색 (레이어/콜라이더 무시)
    foreach (var tc in TileCenter.Registry)
    {
        if (!tc) continue;
        float d = (tc.transform.position - transform.position).sqrMagnitude;
        if (d < bestD) { bestD = d; best = tc; }
    }
    return best;
}

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapSearchRadius);
    }
#endif
}
