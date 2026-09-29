/*
 * Role hierarchy: Presentation > UI > Gameplay > Map > MiniMap
 * Primary responsibility: Manages SectorTacticalMiniMapGraphic stage, sector, navigation, spawn, or spatial progression state.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SectorTacticalMiniMapGraphic : MaskableGraphic
{
    [Header("Refresh")]
    [SerializeField, Min(0.02f)] private float _scanInterval = 0.15f;
    [SerializeField, Min(0f)] private float _innerPadding = 15.6f;

    [Header("Colors")]
    [SerializeField] private Color _mapBackground = new(0.015f, 0.04f, 0.055f, 0.82f);
    [SerializeField] private Color _mapBorder = new(0.05f, 0.85f, 1f, 0.9f);
    [SerializeField] private Color _structureColor = new(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color _closeVisionColor = new(0.35f, 0.8f, 1f, 0.18f);
    [SerializeField] private Color _forwardVisionColor = new(1f, 0.92f, 0.45f, 0.22f);
    [SerializeField] private Color _playerColor = new(0.1f, 0.95f, 1f, 1f);
    [SerializeField] private Color _enemyColor = new(1f, 0.2f, 0.15f, 1f);
    [SerializeField] private Color _discoveredLootColor = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _portalColor = new(0.2f, 1f, 0.55f, 1f);
    [SerializeField] private Color _extractionPortalColor = new(0.1f, 1f, 0.35f, 1f);

    private readonly List<Collider> _structures = new();
    private readonly List<VisionVisibilityTarget> _enemies = new();
    private readonly List<VisionDiscoveredVisibilityTarget> _discoveries = new();
    private readonly List<SectorPortal> _portals = new();

    private SectorStateManager _stateManager;
    private SectorRuntime _sector;
    private EntityVisionController _viewer;
    private Transform _player;
    private Bounds _worldBounds;
    private float _nextScanTime;
    private int _structureLayer = -2;

    // Purpose: Configures this component''s configure.
    public void Configure(SectorStateManager stateManager)
    {
        _stateManager = stateManager;
        Scan(force: true);
    }

    // Purpose: Processes one update step for tick.
    public void Tick()
    {
        if (_stateManager == null)
            _stateManager = FindAnyObjectByType<SectorStateManager>();

        if (Time.unscaledTime >= _nextScanTime ||
            (_stateManager != null && _stateManager.CurrentSector != _sector))
        {
            Scan(force: false);
        }

        SetVerticesDirty();
    }

    // Purpose: Responds to the populate mesh callback.
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect fullRect = GetPixelAdjustedRect();
        AddQuad(vh, fullRect, _mapBackground);
        AddRectOutline(vh, fullRect, 2f, _mapBorder);

        if (_sector == null)
            return;

        Rect mapRect = Inset(fullRect, Mathf.Max(0f, _innerPadding));
        DrawVision(vh, mapRect);
        DrawStructures(vh, mapRect);
        DrawDiscoveredLoot(vh, mapRect);
        DrawVisibleEnemies(vh, mapRect);
        DrawPlayer(vh, mapRect);
        DrawAvailablePortals(vh, fullRect, mapRect);
    }

    // Purpose: Performs the scan workflow for this component.
    private void Scan(bool force)
    {
        _nextScanTime = Time.unscaledTime + Mathf.Max(0.02f, _scanInterval);
        SectorRuntime current = _stateManager != null ? _stateManager.CurrentSector : null;

        if (!force && current == _sector)
        {
            ResolveViewer();
            CacheDynamicTargets();
            return;
        }

        _sector = current;
        _structures.Clear();
        _portals.Clear();
        ResolveViewer();
        CacheDynamicTargets();

        if (_sector == null)
            return;

        _worldBounds = _sector.GetWorldBounds();

        if (_structureLayer == -2)
            _structureLayer = LayerMask.NameToLayer("SectorStructure");

        Collider[] colliders = _sector.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];

            if (candidate == null || !candidate.enabled || candidate.isTrigger)
                continue;

            if (_structureLayer >= 0 && candidate.gameObject.layer != _structureLayer)
                continue;

            if (candidate.GetComponentInParent<SectorPortal>() != null ||
                candidate.GetComponentInParent<Damageable>() != null ||
                candidate.GetComponentInParent<WorldItemPickup>() != null ||
                candidate.GetComponentInParent<SectorTreasure>() != null)
            {
                continue;
            }

            _structures.Add(candidate);
        }

        CachePortal(_sector.XMin);
        CachePortal(_sector.XMax);
        CachePortal(_sector.ZMin);
        CachePortal(_sector.ZMax);
    }

    // Purpose: Resolves the viewer needed by this component.
    private void ResolveViewer()
    {
        _viewer = VisionVisibilitySystem.Instance != null
            ? VisionVisibilitySystem.Instance.Viewer
            : null;

        if (_viewer == null)
            return;

        PlayerCharacter playerCharacter = _viewer.GetComponentInParent<PlayerCharacter>();
        _player = playerCharacter != null ? playerCharacter.transform : _viewer.transform;
    }

    // Purpose: Caches dynamic targets for later reuse.
    private void CacheDynamicTargets()
    {
        _enemies.Clear();
        _discoveries.Clear();

        if (_sector == null)
            return;

        VisionVisibilityTarget[] visibilityTargets =
            _sector.GetComponentsInChildren<VisionVisibilityTarget>(true);

        for (int i = 0; i < visibilityTargets.Length; i++)
        {
            VisionVisibilityTarget target = visibilityTargets[i];

            if (target != null && target.Enemy != null)
                _enemies.Add(target);
        }

        _discoveries.AddRange(
            _sector.GetComponentsInChildren<VisionDiscoveredVisibilityTarget>(true));
    }

    // Purpose: Caches portal for later reuse.
    private void CachePortal(SectorEdge edge)
    {
        if (edge == null || edge.portal == null)
            return;

        SectorPortal portal = edge.portal.GetComponentInChildren<SectorPortal>(true);

        if (portal != null)
            _portals.Add(portal);
    }

    // Purpose: Draws the vision authoring UI.
    private void DrawVision(VertexHelper vh, Rect mapRect)
    {
        if (_viewer == null || _player == null)
            return;

        Vector3 origin = _viewer.VisualOrigin.position;
        DrawClippedFan(vh, mapRect, origin, Vector3.forward, 360f,
            _viewer.CloseVisionRadius, 40, _closeVisionColor);

        if (_viewer.UsesForwardArc)
        {
            DrawClippedFan(vh, mapRect, origin, _viewer.ForwardDirection,
                _viewer.ForwardArcAngle, _viewer.VisionRange, 40, _forwardVisionColor);
        }
    }

    // Purpose: Draws the clipped fan authoring UI.
    private void DrawClippedFan(
        VertexHelper vh,
        Rect mapRect,
        Vector3 origin,
        Vector3 forward,
        float angle,
        float range,
        int segments,
        Color color)
    {
        if (range <= 0f || segments < 2)
            return;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();
        Vector2 center = WorldToMap(origin, mapRect);
        Vector2 previous = default;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float yaw = angle >= 359.9f
                ? t * 360f
                : Mathf.Lerp(-angle * 0.5f, angle * 0.5f, t);
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * forward;
            float visibleDistance = _viewer.GetVisibleDistanceFrom(
                _viewer.EyePosition,
                direction,
                range);
            Vector2 current = WorldToMap(origin + direction * visibleDistance, mapRect);

            if (i > 0)
                AddTriangle(vh, center, previous, current, color);

            previous = current;
        }
    }

    // Purpose: Draws the structures authoring UI.
    private void DrawStructures(VertexHelper vh, Rect mapRect)
    {
        for (int i = 0; i < _structures.Count; i++)
        {
            Collider structure = _structures[i];

            if (structure == null || !structure.enabled)
                continue;

            if (structure is BoxCollider box)
            {
                Vector3 half = box.size * 0.5f;
                Vector3 center = box.center;
                Vector2 a = WorldToMap(
                    box.transform.TransformPoint(center + new Vector3(-half.x, 0f, -half.z)),
                    mapRect);
                Vector2 b = WorldToMap(
                    box.transform.TransformPoint(center + new Vector3(-half.x, 0f, half.z)),
                    mapRect);
                Vector2 c = WorldToMap(
                    box.transform.TransformPoint(center + new Vector3(half.x, 0f, half.z)),
                    mapRect);
                Vector2 d = WorldToMap(
                    box.transform.TransformPoint(center + new Vector3(half.x, 0f, -half.z)),
                    mapRect);
                AddQuad(vh, a, b, c, d, _structureColor);
                continue;
            }

            Bounds bounds = structure.bounds;
            Vector2 min = WorldToMap(bounds.min, mapRect);
            Vector2 max = WorldToMap(bounds.max, mapRect);
            AddQuad(vh, Rect.MinMaxRect(
                Mathf.Min(min.x, max.x),
                Mathf.Min(min.y, max.y),
                Mathf.Max(min.x, max.x),
                Mathf.Max(min.y, max.y)), _structureColor);
        }
    }

    // Purpose: Draws the visible enemies authoring UI.
    private void DrawVisibleEnemies(VertexHelper vh, Rect mapRect)
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            VisionVisibilityTarget target = _enemies[i];

            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                !target.IsVisible ||
                (target.Damageable != null && target.Damageable.IsDead))
            {
                continue;
            }

            AddCircle(vh, WorldToMap(target.VisibilityPoint, mapRect), 4f, 12, _enemyColor);
        }
    }

    // Purpose: Draws the discovered loot authoring UI.
    private void DrawDiscoveredLoot(VertexHelper vh, Rect mapRect)
    {
        for (int i = 0; i < _discoveries.Count; i++)
        {
            VisionDiscoveredVisibilityTarget target = _discoveries[i];

            if (target == null || !target.gameObject.activeInHierarchy || !target.IsDiscovered)
                continue;

            AddDiamond(vh, WorldToMap(target.VisibilityPoint, mapRect), 4f, _discoveredLootColor);
        }
    }

    // Purpose: Draws the player authoring UI.
    private void DrawPlayer(VertexHelper vh, Rect mapRect)
    {
        if (_player == null)
            return;

        Vector3 worldForward = _viewer != null ? _viewer.ForwardDirection : _player.forward;
        Vector2 direction = new(worldForward.x, worldForward.z);

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector2.up;
        else
            direction.Normalize();

        Vector2 right = new(direction.y, -direction.x);
        Vector2 center = WorldToMap(_player.position, mapRect);
        AddTriangle(vh,
            center + direction * 7f,
            center - direction * 4f - right * 4.5f,
            center - direction * 4f + right * 4.5f,
            _playerColor);
    }

    // Purpose: Draws the available portals authoring UI.
    private void DrawAvailablePortals(VertexHelper vh, Rect fullRect, Rect mapRect)
    {
        for (int i = 0; i < _portals.Count; i++)
        {
            SectorPortal portal = _portals[i];

            if (portal == null || (!portal.IsAvailable && !portal.IsExtraction))
                continue;

            float inset = Mathf.Max(4f, _innerPadding * 0.55f);
            Vector2 center;
            Vector2 outward;

            switch (portal.Direction)
            {
                case SectorPortalDirection.XMin:
                    center = new Vector2(fullRect.xMin + inset, mapRect.center.y);
                    outward = Vector2.left;
                    break;
                case SectorPortalDirection.XMax:
                    center = new Vector2(fullRect.xMax - inset, mapRect.center.y);
                    outward = Vector2.right;
                    break;
                case SectorPortalDirection.ZMin:
                    center = new Vector2(mapRect.center.x, fullRect.yMin + inset);
                    outward = Vector2.down;
                    break;
                default:
                    center = new Vector2(mapRect.center.x, fullRect.yMax - inset);
                    outward = Vector2.up;
                    break;
            }

            Vector2 side = new(-outward.y, outward.x);
            Color color = portal.IsExtraction ? _extractionPortalColor : _portalColor;
            AddTriangle(vh,
                center + outward * 6f,
                center - outward * 5f + side * 5f,
                center - outward * 5f - side * 5f,
                color);
        }
    }

    // Purpose: Performs the world to map workflow for this component.
    private Vector2 WorldToMap(Vector3 world, Rect mapRect)
    {
        float sizeX = Mathf.Max(0.001f, _worldBounds.size.x);
        float sizeZ = Mathf.Max(0.001f, _worldBounds.size.z);
        return new Vector2(
            Mathf.Lerp(mapRect.xMin, mapRect.xMax,
                Mathf.InverseLerp(_worldBounds.min.x, _worldBounds.min.x + sizeX, world.x)),
            Mathf.Lerp(mapRect.yMin, mapRect.yMax,
                Mathf.InverseLerp(_worldBounds.min.z, _worldBounds.min.z + sizeZ, world.z)));
    }

    // Purpose: Performs the inset workflow for this component.
    private static Rect Inset(Rect rect, float amount) => Rect.MinMaxRect(
        rect.xMin + amount, rect.yMin + amount, rect.xMax - amount, rect.yMax - amount);

    // Purpose: Adds the requested rect outline to this component.
    private static void AddRectOutline(VertexHelper vh, Rect rect, float thickness, Color color)
    {
        AddQuad(vh, Rect.MinMaxRect(rect.xMin, rect.yMax - thickness, rect.xMax, rect.yMax), color);
        AddQuad(vh, Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax, rect.yMin + thickness), color);
        AddQuad(vh, Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMin + thickness, rect.yMax), color);
        AddQuad(vh, Rect.MinMaxRect(rect.xMax - thickness, rect.yMin, rect.xMax, rect.yMax), color);
    }

    // Purpose: Adds the requested diamond to this component.
    private static void AddDiamond(VertexHelper vh, Vector2 center, float radius, Color color)
    {
        AddTriangle(vh, center + Vector2.up * radius, center + Vector2.right * radius, center + Vector2.down * radius, color);
        AddTriangle(vh, center + Vector2.up * radius, center + Vector2.down * radius, center + Vector2.left * radius, color);
    }

    // Purpose: Adds the requested circle to this component.
    private static void AddCircle(VertexHelper vh, Vector2 center, float radius, int segments, Color color)
    {
        Vector2 previous = center + Vector2.up * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector2 current = center + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            AddTriangle(vh, center, previous, current, color);
            previous = current;
        }
    }

    // Purpose: Adds the requested quad to this component.
    private static void AddQuad(VertexHelper vh, Rect rect, Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.up);
        vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.one);
        vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.right);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    // Purpose: Adds the requested quad to this component.
    private static void AddQuad(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d,
        Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(a.x, a.y), color, Vector2.zero);
        vh.AddVert(new Vector3(b.x, b.y), color, Vector2.up);
        vh.AddVert(new Vector3(c.x, c.y), color, Vector2.one);
        vh.AddVert(new Vector3(d.x, d.y), color, Vector2.right);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    // Purpose: Adds the requested triangle to this component.
    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(a.x, a.y, 0f), color, Vector2.zero);
        vh.AddVert(new Vector3(b.x, b.y, 0f), color, Vector2.zero);
        vh.AddVert(new Vector3(c.x, c.y, 0f), color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }
}
