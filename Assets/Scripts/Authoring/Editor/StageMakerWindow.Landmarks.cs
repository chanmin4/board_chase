using UnityEditor;
using UnityEngine;

public sealed partial class StageMakerWindow
{
    private bool _landmarkSettingsOpen=true;
    private void DrawLandmarkSettings()
    {
        _landmarkSettingsOpen=EditorGUILayout.Foldout(_landmarkSettingsOpen,"보장 랜드마크 · 새 생성 방식",true,EditorStyles.foldoutHeader);
        if(!_landmarkSettingsOpen)return;
        EditorGUILayout.HelpBox("랜드마크 ON: 특별/공용 비율 대신 등록한 랜드마크 전종 + 전용 일반맵을 생성합니다. 복합 프리팹은 1~4개의 SectorRuntime을 미리 이어 배치하고 내부 벽을 편집한 뒤 LandmarkPrefab.cells에 연결하세요. 플레이 중 벽/소품을 생성하지 않습니다.",MessageType.Info);
        EditorGUILayout.PropertyField(_serialized.FindProperty("_landmarks"),new GUIContent("랜드마크 보장 목록"),true);
        if(_selected.Landmarks.enabled)
            foreach(var definition in _selected.Landmarks.guaranteed)
            {
                if(definition==null)continue;
                EditorGUILayout.ObjectField(definition.displayName,definition,typeof(LandmarkDefinitionSO),false);
                foreach(var variant in definition.variants)
                    if(variant==null||!variant.TryValidate(out _))EditorGUILayout.HelpBox(definition.name+": 복합 프리팹의 칸/프리셋 연결을 확인하세요.",MessageType.Error);
            }
    }
}
