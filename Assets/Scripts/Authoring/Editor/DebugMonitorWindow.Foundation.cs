using UnityEditor;
using UnityEngine;

public partial class DebugMonitorWindow
{
    private PassiveItemSO _foundationPassive;
    private string _foundationStatus;
    private void DrawProgressionFoundationControls(RaidRuntime raid)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("진행·전투 시스템 연결", EditorStyles.boldLabel);
        if (GUILayout.Button("진행·전투 공용 설정 열기 (스태미나·시야·특별 섹터·방탄·패시브)"))
            ProgressionFoundationWindow.OpenFor(raid != null ? raid.Definition : null);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || CombatLabRuntime.IsIsolatedSession))
        {
            var player = Object.FindFirstObjectByType<PlayerInventoryRuntime>();
            var passive = player != null ? player.GetComponent<PlayerPassiveInventoryRuntime>() : null;
            EditorGUILayout.LabelField("패시브 장착칸", passive != null ? $"{passive.UnlockedSlotCount}/{passive.MaxSlotCount}" : "플레이어 없음");
            _foundationPassive = (PassiveItemSO)EditorGUILayout.ObjectField("패시브 지급 원본", _foundationPassive, typeof(PassiveItemSO), false);
            using (new EditorGUI.DisabledScope(player == null || _foundationPassive == null))
                if (GUILayout.Button("패시브 1개 지급 · 랜덤 ON이면 새 개체 생성"))
                {
                    player.TryPickup(_foundationPassive, 1, out _foundationStatus);
                }
            using (new EditorGUI.DisabledScope(raid == null || player == null))
                if (GUILayout.Button("현재 해치 → 다음 스테이지 (실제 위치·클리어·전력·퀘스트 조건 적용)"))
                {
                    if (!raid.TryAdvanceThroughHatch(_sectorStateManager != null ? _sectorStateManager.CurrentSector : null, player))
                        _foundationStatus = raid.LastExtractionFailure;
                    else _foundationStatus = "다음 스테이지 이동 요청 수락";
                }
            if (raid != null && raid.Definition != null)
                EditorGUILayout.LabelField("해치 목적지", raid.Definition.RaidRuntime.hatchAdvancesStage ? "다음 스테이지" : "HOME (기존 모드)");
            var progression = Object.FindFirstObjectByType<StageProgressionManager>();
            if (progression != null && !string.IsNullOrEmpty(progression.LastHatchTransitionMessage))
                EditorGUILayout.HelpBox(progression.LastHatchTransitionMessage, MessageType.Info);
            if (raid?.State?.layout?.rooms != null)
                EditorGUILayout.LabelField("특별 섹터 배정", raid.State.layout.rooms.FindAll(x => x != null && !string.IsNullOrEmpty(x.specialSectorPoolId)).Count.ToString());
            var area = SectorLockedCombatArea.Find(_sectorStateManager != null ? _sectorStateManager.CurrentSector : null);
            using (new EditorGUI.DisabledScope(raid?.State?.active != true || _sectorStateManager == null || player == null))
                if (GUILayout.Button("잠금 구역 테스트 · 현재 원정의 잠긴 방 외곽으로 이동"))
                {
                    var room = raid.State.layout.rooms.Find(x => x != null && x.hasInternalCombatLock && !x.internalCombatUnlocked && !x.isCleared);
                    if (room == null) _foundationStatus = "이번 원정에는 미개방 내부 잠금방이 없습니다. 새 원정에서 금고가 생성되어야 합니다. 생성 확률/완료 상태는 바꾸지 않았습니다.";
                    else
                    {
                        _teleportCoordX = room.coord.x; _teleportCoordY = room.coord.y;
                        ExecuteCoordDebugMove();
                        _foundationStatus = $"이동 요청: {room.coord}. 패널에 접근해 E → Key Tool 열쇠 드래그 → 잠금 해제. 열쇠는 자동 지급/무료 사용 처리하지 않습니다.";
                    }
                }
            if (area != null && area.IsBound)
            {
                EditorGUILayout.LabelField("내부 잠금 구역", area.IsLocked ? "잠김 · 외곽 통행 가능" :
                    area.IsLowering ? "벽 하강 중 · 이동 잠김" : area.Sector.IsCleared ? "클리어 · 이동 가능" : "전투 중 · 이동 잠김");
                EditorGUILayout.LabelField("필요 열쇠", area.KeyTier.ToString());
            }
        }
        if (!string.IsNullOrEmpty(_foundationStatus)) EditorGUILayout.HelpBox(_foundationStatus, MessageType.Info);
    }
}
