/*
 * Role hierarchy: Gameplay > Entities > Creature > NamedEnemy > Action
 * Primary responsibility: Defines ForceStopNamedPatternActionSO state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using UnityEngine;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[CreateAssetMenu(
    fileName = "ForceStopNamedPatternAction",
    menuName = "State Machines/Named Enemy Actions/Force Stop Pattern")]
public class ForceStopNamedPatternActionSO : StateActionSO<ForceStopNamedPatternAction>
{
}

public class ForceStopNamedPatternAction : StateAction
{
    private NamedPatternController _patternController;
    private readonly List<INamedPatternRuntimeResetter> _runtimeResetters = new();

    // Purpose: Caches the component references required before runtime initialization.
    public override void Awake(StateMachine stateMachine)
    {
        _patternController = stateMachine.GetComponentInParent<NamedPatternController>();

        Transform searchRoot = _patternController != null
            ? _patternController.transform
            : stateMachine.transform.root;

        MonoBehaviour[] behaviours = searchRoot.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is INamedPatternRuntimeResetter resetter)
                _runtimeResetters.Add(resetter);
        }
    }

    // Purpose: Responds to the state enter callback.
    public override void OnStateEnter()
    {
        if (_patternController != null)
            _patternController.ForceStopPattern();

        for (int i = 0; i < _runtimeResetters.Count; i++)
            _runtimeResetters[i]?.ForceResetNamedPatternRuntime();
    }

    // Purpose: Responds to the update callback.
    public override void OnUpdate()
    {
    }
}
