/*
 * Role hierarchy: Gameplay > World > Sectors > ScriptableObject
 * Primary responsibility: Defines the StageRunFlowSO data asset used to configure runtime rules in the Inspector.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageRunFlow",
    menuName = "Game/Stage/Stage Run Flow")]
public sealed class StageRunFlowSO : ScriptableObject
{
    [Tooltip("Ordered stages in one complete run. The final non-null entry is the final stage.")]
    [SerializeField] private List<StageDefinitionSO> _stages = new();

    public IReadOnlyList<StageDefinitionSO> Stages => _stages;

    // The demo forks once and rejoins at 4. Later expedition routes are not enabled here.
    public IEnumerable<StageDefinitionSO> GetHatchDestinations(StageDefinitionSO current)
    {
        if (current == null) yield break;
        if (current.StageIndex == 1)
        {
            if (TryGetStage(2, out var second)) yield return second;
            if (TryGetStage(3, out var third)) yield return third;
        }
        else if (current.StageIndex == 2 || current.StageIndex == 3)
        {
            if (TryGetStage(4, out var fourth)) yield return fourth;
        }
    }

    // Purpose: Attempts to get stage and reports whether the request succeeded.
    public bool TryGetStage(int stageIndex, out StageDefinitionSO stage)
    {
        stage = null;

        if (_stages == null)
            return false;

        for (int i = 0; i < _stages.Count; i++)
        {
            StageDefinitionSO candidate = _stages[i];

            if (candidate != null && candidate.CanEnter && candidate.StageIndex == stageIndex)
            {
                stage = candidate;
                return true;
            }
        }

        return false;
    }

    // Purpose: Attempts to get next stage and reports whether the request succeeded.
    public bool TryGetNextStage(StageDefinitionSO current, out StageDefinitionSO next)
    {
        next = null;

        if (current == null || _stages == null)
            return false;

        for (int i = 0; i < _stages.Count; i++)
        {
            if (_stages[i] != current)
                continue;

            for (int j = i + 1; j < _stages.Count; j++)
            {
                if (_stages[j] == null || !_stages[j].CanEnter)
                    continue;

                next = _stages[j];
                return true;
            }

            return false;
        }

        return false;
    }

    // Purpose: Checks whether this component has next stage.
    public bool HasNextStage(StageDefinitionSO current)
    {
        return TryGetNextStage(current, out _);
    }

    // Purpose: Checks whether final stage is currently true.
    public bool IsFinalStage(StageDefinitionSO stage)
    {
        if (stage == null || _stages == null)
            return false;

        for (int i = _stages.Count - 1; i >= 0; i--)
        {
            if (_stages[i] != null && _stages[i].CanEnter)
                return _stages[i] == stage;
        }

        return false;
    }

#if UNITY_EDITOR
    // Purpose: Normalizes Inspector-authored data after it changes in the Editor.
    private void OnValidate()
    {
        if (_stages == null)
            return;

        HashSet<int> stageIndices = new();

        for (int i = 0; i < _stages.Count; i++)
        {
            StageDefinitionSO stage = _stages[i];

            if (stage == null)
            {
                Debug.LogError($"[StageRunFlowSO] Stage entry is empty. index={i}", this);
                continue;
            }

            if (!stageIndices.Add(stage.StageIndex))
            {
                Debug.LogError(
                    $"[StageRunFlowSO] Duplicate stage index. stage={stage.StageIndex}, index={i}",
                    this);
            }
        }
    }
#endif
}
