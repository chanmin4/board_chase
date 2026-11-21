/*
 * Role hierarchy: Gameplay > World > Sectors
 * Primary responsibility: Manages SectorRuntime stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

// Assets/Scripts/GameScene/Sector/SectorRuntime.cs
using System.Collections.Generic;
using UnityEngine;

public class SectorRuntime : MonoBehaviour
{
    [HideInInspector] public float AuthoredSpaceMultiplier=1f;
    private const string WorldItemContainerName = "WorldItemContainer";

    [Header("NeedRef Roots")]
    [SerializeField] private Transform _PatternObjectRoot;

    [Header("Runtime Anchors")]
    [Tooltip("Player spawn/reposition point used when this sector becomes the stage start sector. Falls back to cameraPoint, then this transform.")]
    [SerializeField] private Transform _playerStartPoint;

    [Header("Enemy Spawn")]
    [Tooltip("Parent containing the hand-authored EnemySpawnPoint markers for this sector. SectorEnemySpawner randomly selects only enabled markers below this parent.")]
    [SerializeField] private Transform _enemySpawnPointRoot;

    [Header("Runtime Room Policy")]
    [Tooltip("Resolved from the selected SectorPresetSO when this Normal sector is generated. Empty means the historical Required Encounter + Clear Encounter behaviour.")]
    [ReadOnly] [SerializeField] private NormalSectorPolicySO _normalSectorPolicy;

    // This is intentionally resolved from the authored field hierarchy instead
    // of serialized per sector. Every sector variant inherits the same
    // Container/WorldItemContainer child from Sector_Base.
    private Transform _worldItemContainer;
    private SectorMapIconKind _mapIconKind;

    [Header("Ref Don't Touch")]
    [ReadOnly] public Vector2Int coord;
    [ReadOnly] public bool isOpened;
    [ReadOnly] public bool isCleared;
    [ReadOnly] public bool is_startsector = false;

    public Transform cameraPoint;

    public SectorEdge XMin;
    public SectorEdge XMax;
    public SectorEdge ZMin;
    public SectorEdge ZMax;

    [Header("Fallback Bounds")]
    [SerializeField] private Vector3 fallbackCenterOffset = Vector3.zero;
    [SerializeField] private Vector2 fallbackSizeXZ = new Vector2(10f, 10f);

    public Vector2Int Coord => coord;
    public SectorMapIconKind MapIconKind => _mapIconKind;
    public void ConfigureMapIconKind(SectorMapIconKind kind) => _mapIconKind = kind;
    public bool IsOpened => isOpened;
    public bool IsCleared => isCleared;
    public bool IsStartSector => is_startsector;
    public Transform EnemySpawnPointRoot => _enemySpawnPointRoot;
    public Transform PatternObjectRoot => _PatternObjectRoot != null ? _PatternObjectRoot : transform;
    public NormalSectorPolicySO NormalSectorPolicy => _normalSectorPolicy;
    public bool UsesNormalSectorEnemyEncounter =>
        (InternalCombatArea != null && InternalCombatArea.IsBound) ||
        NormalSectorPolicySO.ResolveUsesEnemyEncounter(_normalSectorPolicy);
    public bool RequiresNormalSectorEnemyClearForProgression =>
        (InternalCombatArea != null && InternalCombatArea.IsBound) ||
        NormalSectorPolicySO.ResolveRequiresEnemyClearForProgression(_normalSectorPolicy);
    public bool AllowsPortalTravelBeforeClear =>
        InternalCombatArea != null && InternalCombatArea.IsBound
            ? InternalCombatArea.AllowsPerimeterTravel
            : NormalSectorPolicySO.ResolveAllowsPortalTravelBeforeClear(_normalSectorPolicy);
    private SectorLockedCombatArea _internalCombatArea;
    public SectorLockedCombatArea InternalCombatArea => _internalCombatArea != null
        ? _internalCombatArea : (_internalCombatArea = SectorLockedCombatArea.Find(this));
    public bool UsesNormalSectorStageDefaultTimer =>
        NormalSectorPolicySO.ResolveUsesStageDefaultTimer(_normalSectorPolicy);

    /// <summary>
    /// Runtime parent for floor items belonging to this field. It is resolved
    /// from the authored child named WorldItemContainer under this sector's
    /// Container hierarchy, so HomeSector and generated stage sectors each
    /// keep their own drops without individual inspector wiring.
    /// </summary>
    public Transform WorldItemContainer
    {
        get
        {
            if (_worldItemContainer != null &&
                _worldItemContainer.IsChildOf(transform))
            {
                return _worldItemContainer;
            }

            Transform[] candidates = GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < candidates.Length; i++)
            {
                Transform candidate = candidates[i];

                if (candidate != null && candidate.name == WorldItemContainerName)
                {
                    _worldItemContainer = candidate;
                    return _worldItemContainer;
                }
            }

            return null;
        }
    }

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
    public void ConfigurePlayerStartPoint(Transform point) { if(point!=null)_playerStartPoint=point; }

    // Purpose: Updates the runtime info state.
    public void SetRuntimeInfo(Vector2Int newCoord, bool opened, bool isStartSector)
    {
        coord = newCoord;
        isOpened = opened;
        isCleared = false;
        is_startsector = isStartSector;
    }

    /// <summary>
    /// Runtime generation applies the policy selected by the SectorPresetSO.
    /// This intentionally does not alter the authored prefab asset.
    /// </summary>
    public void ConfigureNormalSectorPolicy(NormalSectorPolicySO policy)
    {
        _normalSectorPolicy = policy;
    }

    // Purpose: Updates the opened state.
    public void SetOpened(bool opened)
    {
        isOpened = opened;
    }

    // Purpose: Updates the cleared state.
    public void SetCleared(bool cleared)
    {
        isCleared = cleared;
    }

    // Purpose: Updates the enemy spawn point root state.
    public bool SetEnemySpawnPointRoot(Transform root)
    {
        if (root != null && root != transform && !root.IsChildOf(transform))
        {
            Debug.LogError(
                $"[SectorRuntime] Enemy spawn point root must belong to this sector. sector={name}, root={root.name}",
                this);
            return false;
        }

        _enemySpawnPointRoot = root;
        return true;
    }

    // Purpose: Collects enemy spawn points for this workflow.
    public void CollectEnemySpawnPoints(List<Transform> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (_enemySpawnPointRoot == null)
            return;

        EnemySpawnPoint[] points =
            _enemySpawnPointRoot.GetComponentsInChildren<EnemySpawnPoint>(true);

        for (int i = 0; i < points.Length; i++)
        {
            EnemySpawnPoint point = points[i];

            if (point != null && point.EnabledForSpawning)
                results.Add(point.transform);
        }
    }

    /// <summary>
    /// Returns enabled authored content anchors below this sector. This is a
    /// data-only bridge for the later deterministic content runner; calling it
    /// never creates, removes or configures world objects.
    /// </summary>
    public void CollectContentSlotAnchors(List<SectorContentSlotAnchor> results)
    {
        if (results == null)
            return;

        results.Clear();

        SectorContentSlotAnchor[] anchors =
            GetComponentsInChildren<SectorContentSlotAnchor>(true);

        for (int i = 0; i < anchors.Length; i++)
        {
            SectorContentSlotAnchor anchor = anchors[i];
            if (anchor != null &&
                anchor.EnabledForRuntime &&
                !string.IsNullOrWhiteSpace(anchor.SlotId))
            {
                results.Add(anchor);
            }
        }
    }

    // Purpose: Returns the current world bounds.
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

    // Purpose: Checks whether this component has all side bounds.
    private bool HasAllSideBounds()
    {
        return XMin != null && XMin.bound != null &&
               XMax != null && XMax.bound != null &&
               ZMin != null && ZMin.bound != null &&
               ZMax != null && ZMax.bound != null;
    }

}
