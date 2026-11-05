using UnityEditor;
using UnityEngine;

public sealed partial class SectorMakerWindow
{
    [SerializeField] private bool _showInternalCombatLock;
    private void DrawInternalCombatLockTools()
    {
        _showInternalCombatLock = EditorGUILayout.Foldout(_showInternalCombatLock, "내부 잠금 전투 구역 · 사전 배치", true);
        if (!_showInternalCombatLock) return;
        EditorGUILayout.HelpBox("벽/패널/스폰 지점은 프리팹에 직접 미리 배치합니다. 실행 중 생성/자동 축소/재배치는 없습니다.\n내려가는 외형과 고정 차단 판정을 분리하세요. 판정은 하강 완료 순간에만 해제됩니다.", MessageType.Info);
        var sector = _activeSectorRuntime != null ? _activeSectorRuntime : _sectorRuntime;
        using (new EditorGUI.DisabledScope(!IsEditingRoom || sector == null || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var area = SectorLockedCombatArea.Find(sector);
            if (area == null)
            {
                EditorGUILayout.HelpBox("작성한 루트에 SectorLockedCombatArea를 추가하고, 네 방향 패널의 Area 참조를 연결하세요. Treasure_01을 사전 배치 예시로 사용할 수 있습니다.", MessageType.None);
                return;
            }
            if (GUILayout.Button("잠금 구역 Hierarchy 선택")) Selection.activeGameObject = area.gameObject;
            var data = new SerializedObject(area);
            data.Update();
            EditorGUILayout.PropertyField(data.FindProperty("_keyTier"), new GUIContent("필요 열쇠"));
            EditorGUILayout.PropertyField(data.FindProperty("_barrierRoot"), new GUIContent("내려가는 벽 외형"));
            EditorGUILayout.PropertyField(data.FindProperty("_blockingRoot"), new GUIContent("제자리 차단 판정"));
            EditorGUILayout.PropertyField(data.FindProperty("_perimeterEntry"), new GUIContent("외곽 테스트 진입점"));
            EditorGUILayout.PropertyField(data.FindProperty("_loweringSeconds"), new GUIContent("하강 시간"));
            EditorGUILayout.PropertyField(data.FindProperty("_lowerDistance"), new GUIContent("하강 거리"));
            EditorGUILayout.PropertyField(data.FindProperty("_interiorRatio"), new GUIContent("내부 범위 비율 · 스폰 검사만"));
            EditorGUILayout.PropertyField(data.FindProperty("_spawnInset"), new GUIContent("스폰 벽 여유"));
            data.ApplyModifiedProperties();
            if (!area.HasAuthoredLayout(out string reason)) EditorGUILayout.HelpBox(reason, MessageType.Warning);
        }
    }
}
