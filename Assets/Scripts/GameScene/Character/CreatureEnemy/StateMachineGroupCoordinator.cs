using System;
using UnityEngine;
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

        [Tooltip("If true, this child state machine returns to its Transition Table initial state when this rule enables it.")]
        public bool resetOnEnable;
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
        _enabledChildCount = 0;

        if (_parentStateMachine == null || !_parentStateMachine.enabled)
        {
            _currentParentState = null;
            SetAllManagedChildren(false);
            return;
        }

        _currentParentState = GetCurrentParentState();

        if (_childRules == null)
            return;

        for (int i = 0; i < _childRules.Length; i++)
        {
            ChildStateMachine child = _childRules[i].childStateMachine;
            if (child == null)
                continue;

            bool shouldEnable = ResolveChildEnabled(_childRules[i], _currentParentState);
            ApplyChildStateMachineState(_childRules[i], shouldEnable);

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
            if (_childRules[i].childStateMachine == null)
                continue;

            ApplyChildStateMachineState(_childRules[i], enabled);
        }
    }

    private void ApplyChildStateMachineState(ChildRule rule, bool shouldEnable)
    {
        ChildStateMachine child = rule.childStateMachine;
        if (child == null)
            return;

        bool wasEnabled = child.enabled;

        if (wasEnabled != shouldEnable)
            child.enabled = shouldEnable;

        if (!wasEnabled && shouldEnable && rule.resetOnEnable)
            child.ResetToInitialState(true, false);
    }
}
