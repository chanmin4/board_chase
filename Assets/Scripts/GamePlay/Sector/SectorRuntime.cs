using UnityEngine;
using System.Collections.Generic;
public class SectorRuntime : MonoBehaviour
{
    public Vector2Int coord;
    public bool isStartSector;
    public bool isOpened;

    public Transform cameraPoint;
    public Transform[] enemySpawnPoints;

    public SectorEdge XMin;
    public SectorEdge XMax;
    public SectorEdge ZMin;
    public SectorEdge ZMax;
    [Header("Fallback Bounds")]
    //bound 한개라도 존재x일시 fallback
    private Vector3 fallbackCenterOffset = Vector3.zero;
    private Vector2 fallbackSizeXZ = new Vector2(10f, 10f);
        /// <summary>
/// 좌/우/상/하 bound 4개가 모두 연결되어 있는지 확인한다.
/// </summary>
private bool HasAllSideBounds()
{
    return XMin.bound!= null &&
           XMax.bound != null &&
           ZMax.bound != null &&
           ZMin.bound != null;
}

    /// <summary>
    /// 이 섹터의 월드 bounds를 반환한다.
    /// 
    /// 우선순위:
    /// 1. Collider가 있으면 collider bounds 사용
    /// 2. 없으면 fallback size 사용
    /// </summary>
   /// <summary>
/// 이 섹터의 실제 내부 월드 bounds를 반환한다.
/// 
/// 우선순위:
/// 1. 좌/우/상/하 4개 bound가 모두 있으면 그 "안쪽 면" 기준으로 섹터 내부 영역 계산
/// 2. 없으면 fallback size 사용
/// </summary>
public Bounds GetWorldBounds()
{
    if (HasAllSideBounds())
    {
        // 좌측 벽의 안쪽 면 = leftBound.bounds.max.x
        float XMinx = XMin.bound.bounds.max.x;

        // 우측 벽의 안쪽 면 = rightBound.bounds.min.x
        float XMaxx = XMax.bound.bounds.min.x;

        // 아래 벽의 안쪽 면 = bottomBound.bounds.max.z
        float ZMinz = ZMin.bound.bounds.max.z;

        // 위 벽의 안쪽 면 = topBound.bounds.min.z
        float ZMaxz = ZMax.bound.bounds.min.z;

        // 혹시 인스펙터 연결이 반대로 됐어도 안전하게 보정
        if (XMinx > XMaxx)
        {
            float temp = XMinx;
            XMinx = XMaxx;
            XMaxx = temp;
        }

        if (ZMinz > ZMaxz)
        {
            float temp = ZMinz;
            ZMinz = ZMaxz;
            ZMaxz = temp;
        }

        Vector3 center = new Vector3(
            (XMinx + XMaxx) * 0.5f,
            transform.position.y,
            (ZMinz + ZMaxz)* 0.5f
        );

        Vector3 size = new Vector3(
            Mathf.Max(0.01f, XMaxx - XMinx),
            0.1f,
            Mathf.Max(0.01f, ZMaxz - ZMinz)
        );

        return new Bounds(center, size);
    }

    // fallback
    Vector3 fallbackCenter = transform.position + fallbackCenterOffset;
    Vector3 fallbackSize = new Vector3(fallbackSizeXZ.x, 0.1f, fallbackSizeXZ.y);
    return new Bounds(fallbackCenter, fallbackSize);
}
}