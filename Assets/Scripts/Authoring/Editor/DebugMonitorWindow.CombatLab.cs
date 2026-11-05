using System;
using UnityEditor;
using UnityEngine;
using VSplatter.StateMachine.Debugging;

public sealed partial class DebugMonitorWindow
{
    [SerializeField] private SectorPresetSO _labPreset;
    [SerializeField] private StageDefinitionSO _labStage;
    [SerializeField] private int _labSeed = 12345;
    [SerializeField] private bool _labStrongRoom;
    [SerializeField] private Vector3 _labPlayerPosition;
    [SerializeField] private float _labPlayerYaw;
    [SerializeField] private EnemyShooterConfigSO _labSpawnConfig;
    [SerializeField] private Vector3 _labSpawnPosition;
    [SerializeField] private float _labSpawnYaw;
    [SerializeField] private int _labSpawnCount = 1;
    private int _labPlacement = -2; // -2 none, -1 player, 0 immediate enemy
    private Vector2 _labScroll, _traceScroll;
    private string _labMessage;
    private MessageType _labMessageType = MessageType.Info;
    private bool _traceFailures;
    private bool _traceFoldout = true;
    [SerializeField] private float _tracePanelHeight = 300f;

    private void DrawCombatLab()
    {
        _labScroll = EditorGUILayout.BeginScrollView(_labScroll);
        EditorGUILayout.HelpBox("기존 게임을 HOME에서 Play → 전투 테스트 시작. 섹터만 임시 Additive 씬에 추가하고 기존 플레이어·Main Camera·RUN 매니저를 그대로 사용합니다. 종료 시 진입 장비/탄약/체력/감염도를 복구하고 HOME 시작 위치로 돌아갑니다. 실험 처치·아이템은 실제 진행/저장에 반영하지 않습니다.", MessageType.Info);
        if (!string.IsNullOrEmpty(_labMessage)) EditorGUILayout.HelpBox(_labMessage, _labMessageType);
        if (_labStage == null) _labStage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>("Assets/ScriptableObject/GameScene/Stage/Stage1Definition.asset");
        var current = CombatLabSession.Current;
        using (new EditorGUI.DisabledScope(current != null))
        {
            EditorGUI.BeginChangeCheck();
            _labPreset = (SectorPresetSO)EditorGUILayout.ObjectField("싸움 장소 · Sector Preset", _labPreset, typeof(SectorPresetSO), false);
            if (EditorGUI.EndChangeCheck() && _labPreset != null && _labPreset.SectorPrefab != null)
            {
                var sector = _labPreset.SectorPrefab;
                _labPlayerPosition = sector.transform.InverseTransformPoint(sector.PlayerStartPoint.position);
                _labPlayerYaw = sector.PlayerStartPoint.localEulerAngles.y;
                _labSpawnPosition = _labPlayerPosition + Vector3.forward * 5;
            }
            _labStage = (StageDefinitionSO)EditorGUILayout.ObjectField("장비/소지품 기준 스테이지", _labStage, typeof(StageDefinitionSO), false);
            _labSeed = EditorGUILayout.IntField("추첨 Seed", _labSeed);
            _labStrongRoom = EditorGUILayout.Toggle("금고방 장비 등급 보정", _labStrongRoom);
        }
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || current != null))
            if (GUILayout.Button("전투 테스트 시작 · 기존 HOME 플레이어 이동"))
                LabAction(() =>
                {
                    if (_labStage == null) throw new InvalidOperationException("Stage를 선택하세요.");
                    var driver = CombatLabSession.Prepare(_labPreset);
                    CopyLabPlan(driver);
                    CombatLabSession.Start(driver);
                    if (_labSpawnPosition == Vector3.zero) _labSpawnPosition = _labPlayerPosition + Vector3.forward * 5;
                    _labMessage = "시작 완료. Game에서 그대로 플레이하고 실시간으로 적을 추가/제거하세요.";
                    _labMessageType = MessageType.Info;
                });
        current = CombatLabSession.Current;
        bool editing = current == null || (current.Ready && !current.IsEnding);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Player 시작 위치 · 섹터 로컬 좌표", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!editing))
        {
            _labPlayerPosition = EditorGUILayout.Vector3Field("Player 바닥 위치", _labPlayerPosition);
            _labPlayerYaw = EditorGUILayout.FloatField("Player 방향 Y (도)", _labPlayerYaw);
            if (GUILayout.Toggle(_labPlacement == -1, "Scene에서 Player 위치 지정", "Button")) _labPlacement = -1;
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("적 즉시 소환 · 섹터 로컬 좌표", EditorStyles.boldLabel);
        _labSpawnConfig = (EnemyShooterConfigSO)EditorGUILayout.ObjectField("소환할 적 Config", _labSpawnConfig, typeof(EnemyShooterConfigSO), false);
        _labSpawnPosition = EditorGUILayout.Vector3Field("소환 좌표", _labSpawnPosition);
        _labSpawnYaw = EditorGUILayout.FloatField("방향 Y (도)", _labSpawnYaw);
        _labSpawnCount = Mathf.Clamp(EditorGUILayout.IntField("수량", _labSpawnCount), 1, 64);
        EditorGUILayout.LabelField("좌표는 실험 섹터 내부 기준입니다. 수량이 여러 개면 입력 위치부터 간격을 두고 소환합니다.", EditorStyles.wordWrappedMiniLabel);
        using (new EditorGUI.DisabledScope(current == null || !current.Ready || current.IsEnding))
        {
            if (GUILayout.Button("플레이어 앞 좌표 가져오기"))
            {
                _labSpawnPosition = current.Sector.transform.InverseTransformPoint(current.Player.transform.position + current.Player.transform.forward * 5);
                _labSpawnPosition.y = _labPlayerPosition.y;
            }
            if (GUILayout.Button("입력 좌표에 적 즉시 소환"))
                LabAction(() =>
                {
                    if (_labSpawnConfig == null) throw new InvalidOperationException("소환할 적 Config를 선택하세요.");
                    for (int i = 0; i < _labSpawnCount; i++)
                    {
                        // Spread multiple summons instead of stacking their bodies at one point.
                        var offset = new Vector3((i % 8) * 2f, 0, (i / 8) * 2f);
                        current.SummonEnemy(_labSpawnConfig, _labSpawnPosition + offset, _labSpawnYaw);
                    }
                    _labMessage = $"{_labSpawnConfig.name} {_labSpawnCount}개체 소환 완료";
                    _labMessageType = MessageType.Info;
                });
            if (GUILayout.Toggle(_labPlacement == 0, "Scene에서 소환 좌표 지정 (선택)", "Button")) _labPlacement = 0;
        }
        if (_labPlacement != -2 && GUILayout.Button("위치 지정 끝내기")) _labPlacement = -2;
        if (current != null)
        {
            if (!string.IsNullOrEmpty(current.Status)) EditorGUILayout.HelpBox(current.Status, current.Ready ? MessageType.None : MessageType.Error);
            if (EditorApplication.isPlaying && EditorApplication.isPaused)
            {
                EditorGUILayout.HelpBox("Unity가 일시정지 중입니다. Error Pause가 켜져 있다면 Console 오류 내용을 먼저 확인하세요.", MessageType.Warning);
                if (GUILayout.Button("확인 후 전투 계속 · 일시정지 해제")) EditorApplication.isPaused = false;
            }
            using (new EditorGUI.DisabledScope(current.IsEnding))
            {
                if (GUILayout.Button("전투 재시작 · 소환했던 적 / 진입 장비 복구"))
                    LabAction(() => { CopyLabPlan(current); current.RestartCombat(); });
                if (GUILayout.Button("모든 적 / 시체 / 발사체 제거"))
                    LabAction(() => { current.Spawns.Clear(); current.ClearEnemies(); });
                foreach (var enemy in current.Enemies.ToArray())
                {
                    if (enemy == null) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(enemy, typeof(Enemy), true);
                        if (GUILayout.Button("제거", GUILayout.Width(55))) LabAction(() => current.RemoveEnemy(enemy));
                    }
                }
                if (GUILayout.Button("전투 실험 종료 · HOME 복귀")) LabAction(CombatLabSession.Restore);
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("전투 중 ‘캐릭터’ 탭에서 대상을 선택하면 상태 경로, 전환 이유, 체력·장비·소지품을 볼 수 있습니다. ‘콜라이더’ 탭에서 Scene 표시 범위를 선택하세요. Seed는 초기 장비 추첨 재현용이며 이후 프레임/입력까지 완전히 재현하는 리플레이는 아닙니다.", MessageType.Info);
        EditorGUILayout.LabelField("플레이어는 HOME에서 사용하던 실제 장비를 사용합니다. 실험 중 플레이어 사망 판정은 전투 자동 재시작으로 처리합니다. 재시작은 적/발사체와 플레이어 상태를 초기화하며 방의 소품 상태는 유지합니다.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
    }

    private void CopyLabPlan(CombatLabRuntime driver)
    {
        driver.Stage = _labStage;
        driver.Seed = _labSeed; driver.StrongRoom = _labStrongRoom;
        driver.PlayerPosition = _labPlayerPosition; driver.PlayerYaw = _labPlayerYaw;
    }
    private void LabAction(Action action)
    {
        try { action(); SceneView.RepaintAll(); }
        catch (Exception ex)
        {
            _labMessage = "작업 실패: " + ex.Message;
            _labMessageType = MessageType.Error;
            Debug.LogException(ex); // Preserve the exact failing line instead of hiding the exception.
            Repaint();
        }
    }

    private void HandleLabSceneGUI(SceneView view)
    {
        if (_monitorWorkspaceTab != MonitorWorkspaceTab.CombatLab || !EditorApplication.isPlaying) return;
        var driver = CombatLabSession.Current;
        if (driver == null || !driver.Ready || driver.IsEnding) return;
        var playerWorld = driver.Sector.transform.TransformPoint(_labPlayerPosition);
        DrawLabMarker(ref playerWorld, ref _labPlayerYaw, "Player", Color.cyan, _labPlacement == -1);
        _labPlayerPosition = driver.Sector.transform.InverseTransformPoint(playerWorld);
        var spawnWorld = driver.Sector.transform.TransformPoint(_labSpawnPosition);
        DrawLabMarker(ref spawnWorld, ref _labSpawnYaw, "즉시 소환 좌표", Color.red, _labPlacement == 0);
        _labSpawnPosition = driver.Sector.transform.InverseTransformPoint(spawnWorld);
        if (_labPlacement == -2) return;
        var evt = Event.current;
        if (evt.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape) { _labPlacement = -2; evt.Use(); return; }
        if (evt.type != EventType.MouseDown || evt.button != 0 || evt.alt) return;
        Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
        if (Physics.Raycast(ray, out var hit, 10000, LayerMask.GetMask("SectorFloor"), QueryTriggerInteraction.Ignore) &&
            hit.transform.IsChildOf(driver.Sector.transform))
        {
            if (_labPlacement == -1) _labPlayerPosition = driver.Sector.transform.InverseTransformPoint(hit.point);
            else if (_labPlacement == 0) _labSpawnPosition = driver.Sector.transform.InverseTransformPoint(hit.point);
            CopyLabPlan(driver); Repaint(); evt.Use();
        }
    }
    private static void DrawLabMarker(ref Vector3 position, ref float yaw, string label, Color color, bool editable)
    {
        using (new Handles.DrawingScope(color))
        {
            Handles.DrawWireDisc(position, Vector3.up, .45f);
            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(forward), 1.2f, EventType.Repaint);
            Handles.Label(position + Vector3.up, label);
            if (editable)
            {
                position = Handles.PositionHandle(position, Quaternion.identity);
                yaw = Handles.RotationHandle(Quaternion.Euler(0, yaw, 0), position).eulerAngles.y;
            }
        }
    }

    private void DrawExecutionTrace(CharacterRow row)
    {
        _traceFoldout = EditorGUILayout.Foldout(_traceFoldout, "HFSM · 실제 전환 이유 / 검사 결과", true);
        if (!_traceFoldout) return;
        HfsmExecutionTrace.Enabled = EditorGUILayout.Toggle("실행 결과 기록 (Editor 전용)", HfsmExecutionTrace.Enabled);
        _traceFailures = EditorGUILayout.Toggle("최근 검사 탈락 / 미검사도 보기", _traceFailures);
        if (GUILayout.Button("기록 비우기")) HfsmExecutionTrace.Clear();
        GUILayout.Label("조건을 재실행하지 않습니다. AND 단락 평가는 ‘미검사’, 전환 뒤 순위가 낮은 규칙은 평가하지 않습니다.", EditorStyles.wordWrappedMiniLabel);
        _tracePanelHeight = Mathf.Clamp(_tracePanelHeight, 80f, 1200f);
        _traceScroll = EditorGUILayout.BeginScrollView(_traceScroll, GUILayout.Height(_tracePanelHeight));
        int shown = 0;
        var history = HfsmExecutionTrace.Transitions;
        for (int i = history.Count - 1; i >= 0 && shown < 30; i--)
        {
            var entry = history[i];
            if (entry.Owner == null || !Array.Exists(row.stateMachines, m => m == entry.Owner)) continue;
            DrawTraceEntry(entry); shown++;
        }
        if (_traceFailures)
            foreach (var machine in row.stateMachines)
                foreach (var entry in HfsmExecutionTrace.LatestChecks(machine))
                    if (!entry.Passed) DrawTraceEntry(entry);
        if (shown == 0) GUILayout.Label("아직 기록된 전환이 없습니다. 기록을 켠 뒤 실제 AI 전환을 기다리세요.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
        DrawPanelResizeGrip(ref _tracePanelHeight, 80f, 1200f, "HFSM 기록 높이 · 위아래로 드래그");
    }

    private void DrawPanelResizeGrip(ref float height, float minimum, float maximum, string label)
    {
        Rect grip = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
        int controlId = GUIUtility.GetControlID(label.GetHashCode(), FocusType.Passive, grip);
        EditorGUI.DrawRect(grip, EditorGUIUtility.isProSkin
            ? new Color(0.18f, 0.18f, 0.18f)
            : new Color(0.75f, 0.75f, 0.75f));
        GUI.Label(grip, new GUIContent(label, "드래그해서 표시 영역의 높이를 조절합니다."), EditorStyles.centeredGreyMiniLabel);
        EditorGUIUtility.AddCursorRect(grip, MouseCursor.ResizeVertical);
        Event current = Event.current;
        switch (current.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (current.button != 0 || !grip.Contains(current.mousePosition)) break;
                GUIUtility.hotControl = controlId;
                current.Use();
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId) break;
                height = Mathf.Clamp(height + current.delta.y, minimum, maximum);
                current.Use();
                Repaint();
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId || current.button != 0) break;
                GUIUtility.hotControl = 0;
                current.Use();
                Repaint();
                break;
        }
    }
    private static void DrawTraceEntry(HfsmExecutionTrace.Entry entry)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label($"{entry.Time:0.00}s · frame {entry.Frame} · {entry.Level}계층 · {(entry.Passed ? "전환" : "탈락")}", EditorStyles.wordWrappedLabel);
            GUILayout.Label($"{entry.From}_{entry.Level} → {entry.To}_{entry.TargetLevel}", EditorStyles.wordWrappedLabel);
            GUILayout.Label($"{entry.Table} · 행 #{entry.Row} · 우선순위 {entry.Priority} · 종료 {entry.ExitReason}", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label(string.IsNullOrEmpty(entry.Rule) ? "개별 규칙" : "공유 규칙: " + entry.Rule, EditorStyles.wordWrappedMiniLabel);
            if (entry.Always) EditorGUILayout.LabelField("무조건 전환 (Always)");
            foreach (var check in entry.Checks)
            {
                if (check == null) continue;
                string result = check.Actual.HasValue ? $"실제 {check.Actual.Value} · {(check.Actual.Value == check.Expected ? "통과" : "탈락")}" : "미검사 (단락 평가 / Always)";
                GUILayout.Label($"{check.Join} {check.Name}\n기대 {check.Expected} · {result}", EditorStyles.wordWrappedLabel);
            }
        }
    }
}
