using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class DebugMonitorWindow
{
    private GameColliderOverlay _gameColliderOverlay;
    private Camera _gameColliderCamera;
    private int _gameColliderDrawn;
    private readonly Plane[] _gameColliderFrustum = new Plane[6];
    private readonly List<Vector3> _gameColliderLines = new(256);
    private static readonly Vector2[] WireCircle = CreateWireCircle();
    private static readonly int[] BoxEdges = { 0,1, 0,2, 0,4, 1,3, 1,5, 2,3, 2,6, 3,7, 4,5, 4,6, 5,7, 6,7 };
    private readonly Vector3[] _wireCorners = new Vector3[8];
    private SectorRuntime _colliderLabSector;

    private void RefreshColliderCache()
    {
        if (EditorApplication.timeSinceStartup < _nextColliderRefresh) return;
        var found = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var collider in found) if (IsSceneCollider(collider)) found[count++] = collider;
        System.Array.Resize(ref found, count);
        _colliderCache = found;
        _nextColliderRefresh = EditorApplication.timeSinceStartup + 1;
        _colliderLabSector = CombatLabSession.Current?.Sector;
        // Classification is reused between frames, including Scene and Game.
        if (_colliderCategories.Count > 10000) _colliderCategories.Clear();
    }
    private int CachedColliderCategory(Collider collider)
    {
        int id = collider.GetInstanceID();
        if (!_colliderCategories.TryGetValue(id, out int category))
            _colliderCategories[id] = category = ColliderCategory(collider);
        return category;
    }
    private void UpdateGameColliderOverlay()
    {
        bool active = _showColliders && _showGameColliders && EditorApplication.isPlaying;
        if (!active) { DestroyGameColliderOverlay(); return; }
        if (_gameColliderOverlay != null) return;
        var root = new GameObject("Debug Monitor — temporary Game collider overlay", typeof(RectTransform), typeof(Canvas));
        root.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
        var child = new GameObject("Collider wires", typeof(RectTransform), typeof(CanvasRenderer), typeof(GameColliderOverlay));
        child.hideFlags = HideFlags.HideAndDontSave; child.transform.SetParent(root.transform, false);
        var rect = (RectTransform)child.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        _gameColliderOverlay = child.GetComponent<GameColliderOverlay>();
        _gameColliderOverlay.raycastTarget = false;
        _gameColliderOverlay.Populate = PopulateGameColliders;
        _gameColliderOverlay.DrawLabels = DrawGameColliderLabels;
    }
    private void DestroyGameColliderOverlay()
    {
        if (_gameColliderOverlay != null)
        {
            _gameColliderOverlay.Populate = null; _gameColliderOverlay.DrawLabels = null;
            Object.DestroyImmediate(_gameColliderOverlay.transform.parent.gameObject);
        }
        _gameColliderOverlay = null; _gameColliderCamera = null; _gameColliderDrawn = 0;
    }
    private bool GameColliderVisible(Collider collider)
    {
        if (collider == null || (!_colliderDisabled && (!collider.enabled || !collider.gameObject.activeInHierarchy)) ||
            (!_colliderTriggers && collider.isTrigger) || !WithinColliderScope(collider.transform)) return false;
        int category = CachedColliderCategory(collider);
        return category == 0 ? _colliderCharacters : category == 1 ? _colliderProjectiles : category == 2 ? _colliderEnvironment : _colliderOther;
    }
    private Color ColliderWireColor(Collider collider)
    {
        if (!collider.enabled || !collider.gameObject.activeInHierarchy) return Color.gray;
        int category = CachedColliderCategory(collider);
        return category == 0 ? Color.cyan : category == 1 ? new Color(1,.3f,.2f) : category == 2 ? Color.yellow : Color.green;
    }
    private void PopulateGameColliders(VertexHelper vertices, RectTransform root)
    {
        RefreshColliderCache();
        if (_gameColliderCamera == null || !_gameColliderCamera.isActiveAndEnabled)
        {
            var manager = Object.FindAnyObjectByType<CameraManager>();
            _gameColliderCamera = manager != null ? manager.GetComponentInChildren<Camera>() : Camera.main;
        }
        if (_gameColliderCamera == null || _gameColliderCamera.targetTexture != null) return;
        GeometryUtility.CalculateFrustumPlanes(_gameColliderCamera, _gameColliderFrustum);
        _gameColliderDrawn = 0;
        const int vertexBudget = 32000; // At most 8k line segments in one UI batch.
        foreach (var collider in _colliderCache)
        {
            if (_gameColliderDrawn >= _colliderLimit || vertices.currentVertCount >= vertexBudget) break;
            if (!GameColliderVisible(collider)) continue;
            if (collider.enabled && collider.gameObject.activeInHierarchy && !GeometryUtility.TestPlanesAABB(_gameColliderFrustum, collider.bounds)) continue;
            BuildGameWire(collider);
            var color = ColliderWireColor(collider);
            for (int i = 0; i + 1 < _gameColliderLines.Count && vertices.currentVertCount < vertexBudget; i += 2)
                AddGameLine(vertices, root, _gameColliderLines[i], _gameColliderLines[i+1], color);
            _gameColliderDrawn++;
        }
        if (_colliderProjectiles && _colliderSweeps)
            foreach (var sweep in CombatPhysicsTrace.Recent)
            {
                if (vertices.currentVertCount >= vertexBudget) break;
                if (Time.time - sweep.Time > _sweepSeconds || !WithinSweepScope(sweep)) continue;
                _gameColliderLines.Clear(); AddCapsuleWire(sweep.Origin, sweep.End, sweep.Radius);
                Color color = sweep.DamageHit ? Color.magenta : sweep.Hit ? Color.yellow : Color.red;
                for (int i = 0; i + 1 < _gameColliderLines.Count && vertices.currentVertCount < vertexBudget; i += 2)
                    AddGameLine(vertices, root, _gameColliderLines[i], _gameColliderLines[i+1], color);
            }
    }
    private void AddGameLine(VertexHelper vertices, RectTransform root, Vector3 a, Vector3 b, Color color)
    {
        if (GameColliderOverlay.ProjectSegment(_gameColliderCamera, root, a, b, out var start, out var end))
            GameColliderOverlay.AddLine(vertices, start, end, color);
    }
    private void BuildGameWire(Collider collider)
    {
        _gameColliderLines.Clear();
        if (collider is BoxCollider box) AddBoxWire(box.center, box.size, collider.transform.localToWorldMatrix);
        else if (collider is SphereCollider sphere)
        { var scale = Abs(sphere.transform.lossyScale); AddSphereWire(sphere.transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(scale.x,scale.y,scale.z)); }
        else if (collider is CapsuleCollider capsule)
        { CapsuleGeometry(capsule.transform, capsule.center, capsule.direction, capsule.radius, capsule.height, out var a, out var b, out var radius); AddCapsuleWire(a,b,radius); }
        else if (collider is CharacterController body)
        { CapsuleGeometry(body.transform, body.center, 1, body.radius, body.height, out var a, out var b, out var radius); AddCapsuleWire(a,b,radius); }
        else if (collider is MeshCollider mesh && mesh.sharedMesh != null)
        { var bounds = mesh.sharedMesh.bounds; AddBoxWire(bounds.center,bounds.size,mesh.transform.localToWorldMatrix); }
        else if (collider is TerrainCollider terrain && terrain.terrainData != null)
        { var bounds = terrain.terrainData.bounds; AddBoxWire(bounds.center,bounds.size,terrain.transform.localToWorldMatrix); }
        else AddBoxWire(collider.bounds.center,collider.bounds.size,Matrix4x4.identity);
    }
    private void AddBoxWire(Vector3 center, Vector3 size, Matrix4x4 matrix)
    {
        for (int i = 0; i < 8; i++) _wireCorners[i] = matrix.MultiplyPoint3x4(center + Vector3.Scale(size*.5f,
            new Vector3((i&1)==0 ? -1:1, (i&2)==0 ? -1:1, (i&4)==0 ? -1:1)));
        for (int i=0;i<BoxEdges.Length;i++) _gameColliderLines.Add(_wireCorners[BoxEdges[i]]);
    }
    private static Vector2[] CreateWireCircle()
    {
        var points = new Vector2[16];
        for (int i=0;i<points.Length;i++) { float angle = i*Mathf.PI*2/points.Length; points[i] = new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)); }
        return points;
    }
    private void AddSphereWire(Vector3 center, float radius)
    {
        for (int plane=0;plane<3;plane++) for (int i=0;i<WireCircle.Length;i++)
        {
            Vector2 a=WireCircle[i]*radius,b=WireCircle[(i+1)%WireCircle.Length]*radius;
            _gameColliderLines.Add(center+(plane==0?new Vector3(a.x,0,a.y):plane==1?new Vector3(a.x,a.y,0):new Vector3(0,a.x,a.y)));
            _gameColliderLines.Add(center+(plane==0?new Vector3(b.x,0,b.y):plane==1?new Vector3(b.x,b.y,0):new Vector3(0,b.x,b.y)));
        }
    }
    private void AddCapsuleWire(Vector3 a, Vector3 b, float radius)
    {
        AddSphereWire(a,radius);
        if ((b-a).sqrMagnitude < .000001f) return;
        AddSphereWire(b,radius);
        var rotation = Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        for (int i=0;i<4;i++)
        { var side=i==0?Vector3.right:i==1?Vector3.left:i==2?Vector3.forward:Vector3.back;
          var offset=rotation*side*radius; _gameColliderLines.Add(a+offset);_gameColliderLines.Add(b+offset); }
    }
    private void DrawGameColliderLabels()
    {
        if (!_colliderLabels || _gameColliderCamera == null) return;
        int count=0;
        foreach (var collider in _colliderCache)
        {
            if (count >= Mathf.Min(60,_colliderLimit)) break;
            if (!GameColliderVisible(collider)) continue;
            var point=_gameColliderCamera.WorldToScreenPoint(collider.transform.position);
            if (point.z <= _gameColliderCamera.nearClipPlane || !_gameColliderCamera.pixelRect.Contains(point)) continue;
            GUI.Label(new Rect(point.x,Screen.height-point.y,260,20),collider.name+" · "+collider.GetType().Name+(collider.isTrigger?" [T]":"")); count++;
        }
    }
}
