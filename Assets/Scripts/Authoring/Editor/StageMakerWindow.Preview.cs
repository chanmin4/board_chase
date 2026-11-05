using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed partial class StageMakerWindow
{
    [SerializeField] private int _seed = 1, _batchCount = 100;
    [SerializeField] private Vector2Int _legacyStart = new(-1, 0);
    [SerializeField] private string _requiredPreviewIds = "";
    private StageMakerPreview _preview;
    private StageMakerPreviewCell _selectedCell;
    private string _generationError, _batchMessage = "", _previewSourceJson;
    private bool _previewStale = true, _fitPending;
    private bool _hasPreviewAttempt, _roomDetailsOpen;
    private Vector2 _mapPan, _detailScroll;
    private Vector2 _fittedViewport;
    private bool _autoFitMap = true;
    private float _zoom = 1;
    private bool _batchRunning;
    private int _batchDone, _batchGraphFailures, _batchContentFailures, _batchRoomTotal, _batchMin, _batchMax, _batchFirstSeed, _batchTarget;
    private readonly List<string> _batchErrors = new();
    private readonly Dictionary<string, int> _batchCategories = new();
    private string _batchSourceJson;
    private static readonly Vector2 Pitch = new(168, 116);

    private HashSet<string> RequiredPreviewIds() => new(
        (_requiredPreviewIds ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
    private void GeneratePreview()
    {
        if (_selected == null || EditorApplication.isCompiling) return;
        if (_batchRunning) _batchMessage += "\n단일 미리보기 생성으로 연속 검증을 중단했습니다.";
        _batchRunning = false;
        _preview = null; _selectedCell = null;
        _hasPreviewAttempt = true;
        _previewSourceJson = EditorJsonUtility.ToJson(_selected);
        StageMakerPreview.TryBuild(_selected, _seed, _legacyStart, RequiredPreviewIds(), out _preview, out _generationError);
        _previewStale = false; _fitPending = true; Repaint();
    }
    private void RandomPreview()
    {
        // Independent editor seed, never changes UnityEngine.Random or a run save.
        _seed = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        GeneratePreview();
    }
    private void DrawPreviewPanel(Vector2 size)
    {
        GUILayout.Label("맵 구조 미리보기 · 실제 생성기 사용", EditorStyles.boldLabel);
        _showMapImages = EditorGUILayout.ToggleLeft("미니맵 이미지 표시 · 편집용 전체 발견 상태", _showMapImages);
        using (new EditorGUI.DisabledScope(_selected == null || EditorApplication.isCompiling))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"시드 {_seed}", EditorStyles.miniLabel);
                if (GUILayout.Button("다시 생성", GUILayout.Width(65))) GeneratePreview();
                if (GUILayout.Button("랜덤", GUILayout.Width(48))) RandomPreview();
                if (GUILayout.Button("맞춤", GUILayout.Width(42))) _fitPending = true;
            }
        }
        if (_hasPreviewAttempt && _selected != null && _previewSourceJson != EditorJsonUtility.ToJson(_selected)) _previewStale = true;
        if (_preview != null && _preview.layout.runSeed != _seed) _previewStale = true;
        if (_previewStale && _hasPreviewAttempt)
            EditorGUILayout.HelpBox("이전 설정의 결과입니다. [생성]을 눌러 갱신하세요.", MessageType.Warning);
        if (!string.IsNullOrEmpty(_generationError) && !_previewStale)
            EditorGUILayout.HelpBox("생성 실패 · 왼쪽 설정 상단에서 원인과 완화안을 확인하세요.", MessageType.Error);
        if (_preview != null)
        {
            if (_preview.layout.compactTestLayout)
            {
                GUILayout.Label($"소형 테스트 · 실제 탈출구 {_preview.layout.extractionRooms.Count}개 · 발전실·탈출구는 가지 끝 (1×1 제외)", EditorStyles.wordWrappedMiniLabel);
            }
            GUILayout.Label($"결과 시드 {_preview.layout.runSeed} · 방 {_preview.cells.Count} · 연결 {_preview.edges} · 참조 오류 {_preview.errors.Count}", EditorStyles.miniBoldLabel);
            GUILayout.Label(string.Join(" / ", _preview.counts.Select(pair => pair.Key + " " + pair.Value)), EditorStyles.wordWrappedMiniLabel);
            var exits = _preview.cells.Where(cell => _preview.layout.IsExtractionSector(cell.room.coord)).ToArray();
            GUILayout.Label($"외곽 탈출구 {exits.Length}곳: " + string.Join(" / ", exits.Select(cell =>
                $"({cell.room.coord.x},{cell.room.coord.y}) " + StageMakerPreview.ExtractionLabel(_preview.layout, cell.room.coord))), EditorStyles.wordWrappedMiniLabel);
            if (_preview.layout.merchantPlacementVersion == 1)
            {
                bool merchant = _preview.layout.rooms.Any(room => room.hasMerchant);
                GUILayout.Label("상인: " + (merchant ? "등장 · 공용 전투방 1곳" :
                    _preview.layout.merchantRollSucceeded ? "추첨 성공 · 배치 가능한 공용 전투방 없음" : "이번 스테이지 미등장"), EditorStyles.wordWrappedMiniLabel);
            }
            if (_preview.usesSpecialRatio)
                GUILayout.Label($"특별맵 목표 {_preview.requestedSpecialRooms}개 → 실제 {_preview.actualSpecialRooms}개 · " +
                    (_preview.actualSpecialRooms > _preview.requestedSpecialRooms ? "필수 보장으로 목표 초과" :
                        $"부족분 {Mathf.Max(0, _preview.requestedSpecialRooms - _preview.actualSpecialRooms)}개는 일반방") +
                    $" (배정 대상 {_preview.ratioBudgetRooms}방)", EditorStyles.wordWrappedMiniLabel);
        }
        GUILayout.Label("우클릭 드래그: 이동  |  휠: 확대/축소  |  좌클릭: 방 상세  |  청록: 실제 연결 / 노랑: 탈출", EditorStyles.wordWrappedMiniLabel);
        Rect canvas = GUILayoutUtility.GetRect(100, 120, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawMap(canvas);
        if (_batchRunning) EditorGUI.ProgressBar(GUILayoutUtility.GetRect(100, 19, GUILayout.ExpandWidth(true)),
            _batchDone / (float)_batchTarget, $"{_batchDone}/{_batchTarget} 시드 확인 중 · 중단 가능");
        if (!string.IsNullOrEmpty(_batchMessage)) EditorGUILayout.HelpBox(_batchMessage, MessageType.Info);
        _roomDetailsOpen = EditorGUILayout.Foldout(_roomDetailsOpen,
            _selectedCell != null ? $"선택한 방 상세 · {_selectedCell.category}" : "방 상세 · 참조 오류 · 검증 로그", true);
        if (_roomDetailsOpen)
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(Mathf.Clamp(size.y * .26f, 130, 210)));
            DrawRoomDetails();
            EditorGUILayout.EndScrollView();
        }
    }
    private void DrawMap(Rect viewport)
    {
        EditorGUI.DrawRect(viewport, new Color(.035f, .055f, .07f));
        GUI.BeginGroup(viewport);
        var bounds = new Rect(0, 0, viewport.width, viewport.height);
        var evt = Event.current;
        int panId = GUIUtility.GetControlID(FocusType.Passive);
        if (bounds.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.MouseDown && evt.button == 1)
            { GUIUtility.hotControl = panId; evt.Use(); }
            if (evt.type == EventType.ScrollWheel)
            {
                _autoFitMap = false; _fitPending = false;
                float next = Mathf.Clamp(_zoom * Mathf.Pow(1.12f, -evt.delta.y), .01f, 2.5f);
                _mapPan = evt.mousePosition - bounds.center - (evt.mousePosition - bounds.center - _mapPan) * (next / _zoom);
                _zoom = next; evt.Use(); Repaint();
            }
            if (evt.type == EventType.ContextClick) evt.Use();
        }
        if (GUIUtility.hotControl == panId)
        {
            if (evt.type == EventType.MouseDrag)
            { _autoFitMap = false; _fitPending = false; _mapPan += evt.delta; evt.Use(); Repaint(); }
            if (evt.type == EventType.MouseUp) { GUIUtility.hotControl = 0; evt.Use(); }
        }
        if (_preview == null)
        {
            GUI.Label(new Rect(20, 25, bounds.width - 40, 90),
                !string.IsNullOrEmpty(_generationError)
                    ? "조건을 만족하는 맵이 생성되지 않았습니다.\n왼쪽 설정 상단에서 원인 확인 → 조건 조정 → 다시 생성하세요."
                    : "왼쪽에서 설정 후 [랜덤 생성]\n씬을 변경하지 않고 방/포탈 구조만 확인합니다.", EditorStyles.wordWrappedLabel);
            GUI.EndGroup(); return;
        }
        // Layout returns a provisional (often minimum-height) rect. Only fit the final repaint rect.
        if (evt.type == EventType.Repaint && bounds.width > 10 && bounds.height > 10 &&
            (_fitPending || (_autoFitMap && (_fittedViewport - bounds.size).sqrMagnitude > 1)))
        {
            var coords = _preview.cells.Select(cell => cell.room.coord).ToArray();
            if (coords.Length > 0)
            {
                float minX = coords.Min(c => c.x), maxX = coords.Max(c => c.x), minY = coords.Min(c => c.y), maxY = coords.Max(c => c.y);
                StageMakerMapView.Fit(bounds.size, new Vector2(minX, minY), new Vector2(maxX, maxY), Pitch,
                    out _zoom, out _mapPan);
            }
            _autoFitMap = true;
            _fittedViewport = bounds.size;
            _fitPending = false;
        }
        Vector2 Point(Vector2Int coord) => bounds.center + _mapPan + new Vector2(coord.x * Pitch.x, -coord.y * Pitch.y) * _zoom;
        Handles.BeginGUI();
        Handles.color = new Color(.1f, .7f, .78f, .8f);
        foreach (var cell in _preview.cells)
        {
            foreach (var offset in new[] { Vector2Int.right, Vector2Int.up })
            {
                var flag = offset == Vector2Int.right ? StageRoomConnectionFlags.Right : StageRoomConnectionFlags.Up;
                if (cell.room.HasConnection(flag) && _preview.byCoord.ContainsKey(cell.room.coord + offset))
                    Handles.DrawAAPolyLine(Mathf.Max(1, 3 * _zoom), Point(cell.room.coord), Point(cell.room.coord + offset));
            }
        }
        Handles.EndGUI();
        var centerStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(12 * _zoom), 8, 20), clipping = TextClipping.Clip };
        var nameStyle = new GUIStyle(centerStyle) { fontStyle = FontStyle.Normal, wordWrap = false };
        var exitColor = new Color(1f, .82f, .15f);
        var exitStyle = new GUIStyle(centerStyle) { fontSize = Mathf.Clamp(Mathf.RoundToInt(13 * _zoom), 9, 20) };
        exitStyle.normal.textColor = exitColor;
        foreach (var cell in _preview.cells)
        {
            Vector2 center = Point(cell.room.coord);
            var box = new Rect(center.x - 65 * _zoom, center.y - 25 * _zoom, 130 * _zoom, 50 * _zoom);
            if (box.xMax < 0 || box.xMin > bounds.width || box.yMax < -45 || box.yMin > bounds.height) continue;
            Color color = CellColor(cell);
            string extraction = StageMakerPreview.ExtractionLabel(_preview.layout, cell.room.coord);
            bool exit = !string.IsNullOrEmpty(extraction);
            float border = exit ? 4 : 2;
            if (exit && cell == _selectedCell)
                EditorGUI.DrawRect(new Rect(box.x - 6, box.y - 6, box.width + 12, box.height + 12), Color.white);
            EditorGUI.DrawRect(new Rect(box.x - border, box.y - border, box.width + border * 2, box.height + border * 2),
                exit ? exitColor : cell == _selectedCell ? Color.white : color * .55f);
            EditorGUI.DrawRect(box, color);
            string tooltip = $"{cell.room.coord} · {cell.category}\n" + (cell.preset != null ? cell.preset.DisplayName : "프리팹 누락") +
                $"\n시작점 거리 {cell.distance}" + (exit ? "\n" + extraction : "") + (cell.locked ? " · 잠금" : "") + (string.IsNullOrEmpty(cell.error) ? "" : "\n" + cell.error);
            Sprite extractionImage = null;
            Sprite image = _showMapImages ? PreviewMapImage(cell, out extractionImage) : null;
            if (image != null || extractionImage != null)
            {
                float imageSize = Mathf.Max(6, box.height - 8 * _zoom);
                if (image != null && extractionImage != null)
                {
                    float gap = 4 * _zoom;
                    DrawMapSprite(new Rect(center.x - imageSize - gap * .5f, center.y - imageSize * .5f, imageSize, imageSize), image);
                    DrawMapSprite(new Rect(center.x + gap * .5f, center.y - imageSize * .5f, imageSize, imageSize), extractionImage);
                }
                else DrawMapSprite(new Rect(center.x - imageSize * .5f, center.y - imageSize * .5f, imageSize, imageSize), image != null ? image : extractionImage);
            }
            Rect coordinate = image != null || extractionImage != null ? new Rect(box.x + 2, box.y + 1, box.width * .31f, Mathf.Max(12, 16 * _zoom)) : box;
            GUI.Label(coordinate, new GUIContent($"{cell.room.coord.x},{cell.room.coord.y}" + (cell.locked ? " [잠금]" : ""), tooltip), centerStyle);
            if (exit)
            {
                float badgeWidth = _zoom >= .45f ? Mathf.Max(box.width + 12, 70) : 16;
                float badgeHeight = Mathf.Clamp(22 * _zoom, 16, 28);
                var badge = new Rect(center.x - badgeWidth * .5f, box.y - badgeHeight - 5, badgeWidth, badgeHeight);
                EditorGUI.DrawRect(badge, new Color(.18f, .14f, .025f));
                GUI.Label(badge, new GUIContent(_zoom >= .45f ? StageMakerPreview.ExtractionLabel(_preview.layout, cell.room.coord, true) : "E", tooltip), exitStyle);
            }
            if (_zoom >= .3f)
            {
                GUI.Label(new Rect(box.x - 12, box.yMax + 2, box.width + 24, 18 * _zoom + 6), cell.category, centerStyle);
                GUI.Label(new Rect(box.x - 12, box.yMax + 20 * _zoom + 4, box.width + 24, 18 * _zoom + 6),
                    cell.preset != null ? cell.preset.DisplayName : "미지정", nameStyle);
            }
            if (evt.type == EventType.MouseDown && evt.button == 0 && box.Contains(evt.mousePosition))
            { _selectedCell = cell; _roomDetailsOpen = true; _detailScroll = Vector2.zero; evt.Use(); Repaint(); }
        }
        GUI.EndGroup();
    }
    private static Color CellColor(StageMakerPreviewCell cell)
    {
        if (!string.IsNullOrEmpty(cell.error)) return new Color(.55f, .12f, .12f);
        if (!string.IsNullOrEmpty(cell.room.specialSectorPoolId)) return new Color(.4f, .23f, .55f);
        return cell.category switch
        {
            "보스" => new Color(.65f, .18f, .13f), "금고" => new Color(.55f, .4f, .08f),
            "해치" or "시작" => new Color(.05f, .45f, .4f), "발전실" => new Color(.5f, .45f, .1f),
            "상인" or "공용 전투 · 상인" => new Color(.15f, .42f, .22f), "스테이지 전용" => new Color(.12f, .35f, .6f),
            _ => new Color(.16f, .27f, .33f)
        };
    }
    private void DrawRoomDetails()
    {
        if (_selectedCell != null)
        {
            var cell = _selectedCell;
            GUILayout.Label($"{cell.room.coord} · {cell.category} · 시작점 경로 {cell.distance}", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("실제 선택된 Sector SO", cell.preset, typeof(SectorPresetSO), false);
                EditorGUILayout.ObjectField("섹터 프리팹", cell.preset != null ? cell.preset.SectorPrefab : null, typeof(SectorRuntime), false);
                EditorGUILayout.ObjectField("전투 Encounter", cell.encounter, typeof(StageBattleEncounterPresetSO), false);
            }
            GUILayout.Label("연결: " + cell.room.connections + (cell.locked ? " · 내부 잠금" : ""), EditorStyles.miniLabel);
            GUILayout.Label("종류별 Sprite는 왼쪽 [지도 Sprite · 전체 공통]에서 수정합니다.", EditorStyles.wordWrappedMiniLabel);
            string extraction = StageMakerPreview.ExtractionLabel(_preview.layout, cell.room.coord);
            if (!string.IsNullOrEmpty(extraction)) EditorGUILayout.HelpBox(extraction + " · 실제 외곽 탈출 포탈이 있는 방입니다.", MessageType.Info);
            if (!string.IsNullOrEmpty(cell.room.specialSectorPoolId))
                GUILayout.Label($"저장 ID: {cell.room.specialSectorPoolId}/{cell.room.specialSectorVariantId}", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(cell.error)) EditorGUILayout.HelpBox(cell.error, MessageType.Error);
            if (cell.preset != null && GUILayout.Button("선택된 맵 에셋 위치 보기")) EditorGUIUtility.PingObject(cell.preset);
        }
        else if (_preview != null && _preview.errors.Count > 0)
        {
            GUILayout.Label("참조/콘텐츠 오류 · 방을 클릭하면 상세 확인", EditorStyles.boldLabel);
            foreach (string error in _preview.errors.Take(12)) GUILayout.Label(error, EditorStyles.wordWrappedMiniLabel);
        }
        else GUILayout.Label("방을 좌클릭하면 실제 프리팹과 전투 설정을 확인합니다.", EditorStyles.wordWrappedLabel);
        foreach (string failure in _batchErrors.Take(12)) GUILayout.Label(failure, EditorStyles.wordWrappedMiniLabel);
    }
    private void StartBatch()
    {
        if (_selected == null) return;
        _batchFirstSeed = _seed; _batchDone = _batchGraphFailures = _batchContentFailures = _batchRoomTotal = 0;
        _batchTarget = _batchCount;
        _batchMin = int.MaxValue; _batchMax = 0; _batchErrors.Clear(); _batchCategories.Clear();
        _batchSourceJson = EditorJsonUtility.ToJson(_selected); _batchMessage = ""; _batchRunning = true;
    }
    private void TickBatch()
    {
        if (!_batchRunning || _selected == null || EditorApplication.isCompiling) return;
        if (_batchSourceJson != EditorJsonUtility.ToJson(_selected))
        { _batchRunning = false; _batchMessage = "설정이 변경되어 검증을 중단했습니다. 다시 실행하세요."; Repaint(); return; }
        int seed = unchecked(_batchFirstSeed + _batchDone);
        if (!StageMakerPreview.TryBuild(_selected, seed, _legacyStart, RequiredPreviewIds(), out var result, out string error))
        { _batchGraphFailures++; if (_batchErrors.Count < 20) _batchErrors.Add($"seed {seed}: {error}"); }
        else
        {
            int count = result.cells.Count; _batchRoomTotal += count; _batchMin = Mathf.Min(_batchMin, count); _batchMax = Mathf.Max(_batchMax, count);
            foreach (var pair in result.counts)
            { _batchCategories.TryGetValue(pair.Key, out int old); _batchCategories[pair.Key] = old + pair.Value; }
            if (result.errors.Count > 0)
            { _batchContentFailures++; if (_batchErrors.Count < 20) _batchErrors.Add($"seed {seed}: {result.errors[0]}"); }
        }
        _batchDone++;
        int graphs = _batchDone - _batchGraphFailures;
        _batchMessage = $"{_batchDone}회 · 구조 실패 {_batchGraphFailures} · 구조 성공 중 콘텐츠 오류 {_batchContentFailures}";
        if (graphs > 0) _batchMessage += $"\n방 수 {_batchMin}~{_batchMax} / 평균 {_batchRoomTotal / (float)graphs:0.0}\n" +
            string.Join(" / ", _batchCategories.Select(p => p.Key + " 평균 " + (p.Value / (float)graphs).ToString("0.0")));
        if (_batchDone >= _batchTarget) _batchRunning = false;
        Repaint();
    }
}
