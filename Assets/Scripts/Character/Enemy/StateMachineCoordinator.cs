using System;
using UnityEngine;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[DisallowMultipleComponent]
[DefaultExecutionOrder(0)]
public class StateMachineCoordinator : MonoBehaviour
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

	private void Awake()
	{
		if (_forceDisableChildOnAwake && _childStateMachine != null)
			_childStateMachine.enabled = false;
	}

	private void Start()
	{
		RefreshChildStateMachine();
	}

	private void Update()
	{
		if (_refreshEveryFrame)
			RefreshChildStateMachine();
	}

	[ContextMenu("Refresh Child State Machine")]
	public void RefreshChildStateMachine()
	{
		_currentParentState = GetCurrentParentState();
		_resolvedChildEnabled = ResolveChildEnabled(_currentParentState);
		ApplyChildStateMachineState(_resolvedChildEnabled);
	}

	private StateSO GetCurrentParentState()
	{
		if (_parentStateMachine == null)
			return null;

		return _parentStateMachine.CurrentStateSO;
	}

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

	private void ApplyChildStateMachineState(bool shouldEnable)
	{
		if (_childStateMachine != null && _childStateMachine.enabled != shouldEnable)
			_childStateMachine.enabled = shouldEnable;

		_childStateMachineEnabled = _childStateMachine != null && _childStateMachine.enabled;
	}
}
