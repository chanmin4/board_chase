using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "StageProgressionRules",
    menuName = "Game/Stage/Stage Progression Rules")]
public class StageProgressionRulesSO : ScriptableObject
{
    [Serializable]
    public class StageProgressRule
    {
        [Tooltip("stage 번호입니다. 0은 StartSector 전용 시작 stage로 쓰고, 1부터 실제 NxN stage로 쓰는 구조입니다.")]
        public int stageIndex;

        [Tooltip("HUD나 결과 기록에 표시할 stage 이름입니다. 비어 있으면 코드에서 기본 stage 이름을 사용합니다.")]
        public string displayName;

        [Header("Stage Map")]
        [Tooltip("이 stage에서 생성할 일반 방 grid 크기입니다. 2면 2x2, 3이면 3x3입니다. 0은 StartSector만 쓰는 stage입니다.")]
        [Min(0)] public int roomGridSize = 1;

        [Tooltip("목표 방 좌표 (roomGridSize - 1, roomGridSize - 1)에 배치할 방 타입입니다. 현재 프로토타입은 Named 사용을 기준으로 합니다.")]
        public StageRoomType goalRoomType = StageRoomType.Named;

        [Header("Treasure Rooms")]
        [Tooltip("If enabled, this stage can replace some NormalBattle rooms with Treasure rooms.")]
        public bool enableTreasureRooms = true;

        [Min(0)]
        [Tooltip("Minimum Treasure rooms generated for this stage, except forced-stage overrides.")]
        public int treasureRoomMinCount = 1;

        [Min(0)]
        [Tooltip("Maximum Treasure rooms generated for this stage.")]
        public int treasureRoomMaxCount = 2;

        [Range(0f, 1f)]
        [Tooltip("Extra Treasure room chance added per consecutive previous no-hit stage. 0.1 = +10%.")]
        public float extraTreasureChancePerNoHitStage = 0.1f;

        [Range(0f, 1f)]
        [Tooltip("Maximum chance used when rolling each extra Treasure room slot.")]
        public float maxExtraTreasureChance = 1f;

        [Tooltip("If true, room (0,0) is not picked as Treasure unless there are not enough candidates.")]
        public bool excludeFirstRoomFromTreasure = true;

        [Header("Clear Requirement")]
        [Tooltip("현재 방을 Player 소유 상태로 유지해야 하는 시간입니다. 조건을 만족하는 동안 줄어들고, 0이 되면 방 클리어입니다.")]
        [Min(0f)] public float timerSeconds = 30f;

        [Tooltip("켜면 방 점유 조건을 잃었을 때 PlayerTimer가 처음 시간으로 리셋됩니다. 끄면 남은 시간이 유지됩니다.")]
        public bool resetTimerWhenRequirementLost = false;

        [Tooltip("이 stage에서 생성 방을 만들지 않고 StartSector 하나만 플레이 대상으로 씁니다.")]
        [FormerlySerializedAs("useStartSectorAsRequirement")]
        public bool useStartSectorOnly = false;

        [Header("Stage Completion")]
        [Tooltip("StartSector의 PlayerTimer가 끝났을 때 다음 stage로 진행할지 여부입니다. 보통 stage 0 시작방 전용으로 사용합니다.")]
        public bool advanceStageOnStartSectorComplete = false;

        [Tooltip("목표 방의 Named/Boss 전투가 끝났을 때 다음 stage로 진행할지 여부입니다.")]
        [FormerlySerializedAs("advanceStageOnNamedBattleComplete")]
        public bool advanceStageOnBossBattleComplete = true;

        [Tooltip("이 stage의 목표 방 전투 완료 시 다음 stage로 가지 않고 최종 승리 이벤트를 발생시킵니다.")]
        public bool isFinalStage = false;

        [Header("Stage Rest")]
        [Tooltip("stage 완료 후 다음 stage로 넘어가기 전 대기 시간입니다. HUD에는 Stage Break Time으로 표시됩니다.")]
        [Min(0f)] public float restSecondsBeforeNextStage = 5f;

        [Header("Reward")]
        [Tooltip("이 stage 목표 완료 시 Infection Control에 회복시킬 양입니다. 0이면 회복 보상을 주지 않습니다.")]
        [Min(0f)] public float infectionControlRecoverOnComplete = 0f;

        public StageTreasureRoomGenerationSettings CreateTreasureRoomGenerationSettings(
            int consecutiveNoHitStageCount)
        {
            return new StageTreasureRoomGenerationSettings(
                enableTreasureRooms,
                treasureRoomMinCount,
                treasureRoomMaxCount,
                consecutiveNoHitStageCount,
                extraTreasureChancePerNoHitStage,
                maxExtraTreasureChance,
                excludeFirstRoomFromTreasure);
        }
    }

    [Tooltip("stage별 진행 규칙 목록입니다. stageIndex 기준으로 현재 stage 규칙을 찾아 사용합니다.")]
    [SerializeField] private List<StageProgressRule> _rules = new();

    public bool TryGetRule(int stageIndex, out StageProgressRule rule)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            if (_rules[i] != null && _rules[i].stageIndex == stageIndex)
            {
                rule = _rules[i];
                return true;
            }
        }

        rule = null;
        return false;
    }

    public bool HasNextRule(int stageIndex)
    {
        return TryGetRule(stageIndex + 1, out _);
    }
}
