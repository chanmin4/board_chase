using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class DebugMonitorWindow
{
    private enum ColliderScope { SelectedObject, CurrentSector, AllLoadedScenes }
    [SerializeField] private bool _showColliders;
    [SerializeField] private bool _showGameColliders = true, _showSceneColliders;
    [SerializeField] private bool _colliderDetailedMesh;
    [SerializeField] private ColliderScope _colliderScope = ColliderScope.CurrentSector;
    [SerializeField] private bool _colliderCharacters = true, _colliderProjectiles = true;
    [SerializeField] private bool _colliderEnvironment, _colliderOther;
    [SerializeField] private bool _colliderTriggers = true, _colliderDisabled, _colliderLabels, _colliderXray = true;
    [SerializeField] private bool _colliderSweeps = true;
    [SerializeField] private int _colliderLimit = 1500;
    [SerializeField] private float _sweepSeconds = .8f;
    [NonSerialized] private Collider[] _colliderCache = Array.Empty<Collider>();
    private double _nextColliderRefresh;
    private int _colliderDrawn, _colliderMatched, _meshBudget;
    private Vector2 _colliderScroll;
    private readonly Dictionary<int, (Vector3[] vertices, int[] triangles)> _meshWireCache = new();
    private readonly Dictionary<int, int> _colliderCategories = new();

    private void DrawColliderControls()
    {
        _colliderScroll = EditorGUILayout.BeginScrollView(_colliderScroll);
        EditorGUI.BeginChangeCheck();
        _showColliders = EditorGUILayout.Toggle("콜라이더 표시 ON / OFF", _showColliders);
        using (new EditorGUI.DisabledScope(!_showColliders))
        {
            _showGameColliders = EditorGUILayout.Toggle("Game 표시 · Play 중 실시간", _showGameColliders);
            _showSceneColliders = EditorGUILayout.Toggle("Scene 표시 · 별도 선택", _showSceneColliders);
            int scope = EditorGUILayout.Popup("표시 범위", (int)_colliderScope,
                new[] { "선택 개체와 자식", "현재 섹터", "열린 모든 씬" });
            _colliderScope = (ColliderScope)scope;
            _colliderCharacters = EditorGUILayout.Toggle("Player / 적 · 몸체·피격부", _colliderCharacters);
            _colliderProjectiles = EditorGUILayout.Toggle("총알 / 투척물 / 적 발사체", _colliderProjectiles);
            _colliderEnvironment = EditorGUILayout.Toggle("바닥 / 벽 / 섹터 구조물", _colliderEnvironment);
            _colliderOther = EditorGUILayout.Toggle("기타 개체 / 아이템 / 상호작용", _colliderOther);
            _colliderTriggers = EditorGUILayout.Toggle("Trigger 포함", _colliderTriggers);
            _colliderDisabled = EditorGUILayout.Toggle("비활성 / 꺼진 Collider 포함", _colliderDisabled);
            _colliderLabels = EditorGUILayout.Toggle("개체 이름 / 형태 표시", _colliderLabels);
            _colliderXray = EditorGUILayout.Toggle("벽 뒤 형상도 표시", _colliderXray);
            _colliderDetailedMesh = EditorGUILayout.Toggle("Scene Mesh 상세선 · 고비용", _colliderDetailedMesh);
            _colliderLimit = Mathf.Clamp(EditorGUILayout.IntField("최대 표시 개수 (초과 시 제한)", _colliderLimit), 50, 10000);
            _colliderSweeps = EditorGUILayout.Toggle("총알의 실제 SphereCast 경로", _colliderSweeps);
            _sweepSeconds = EditorGUILayout.Slider("판정 경로 잔상 (초)", _sweepSeconds, .05f, 3f);
            if (GUILayout.Button("캐시 새로고침")) { _nextColliderRefresh = 0; _meshWireCache.Clear(); _colliderCategories.Clear(); }
        }
        bool changed = EditorGUI.EndChangeCheck();
        if (changed) { _nextColliderRefresh = 0; SceneView.RepaintAll(); }
        UpdateGameColliderOverlay();
        CombatPhysicsTrace.Enabled = _showColliders && _colliderProjectiles && _colliderSweeps;
        EditorGUILayout.HelpBox($"Game {_gameColliderDrawn}개 / Scene {_colliderDrawn}개. 목록은 1초마다 갱신하고 위치는 실시간 표시합니다. Game 표시에는 Gizmos를 켤 필요가 없습니다. 임시 UI만 생성하며 원본 Collider·씬·프리팹은 수정하지 않습니다.", MessageType.None);
        EditorGUILayout.HelpBox("총알은 모델 위치와 실제 판정 위치가 다를 수 있어 실행된 비행 SphereCast를 표시합니다 (예측용 Cast 제외). 파랑=캐릭터, 빨강=발사체, 노랑=구조물, 초록=기타, 회색=비활성. Trigger에는 T 표시.", MessageType.Info);
        EditorGUILayout.HelpBox("Game은 벽 뒤도 보이는 디버그 오버레이입니다. Mesh/Terrain은 저비용 근사 Bounds이며, Scene Mesh 상세선을 켜면 원본 삼각형 선을 표시합니다. 정확한 Convex·Terrain 형상은 Unity Physics Debugger도 이용하세요.", MessageType.None);
        if (GUILayout.Button("Unity Physics Debugger 열기 · 정확한 물리 형상"))
            EditorApplication.ExecuteMenuItem("Window/Analysis/Physics Debugger");
        EditorGUILayout.EndScrollView();
    }

    internal static bool IsSceneCollider(Collider collider)
    {
        if (collider == null || EditorUtility.IsPersistent(collider)) return false;
        var scene = collider.gameObject.scene;
        return scene.IsValid() && scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene) &&
            PrefabStageUtility.GetPrefabStage(collider.gameObject) == null;
    }

    private void HandleColliderSceneGUI(SceneView view)
    {
        if (!_showColliders || !_showSceneColliders || Event.current.type != EventType.Repaint) return;
        CombatPhysicsTrace.Enabled = _colliderProjectiles && _colliderSweeps;
        RefreshColliderCache();
        var previousDepth = Handles.zTest;
        try
        {
            Handles.zTest = _colliderXray ? CompareFunction.Always : CompareFunction.LessEqual;
            _colliderDrawn = _colliderMatched = 0; _meshBudget = 3000;
            foreach (var collider in _colliderCache)
            {
                if (collider == null || !_colliderDisabled && (!collider.enabled || !collider.gameObject.activeInHierarchy) ||
                    !_colliderTriggers && collider.isTrigger || !WithinColliderScope(collider.transform)) continue;
                int category = CachedColliderCategory(collider);
                if (category == 0 && !_colliderCharacters || category == 1 && !_colliderProjectiles ||
                    category == 2 && !_colliderEnvironment || category == 3 && !_colliderOther) continue;
                _colliderMatched++;
                if (_colliderDrawn >= _colliderLimit) continue;
                Color color = !collider.enabled || !collider.gameObject.activeInHierarchy ? Color.gray :
                    category == 0 ? Color.cyan : category == 1 ? new Color(1, .3f, .2f) : category == 2 ? Color.yellow : Color.green;
                using (new Handles.DrawingScope(color)) DrawColliderShape(collider);
                _colliderDrawn++;
            }
            if (_colliderSweeps && _colliderProjectiles && Application.isPlaying)
                foreach (var sweep in CombatPhysicsTrace.Recent)
                {
                    if (Time.time - sweep.Time > _sweepSeconds || !WithinSweepScope(sweep)) continue;
                    using (new Handles.DrawingScope(sweep.DamageHit ? Color.magenta : sweep.Hit ? Color.yellow : Color.red))
                    {
                        DrawWireCapsule(sweep.Origin, sweep.End, sweep.Radius);
                        if (sweep.Hit) Handles.DrawWireDisc(sweep.HitPoint, view.camera.transform.forward, .15f);
                        if (_colliderLabels) Handles.Label(sweep.End, sweep.Hit ? "Hit: " + sweep.HitName : "실제 Cast · " + sweep.SourceName);
                    }
                }
        }
        finally { Handles.zTest = previousDepth; }
    }
    private bool WithinColliderScope(Transform target)
    {
        if (_colliderScope == ColliderScope.AllLoadedScenes) return true;
        if (_colliderScope == ColliderScope.SelectedObject)
        {
            var selected = Selection.activeTransform;
            return selected != null && (target == selected || target.IsChildOf(selected));
        }
        var sector = _currentSector != null ? _currentSector : _colliderLabSector;
        return sector != null && (target == sector.transform || target.IsChildOf(sector.transform));
    }
    private bool WithinSweepScope(CombatPhysicsTrace.Sweep sweep)
    {
        if (_colliderScope == ColliderScope.AllLoadedScenes) return true;
        if (_colliderScope == ColliderScope.CurrentSector)
        {
            var sector = _currentSector != null ? _currentSector : _colliderLabSector;
            return sector != null && sweep.SectorId == sector.GetInstanceID();
        }
        var selected = Selection.activeTransform;
        if (selected == null) return false;
        // Recorded ids continue to work after a projectile has already been destroyed.
        var root = EntityComponentResolver.ResolveEntityRoot(selected) ?? selected;
        return sweep.SourceId == root.gameObject.GetInstanceID() || sweep.ProjectileId == selected.gameObject.GetInstanceID();
    }
    internal static int ColliderCategory(Collider collider)
    {
        if (collider.GetComponentInParent<BasicBullet>(true) != null || collider.GetComponentInParent<ThrowableProjectile>(true) != null ||
            collider.GetComponentInParent<EnemyProjectile>(true) != null || collider.GetComponentInParent<EnemyArcBombProjectile>(true) != null) return 1;
        if (collider.GetComponentInParent<PlayerCharacter>(true) != null ||
            collider.GetComponentInParent<Enemy>(true) != null) return 0;
        string layer = LayerMask.LayerToName(collider.gameObject.layer);
        return layer == "SectorFloor" || layer == "SectorStructure" ? 2 : 3;
    }
    private void DrawColliderShape(Collider collider)
    {
        string shape = collider.GetType().Name;
        bool approximate = false;
        if (collider is BoxCollider box)
        {
            using (new Handles.DrawingScope(collider.transform.localToWorldMatrix)) Handles.DrawWireCube(box.center, box.size);
        }
        else if (collider is SphereCollider sphere)
        {
            Vector3 s = Abs(collider.transform.lossyScale);
            DrawWireSphere(collider.transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(s.x, s.y, s.z));
        }
        else if (collider is CapsuleCollider capsule)
        {
            CapsuleGeometry(collider.transform, capsule.center, capsule.direction, capsule.radius, capsule.height,
                out var a, out var b, out var radius);
            DrawWireCapsule(a, b, radius);
        }
        else if (collider is CharacterController controller)
        {
            CapsuleGeometry(collider.transform, controller.center, 1, controller.radius, controller.height,
                out var a, out var b, out var radius);
            DrawWireCapsule(a, b, radius);
        }
        else if (_colliderDetailedMesh && collider is MeshCollider mesh && mesh.sharedMesh != null && mesh.sharedMesh.isReadable)
        {
            if (!_meshWireCache.TryGetValue(mesh.sharedMesh.GetInstanceID(), out var data))
            {
                if (_meshWireCache.Count > 128) _meshWireCache.Clear();
                data = (mesh.sharedMesh.vertices, mesh.sharedMesh.triangles);
                _meshWireCache[mesh.sharedMesh.GetInstanceID()] = data;
            }
            using (new Handles.DrawingScope(collider.transform.localToWorldMatrix))
                for (int i = 0; i + 2 < data.triangles.Length && _meshBudget >= 3; i += 3)
                {
                    var a = data.vertices[data.triangles[i]]; var b = data.vertices[data.triangles[i + 1]]; var c = data.vertices[data.triangles[i + 2]];
                    Handles.DrawLine(a, b); Handles.DrawLine(b, c); Handles.DrawLine(c, a); _meshBudget -= 3;
                }
            approximate = mesh.convex || _meshBudget < 3;
            if (_meshBudget < 3) Handles.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
        else
        {
            // Collider.bounds is empty when disabled; local asset bounds still show inactive shapes.
            if (collider is MeshCollider unreadable && unreadable.sharedMesh != null)
            {
                var bounds = unreadable.sharedMesh.bounds;
                using (new Handles.DrawingScope(collider.transform.localToWorldMatrix)) Handles.DrawWireCube(bounds.center, bounds.size);
            }
            else if (collider is TerrainCollider terrain && terrain.terrainData != null)
            {
                var bounds = terrain.terrainData.bounds;
                using (new Handles.DrawingScope(collider.transform.localToWorldMatrix)) Handles.DrawWireCube(bounds.center, bounds.size);
            }
            else Handles.DrawWireCube(collider.bounds.center, collider.bounds.size);
            approximate = true;
        }
        if (_colliderLabels)
            Handles.Label(collider.transform.position, collider.name + " · " + shape +
                (collider.isTrigger ? " [T]" : "") + (approximate ? " [근사 Bounds / Mesh]" : ""));
    }
    private static Vector3 Abs(Vector3 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    internal static void CapsuleGeometry(Transform transform, Vector3 center, int direction, float radius, float height,
        out Vector3 a, out Vector3 b, out float worldRadius)
    {
        var scale = Abs(transform.lossyScale);
        float axisScale = direction == 0 ? scale.x : direction == 1 ? scale.y : scale.z;
        float widthScale = direction == 0 ? Mathf.Max(scale.y, scale.z) : direction == 1 ? Mathf.Max(scale.x, scale.z) : Mathf.Max(scale.x, scale.y);
        worldRadius = radius * widthScale;
        float halfSegment = Mathf.Max(0, height * axisScale * .5f - worldRadius);
        var axis = transform.rotation * (direction == 0 ? Vector3.right : direction == 1 ? Vector3.up : Vector3.forward);
        var worldCenter = transform.TransformPoint(center);
        a = worldCenter - axis * halfSegment; b = worldCenter + axis * halfSegment;
    }
    private static void DrawWireSphere(Vector3 center, float radius)
    {
        Handles.DrawWireDisc(center, Vector3.up, radius);
        Handles.DrawWireDisc(center, Vector3.right, radius);
        Handles.DrawWireDisc(center, Vector3.forward, radius);
    }
    private static void DrawWireCapsule(Vector3 a, Vector3 b, float radius)
    {
        DrawWireSphere(a, radius); DrawWireSphere(b, radius);
        Vector3 axis = b - a;
        if (axis.sqrMagnitude < .000001f) return;
        var rotation = Quaternion.FromToRotation(Vector3.up, axis.normalized);
        for (int i = 0; i < 4; i++)
        { var side = i == 0 ? Vector3.right : i == 1 ? Vector3.left : i == 2 ? Vector3.forward : Vector3.back;
          var offset = rotation * side * radius; Handles.DrawLine(a + offset, b + offset); }
    }
}
