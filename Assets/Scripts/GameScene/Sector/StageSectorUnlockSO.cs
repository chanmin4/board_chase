using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageSectorUnlock",
    menuName = "Game/Sector/Stage Sector Unlock")]
public class StageSectorUnlockSO : ScriptableObject
{
    [Serializable]
    public class StageUnlockStep
    {
        [Tooltip("이 단계가 몇 번째 스테이지인지")]
        public int stageIndex;

        [Tooltip("이 스테이지에서 새로 열릴 섹터 좌표들")]
        public Vector2Int[] sectorCoordsToOpen;
    }

    [Header("Stage Unlock Steps")]
    [SerializeField] private List<StageUnlockStep> steps = new List<StageUnlockStep>();

    /// <summary>
    /// 특정 stageIndex에 해당하는 해금 데이터를 가져온다.
    /// </summary>
    public bool TryGetStep(int stageIndex, out StageUnlockStep step)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].stageIndex == stageIndex)
            {
                step = steps[i];
                return true;
            }
        }

        step = null;
        return false;
    }

    /// <summary>
    /// 현재 테이블에 정의된 최대 스테이지 번호를 반환한다.
    /// </summary>
    public int GetMaxStageIndex()
    {
        int max = -1;

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null)
                max = Mathf.Max(max, steps[i].stageIndex);
        }

        return max;
    }
}