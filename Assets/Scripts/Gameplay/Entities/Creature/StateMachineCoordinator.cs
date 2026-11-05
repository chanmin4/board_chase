/*
 * Role hierarchy: Gameplay > Entities > Creature
 * Primary responsibility: Coordinates the StateMachineCoordinator subsystem lifecycle and collaborating objects.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System;
using UnityEngine;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[DisallowMultipleComponent]
[DefaultExecutionOrder(0)]
public class StateMachineCoordinator : MonoBehaviour, IStateMachineHierarchySource
{
	[Serializable]
	private struct ParentChildRule
	{
		[FormerlySerializedAs("modeState")]
		public StateSO parentState;
		[FormerlySerializedAs("enableAliveStateMachine")]
		public bool enableChildStateMachine;
	}

	[Header("References")]
	[FormerlySerializedAs("_modeStateMachine")]
	[SerializeField] private ParentStateMachine _parentStateMachine;
	[FormerlySerializedAs("_aliveStateMachine")]
	[SerializeField] private ChildStateMachine _childStateMachine;

	[Header("Startup")]
	[FormerlySerializedAs("_forceDisableAliveOnAwake")]
	[SerializeField] private bool _forceDisableChildOnAwake = true;

	[Header("Rules")]
	[FormerlySerializedAs("_defaultAliveEnabled")]
	[SerializeField] private bool _defaultChildEnabled = false;
	[SerializeField] private bool _refreshEveryFrame = true;
	[FormerlySerializedAs("_modeRules")]
	[SerializeField] private ParentChildRule[] _parentStateRules;

	[Header("Debug")]
	[FormerlySerializedAs("_currentModeState")]
	[ReadOnly] [SerializeField] private StateSO _currentParentState;
	[FormerlySerializedAs("_resolvedAliveEnabled")]
	[ReadOnly] [SerializeField] private bool _resolvedChildEnabled;
	[FormerlySerializedAs("_aliveStateMachineEnabled")]
	[ReadOnly] [SerializeField] private bool _childStateMachineEnabled;

	// Purpose: Caches the component references required before runtime initialization.
	private void Awake()
	{
		if (_forceDisableChildOnAwake && _childStateMachine != null)
			_childStateMachine.enabled = false;
	}

	// Purpose: Completes initialization after scene dependencies are ready.
	private void Start()
	{
		RefreshChildStateMachine();
	}

	// Purpose: Processes this component's per-frame runtime work.
	private void Update()
	{
		if (_refreshEveryFrame)
			RefreshChildStateMachine();
	}

	[ContextMenu("Refresh Child State Machine")]
	// Purpose: Refreshes the displayed or cached child state machine.
	public void RefreshChildStateMachine()
	{
		_currentParentState = GetCurrentParentState();
		_resolvedChildEnabled = ResolveChildEnabled(_currentParentState);
		ApplyChildStateMachineState(_resolvedChildEnabled);
	}

	// Purpose: Returns the current current parent state.
	private StateSO GetCurrentParentState()
	{
		if (_parentStateMachine == null)
			return null;

		return _parentStateMachine.CurrentStateSO;
	}

	// Purpose: Resolves the child enabled needed by this component.
	private bool ResolveChildEnabled(StateSO parentState)
	{
		if (_parentStateRules != null)
		{
			for (int i = 0; i < _parentStateRules.Length; i++)
			{
				if (_parentStateRules[i].parentState == parentState)
					return _parentStateRules[i].enableChildStateMachine;
			}
		}

		return _defaultChildEnabled;
	}

	// Purpose: Applies the requested child state machine state change.
	private void ApplyChildStateMachineState(bool shouldEnable)
	{
		StateMachineChildGate.Apply(_childStateMachine, shouldEnable);

		_childStateMachineEnabled = _childStateMachine != null && _childStateMachine.enabled;
	}

	public void GetHierarchyBindings(System.Collections.Generic.List<StateMachineChildBinding> bindings)
	{
		if (_parentStateRules == null) return;
		foreach (var rule in _parentStateRules)
			bindings.Add(new StateMachineChildBinding(_parentStateMachine, rule.parentState, _childStateMachine,
				rule.enableChildStateMachine, false, _defaultChildEnabled));
	}
}
