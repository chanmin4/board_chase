using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed partial class StageMakerWindow
{
    [SerializeField] private bool _showMapImages = true, _showGlobalMapImages, _mapImageSourcesOpen;
    [SerializeField] private SectorMapIconSetSO _sharedMapImages;
    [SerializeField] private MiniMapUISettingsSO _miniMapImages;
    [SerializeField] private FullMapUISettingsSO _fullMapImages;
    private bool _mapImagesSearched;
    private Vector2 _mapImageScroll;
    private readonly HashSet<Object> _editedMapImageAssets = new();
    private static readonly (SectorMapIconKind kind, string label, string field)[] MapIconFields =
    {
        (SectorMapIconKind.NormalCombat, "일반 전투방 · 공용", "_normalCombat"),
        (SectorMapIconKind.ThemedCombat, "스테이지 전용 전투방", "_themedCombat"),
        (SectorMapIconKind.Boss, "보스방", "_boss"), (SectorMapIconKind.Vault, "금고 / 잠금보관실", "_vault"),
        (SectorMapIconKind.Office, "사무실", "_office"), (SectorMapIconKind.Warehouse, "창고", "_warehouse"),
        (SectorMapIconKind.Armory, "보안실 / 무기고", "_armory"), (SectorMapIconKind.Medical, "응급의료실", "_medical"),
        (SectorMapIconKind.Server, "서버실", "_server"), (SectorMapIconKind.Generator, "발전실", "_generator"),
        (SectorMapIconKind.Hatch, "해치 / 시작방", "_hatch"), (SectorMapIconKind.Extraction, "외곽 탈출구", "_extraction"),
        (SectorMapIconKind.Merchant, "상인", "_merchant"), (SectorMapIconKind.Unknown, "미발견 / 잠금", "_unknown"),
        (SectorMapIconKind.Named, "네임드", "_named")
    };

    private void EnsureMapImageSettings()
    {
        if (_mapImagesSearched) return;
        _mapImagesSearched = true;
        T Unique<T>() where T : Object
        {
            var ids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            return ids.Length == 1 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(ids[0])) : null;
        }
        if (_miniMapImages == null) _miniMapImages = Unique<MiniMapUISettingsSO>();
        if (_fullMapImages == null) _fullMapImages = Unique<FullMapUISettingsSO>();
        if (_sharedMapImages == null) _sharedMapImages = _miniMapImages != null ? _miniMapImages.SectorIcons : null;
        if (_sharedMapImages == null) _sharedMapImages = _fullMapImages != null ? _fullMapImages.SectorIcons : null;
        if (_sharedMapImages == null) _sharedMapImages = Unique<SectorMapIconSetSO>();
    }

    private void SaveMapImageAssets()
    {
        if (_showGlobalMapImages && _sharedMapImages != null) AssetDatabase.SaveAssetIfDirty(_sharedMapImages);
        foreach (var asset in _editedMapImageAssets) if (asset != null) AssetDatabase.SaveAssetIfDirty(asset);
        // Keep references for a later Undo -> Save; do not save unrelated dirty project assets.
    }

    private void DrawGlobalMapImages(float width)
    {
        EnsureMapImageSettings();
        GUILayout.Label("공통 지도 Sprite · 종류별 통합 설정", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("스테이지별 설정이 아닙니다. 여기서 바꾸면 모든 스테이지의 미니맵·전체맵·Stage Maker 미리보기에 함께 적용됩니다. 개별 프리팹이나 생성된 방을 수정하지 않습니다.", MessageType.Info);
        _mapImageScroll = EditorGUILayout.BeginScrollView(_mapImageScroll);
        using (new EditorGUI.DisabledScope(!CanWrite))
            _sharedMapImages = (SectorMapIconSetSO)EditorGUILayout.ObjectField("공통 Sprite SO", _sharedMapImages, typeof(SectorMapIconSetSO), false);
        var previewStage = (StageDefinitionSO)EditorGUILayout.ObjectField("미리보기 Stage", _selected, typeof(StageDefinitionSO), false);
        if (previewStage != _selected) { Select(previewStage); _showGlobalMapImages = true; }
        if (_sharedMapImages == null)
            EditorGUILayout.HelpBox("SectorMapIcons 공통 SO를 지정하세요. 연결된 설정 SO는 아래 고급 항목에서 확인할 수 있습니다.", MessageType.Warning);
        else
        {
            var data = new SerializedObject(_sharedMapImages);
            int columns = width >= 680 ? 2 : 1;
            using (new EditorGUI.DisabledScope(!CanWrite))
            {
                for (int i = 0; i < MapIconFields.Length; i += columns)
                using (new EditorGUILayout.HorizontalScope())
                    for (int column = 0; column < columns && i + column < MapIconFields.Length; column++)
                    {
                        var row = MapIconFields[i + column];
                        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Width((width - 30) / columns)))
                        {
                            Rect thumbnail = GUILayoutUtility.GetRect(58, 58, GUILayout.Width(58), GUILayout.Height(58));
                            EditorGUI.DrawRect(thumbnail, new Color(.06f, .08f, .1f));
                            Sprite image = data.FindProperty(row.field).objectReferenceValue as Sprite;
                            DrawMapSprite(thumbnail, image != null ? image : _sharedMapImages.GetIcon(row.kind));
                            using (new EditorGUILayout.VerticalScope())
                            {
                                GUILayout.Label(row.label, EditorStyles.boldLabel);
                                EditorGUILayout.PropertyField(data.FindProperty(row.field), GUIContent.none);
                                bool inheritsNormal = row.kind is SectorMapIconKind.ThemedCombat or SectorMapIconKind.Office or SectorMapIconKind.Warehouse or SectorMapIconKind.Server;
                                GUILayout.Label(image != null ? "미니맵 + 전체맵 공통" : inheritsNormal ? "비우면 일반 전투방 이미지 사용" : "비우면 이미지 표시 안 함", EditorStyles.wordWrappedMiniLabel);
                            }
                        }
                    }
                if (data.ApplyModifiedProperties())
                { _editedMapImageAssets.Add(_sharedMapImages); EditorUtility.SetDirty(_sharedMapImages); Repaint(); }
                if (GUILayout.Button("공통 Sprite 저장", GUILayout.Height(28))) SaveMapImageAssets();
            }
        }
        bool linked = _sharedMapImages != null && _miniMapImages != null && _fullMapImages != null &&
            _miniMapImages.SectorIcons == _sharedMapImages && _fullMapImages.SectorIcons == _sharedMapImages;
        if (!linked) EditorGUILayout.HelpBox("선택한 공통 SO가 양쪽 지도에 연결되지 않았습니다. 아래 '지도 SO 연결'에서 연결해주세요. 현재는 편집 미리보기만 바뀔 수 있습니다.", MessageType.Warning);
        _mapImageSourcesOpen = EditorGUILayout.Foldout(_mapImageSourcesOpen, "지도 SO 연결 · 고급", true);
        if (_mapImageSourcesOpen)
        using (new EditorGUI.DisabledScope(!CanWrite))
        {
            _miniMapImages = (MiniMapUISettingsSO)EditorGUILayout.ObjectField("미니맵 설정 SO", _miniMapImages, typeof(MiniMapUISettingsSO), false);
            _fullMapImages = (FullMapUISettingsSO)EditorGUILayout.ObjectField("전체맵 설정 SO", _fullMapImages, typeof(FullMapUISettingsSO), false);
            using (new EditorGUI.DisabledScope(_sharedMapImages == null || _miniMapImages == null || _fullMapImages == null))
            if (GUILayout.Button("이 공통 Sprite SO를 미니맵·전체맵에 연결"))
            {
                foreach (var target in new Object[] { _miniMapImages, _fullMapImages })
                {
                    var source = new SerializedObject(target); source.FindProperty("_sectorIcons").objectReferenceValue = _sharedMapImages;
                    if (source.ApplyModifiedProperties()) { _editedMapImageAssets.Add(target); EditorUtility.SetDirty(target); }
                }
            }
        }
        GUILayout.Label("Sprite를 드래그해서 교체 · Undo 지원 · 맵 재생성 없이 이미지 즉시 반영", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
    }

    private Sprite PreviewMapImage(StageMakerPreviewCell cell, out Sprite extractionImage)
    {
        EnsureMapImageSettings();
        var snapshot = new SectorMapCellSnapshot
        {
            coord = cell.room.coord, roomType = cell.room.roomType, isRevealed = true,
            isStartSector = cell.room.coord == _preview.layout.startSectorCoord,
            isExtraction = _preview.layout.IsExtractionSector(cell.room.coord),
            raidRole = cell.room.hasMerchant ? RaidRoomRole.Merchant :
                cell.room.raidRole == RaidRoomRole.Boss && !_preview.layout.bossSpawned ? RaidRoomRole.Normal : cell.room.raidRole,
            mapIconKind = SectorMapIconPolicy.KindForRoom(cell.room)
        };
        extractionImage = SectorMapIconPolicy.ExtractionOverlay(snapshot, _sharedMapImages,
            _miniMapImages != null ? _miniMapImages.GetLandmarkIcon(RaidRoomRole.Extraction, true) : null);
        var room = SectorMapIconPolicy.RoomIconSnapshot(snapshot);
        return SectorMapIconPolicy.SharedIcon(room, _sharedMapImages, SectorMapIconPolicy.MiniMapIcon(room, _miniMapImages));
    }

    private void DrawMapSprite(Rect bounds, Sprite sprite)
    {
        if (sprite == null || Event.current.type != EventType.Repaint) return;
        // Atlas-packed/tight sprites use Unity's thumbnail renderer instead of assuming their texture rectangle.
        if (sprite.packed)
        {
            var thumbnail = AssetPreview.GetAssetPreview(sprite) ?? AssetPreview.GetMiniThumbnail(sprite);
            if (thumbnail != null) GUI.DrawTexture(bounds, thumbnail, ScaleMode.ScaleToFit, true);
            if (AssetPreview.IsLoadingAssetPreview(sprite.GetInstanceID())) Repaint();
            return;
        }
        Rect source = sprite.rect;
        if (sprite.texture == null || source.width <= 0 || source.height <= 0) return;
        float scale = Mathf.Min(bounds.width / source.width, bounds.height / source.height);
        var draw = new Rect(bounds.center.x - source.width * scale * .5f, bounds.center.y - source.height * scale * .5f,
            source.width * scale, source.height * scale);
        GUI.DrawTextureWithTexCoords(draw, sprite.texture, new Rect(source.x / sprite.texture.width, source.y / sprite.texture.height,
            source.width / sprite.texture.width, source.height / sprite.texture.height), true);
    }
}
