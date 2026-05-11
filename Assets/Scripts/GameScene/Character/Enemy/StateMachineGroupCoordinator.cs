using System;
using UnityEngine;
using UnityEngine.Serialization;
using VSplatter.StateMachine;
using VSplatter.StateMachine.ScriptableObjects;

[DisallowMultipleComponent]
[DefaultExecutionOrder(0)]
public class StateMachineGroupCoordinator : MonoBehaviour
{
    [Serializable]
    private struct ChildRule
    {
        [Tooltip("When Parent State Machine is currently in this state, this child state machine will be enabled.")]
        public StateSO parentState;

        [Tooltip("Child state machine controlled by this rule.")]
        public ChildStateMachine childStateMachine;

        [Tooltip("Usually true. Set false only if you want this rule to explicitly keep a child disabled.")]
        public bool enableChild;
    }

    [Header("References")]
    [Tooltip("State machine to observe. Can be ParentStateMachine or ChildStateMachine.")]
    [SerializeField] private StateMachine _parentStateMachine;

    [Header("Startup")]
    [Tooltip("Disable every managed child on Awake before rules are evaluated.")]
    [SerializeField] private bool _forceDisableChildrenOnAwake = true;

    [Header("Rules")]
    [Tooltip("Default state for children that do not match current parent state.")]
    [SerializeField] private bool _defaultChildEnabled = false;

    [Tooltip("Refresh rules every frame. Keep enabled unless you call Refresh manually after transitions.")]
    [SerializeField] private bool _refreshEveryFrame = true;

    [SerializeField] private ChildRule[] _childRules;

    [Header("Debug")]
    [ReadOnly] [SerializeField] private StateSO _currentParentState;
    [ReadOnly] [SerializeField] private int _enabledChildCount;

    private void Awake()
    {
        if (_forceDisableChildrenOnAwake)
            SetAllManagedChildren(false);
    }

    private void Start()
    {
        RefreshChildren();
    }

    private void Update()
    {
        if (_refreshEveryFrame)
            RefreshChildren();
    }

    [ContextMenu("Refresh Children")]
    public void RefreshChildren()
    {
        _currentParentState = GetCurrentParentState();
        _enabledChildCount = 0;

        if (_childRules == null)
            return;

        for (int i = 0; i < _childRules.Length; i++)
        {
            ChildStateMachine child = _childRules[i].childStateMachine;
            if (child == null)
                continue;

            bool shouldEnable = ResolveChildEnabled(_childRules[i], _currentParentState);
            ApplyChildStateMachineState(child, shouldEnable);

            if (child.enabled)
                _enabledChildCount++;
        }
    }

    private StateSO GetCurrentParentState()
    {
        return _parentStateMachine != null
            ? _parentStateMachine.CurrentStateSO
            : null;
    }

    private bool ResolveChildEnabled(ChildRule rule, StateSO currentParentState)
    {
        if (rule.parentState != null && rule.parentState == currentParentState)
            return rule.enableChild;

        return _defaultChildEnabled;
    }

    private void SetAllManagedChildren(bool enabled)
    {
        if (_childRules == null)
            return;

        for (int i = 0; i < _childRules.Length; i++)
        {
            ChildStateMachine child = _childRules[i].childStateMachine;
            if (child == null)
                continue;

            ApplyChildStateMachineState(child, enabled);
        }
    }

    private void ApplyChildStateMachineState(ChildStateMachine child, bool shouldEnable)
    {
        if (child.enabled != shouldEnable)
            child.enabled = shouldEnable;
    }
}
