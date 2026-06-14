// Assets/Scripts/GameScene/Sector/SectorRuntime.cs
using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SectorRuntime : MonoBehaviour
{
    [Header("NeedRef Roots")]
    [SerializeField] private Transform _PatternObjectRoot;
    [SerializeField] private Transform[] _cleanupRoots;

    [Header("Runtime Anchors")]
    [Tooltip("Player spawn/reposition point used when this sector becomes the stage start sector. Falls back to cameraPoint, then this transform.")]
    [SerializeField] private Transform _playerStartPoint;

    [Header("Ref Don't Touch")]
    [ReadOnly] public Vector2Int coord;
    [ReadOnly] public bool isOpened;
    [ReadOnly] public bool isCleared;
    [ReadOnly] public bool is_startsector = false;

    public Transform cameraPoint;

    [Tooltip("Generic spawn positions used by enemies and sector objects.")]
    [FormerlySerializedAs("enemySpawnPoints")]
    public Transform[] objectSpawnPoints;

    [Tooltip("Optional generic spawn metadata. Can be empty when objectSpawnPoints are enough.")]
    [FormerlySerializedAs("_spawnPointMetadata")]
    [SerializeField] private SectorObjectSpawnPoint[] _objectSpawnPointMetadata;

    public SectorEdge XMin;
    public SectorEdge XMax;
    public SectorEdge ZMin;
    public SectorEdge ZMax;

    [Header("Fallback Bounds")]
    [SerializeField] private Vector3 fallbackCenterOffset = Vector3.zero;
    [SerializeField] private Vector2 fallbackSizeXZ = new Vector2(10f, 10f);

    public Vector2Int Coord => coord;
    public bool IsOpened => isOpened;
    public bool IsCleared => isCleared;
    public bool IsStartSector => is_startsector;
    public Transform[] ObjectSpawnPoints => objectSpawnPoints;
    public SectorObjectSpawnPoint[] ObjectSpawnPointMetadata => _objectSpawnPointMetadata;
    [Obsolete("Use ObjectSpawnPoints.")]
    public Transform[] enemySpawnPoints => objectSpawnPoints;
    [Obsolete("Use ObjectSpawnPointMetadata.")]
    public SectorObjectSpawnPoint[] SpawnPointMetadata => _objectSpawnPointMetadata;
    public Transform PatternObjectRoot => _PatternObjectRoot != null ? _PatternObjectRoot : transform;
    public Transform[] CleanupRoots => _cleanupRoots;

    public Transform PlayerStartPoint
    {
        get
        {
            if (_playerStartPoint != null)
                return _playerStartPoint;

            if (cameraPoint != null)
                return cameraPoint;

            return transform;
        }
    }

    public void SetRuntimeInfo(Vector2Int newCoord, bool opened, bool isStartSector)
    {
        coord = newCoord;
        isOpened = opened;
        isCleared = false;
        is_startsector = isStartSector;
    }

    public void SetOpened(bool opened)
    {
        isOpened = opened;
    }

    public void SetCleared(bool cleared)
    {
        isCleared = cleared;
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
                (minZ + maxZ) * 0.5f);

            Vector3 size = new Vector3(
                Mathf.Abs(maxX - minX),
                5f,
                Mathf.Abs(maxZ - minZ));

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if ((_objectSpawnPointMetadata == null || _objectSpawnPointMetadata.Length == 0) &&
            objectSpawnPoints != null &&
            objectSpawnPoints.Length > 0)
        {
            SectorObjectSpawnPoint[] found = new SectorObjectSpawnPoint[objectSpawnPoints.Length];

            for (int i = 0; i < objectSpawnPoints.Length; i++)
            {
                Transform point = objectSpawnPoints[i];
                found[i] = point != null ? point.GetComponent<SectorObjectSpawnPoint>() : null;
            }

            _objectSpawnPointMetadata = found;
        }
    }
#endif
}
