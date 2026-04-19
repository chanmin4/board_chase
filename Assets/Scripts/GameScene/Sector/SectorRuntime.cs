using UnityEngine;

public class SectorRuntime : MonoBehaviour
{
    [Header("Ref Don't Touch")]
    [ReadOnly] public Vector2Int coord;
    [ReadOnly] public bool isOpened;
    [ReadOnly] public bool is_startsector = false;

    public Transform cameraPoint;
    public Transform[] enemySpawnPoints;

    public SectorEdge XMin;
    public SectorEdge XMax;
    public SectorEdge ZMin;
    public SectorEdge ZMax;

    [Header("Fallback Bounds")]
    [SerializeField] private Vector3 fallbackCenterOffset = Vector3.zero;
    [SerializeField] private Vector2 fallbackSizeXZ = new Vector2(10f, 10f);

    public Vector2Int Coord => coord;
    public bool IsOpened => isOpened;
    public bool IsStartSector => is_startsector;

    public void SetRuntimeInfo(Vector2Int newCoord, bool opened, bool isStartSector)
    {
        coord = newCoord;
        isOpened = opened;
        is_startsector = isStartSector;
    }

    public void SetOpened(bool opened)
    {
        isOpened = opened;
    }

    public Bounds GetWorldBounds()
    {
        if (HasAllSideBounds())
        {
            float minX = XMin.bound.bounds.center.x;
            float maxX = XMax.bound.bounds.center.x;
            float minZ = ZMin.bound.bounds.center.z;
            float maxZ = ZMax.bound.bounds.center.z;

            Vector3 center = new Vector3(
                (minX + maxX) * 0.5f,
                transform.position.y,
                (minZ + maxZ) * 0.5f
            );

            Vector3 size = new Vector3(
                Mathf.Abs(maxX - minX),
                5f,
                Mathf.Abs(maxZ - minZ)
            );

            return new Bounds(center, size);
        }

        Vector3 fallbackCenter = transform.TransformPoint(fallbackCenterOffset);
        Vector3 fallbackSize = new Vector3(fallbackSizeXZ.x, 5f, fallbackSizeXZ.y);
        return new Bounds(fallbackCenter, fallbackSize);
    }

    private bool HasAllSideBounds()
    {
        return XMin != null && XMin.bound != null &&
               XMax != null && XMax.bound != null &&
               ZMin != null && ZMin.bound != null &&
               ZMax != null && ZMax.bound != null;
    }
}
