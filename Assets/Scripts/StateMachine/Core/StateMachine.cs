/*
 * Role hierarchy: StateMachine > Core
 * Primary responsibility: Defines StateMachine state behavior or transitions that drive the runtime flow.
 * Execution boundary: Runtime (VSplatter.StateMachine)
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VSplatter.StateMachine
{
    public class StateMachine : MonoBehaviour
    {
        [Tooltip("Set the initial state of this StateMachine")]
        [SerializeField] private ScriptableObjects.TransitionTableSO _transitionTableSO = default;

        [Header("Debug")]
        [SerializeField] private bool _logStateChanges = false;
        [SerializeField] private string _debugOwnerLabel = string.Empty;
        [SerializeField] private bool _logInitialState = true;

#if UNITY_EDITOR
        [Space]
        [SerializeField]
        internal Debugging.StateMachineDebugger _debugger = default;
#endif

        private readonly Dictionary<Type, Component> _cachedComponents = new Dictionary<Type, Component>();
        internal State _currentState;
        private bool _hasEnteredCurrentState;
        private ScriptableObjects.StateSO _previousStateSO;
        private float _currentStateEnteredTime;
        private int _lastTransitionFrame = -1;

        public ScriptableObjects.StateSO CurrentStateSO => _currentState?._originSO;
        public ScriptableObjects.StateSO PreviousStateSO => _previousStateSO;
        public float CurrentStateEnteredTime => _currentStateEnteredTime;
        public float CurrentStateElapsedSeconds => Mathf.Max(0f, Time.time - _currentStateEnteredTime);
        public int LastTransitionFrame => _lastTransitionFrame;

        private string DebugOwnerName =>
            string.IsNullOrWhiteSpace(_debugOwnerLabel) ? gameObject.name : _debugOwnerLabel;

        // Purpose: Caches the component references required before runtime initialization.
        private void Awake()
        {
            ResetToInitialState(false);
        }

#if UNITY_EDITOR
        // Purpose: Activates runtime subscriptions and refreshes the enabled state.
        private void OnEnable()
        {
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        // Purpose: Responds to the after assembly reload callback.
        private void OnAfterAssemblyReload()
        {
            ResetToInitialState(false);
        }

        // Purpose: Releases active subscriptions and transient state while disabled.
        private void OnDisable()
        {
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }
#endif

        // Purpose: Completes initialization after scene dependencies are ready.
        private void Start()
        {
            EnterCurrentState();
        }

        // Purpose: Performs the reset to initial state workflow for this component.
        public void ResetToInitialState(bool enterImmediately = true, bool exitCurrentState = false)
        {
            if (exitCurrentState && _hasEnteredCurrentState && _currentState != null)
                _currentState.OnStateExit();

            _currentState = _transitionTableSO.GetInitialState(this);
            _hasEnteredCurrentState = false;
            _previousStateSO = null;
            _currentStateEnteredTime = Time.time;
            _lastTransitionFrame = Time.frameCount;

#if UNITY_EDITOR
            _debugger.Awake(this);
#endif

            if (enterImmediately && isActiveAndEnabled)
                EnterCurrentState();
        }

        // Purpose: Enters the current state state or area.
        private void EnterCurrentState()
        {
            if (_hasEnteredCurrentState || _currentState == null)
                return;

            _currentState.OnStateEnter();
            _hasEnteredCurrentState = true;

            if (_logStateChanges && _logInitialState)
            {
                Debug.Log(
                    $"[StateMachine] owner={DebugOwnerName}, machine={GetType().Name}, start={GetStateName(_currentState)}, frame={Time.frameCount}",
                    this);
            }
        }

        public new bool TryGetComponent<T>(out T component) where T : Component
        {
            Type type = typeof(T);

            if (_cachedComponents.TryGetValue(type, out Component cached))
            {
                component = cached as T;

                if (component != null)
                    return true;

                _cachedComponents.Remove(type);
            }

            if (base.TryGetComponent(out component))
            {
                _cachedComponents[type] = component;
                return true;
            }

            component = GetComponentInParent<T>();

            if (component != null)
            {
                _cachedComponents[type] = component;
                return true;
            }

            return false;
        }

        public T GetOrAddComponent<T>() where T : Component
        {
            if (TryGetComponent(out T component))
                return component;

            component = gameObject.AddComponent<T>();
            _cachedComponents[typeof(T)] = component;

            return component;
        }

        public new T GetComponent<T>() where T : Component
        {
            return TryGetComponent(out T component)
                ? component
                : throw new InvalidOperationException(
                    $"{typeof(T).Name} not found on {name} or its parent hierarchy.");
        }

        // Purpose: Processes this component's per-frame runtime work.
        private void Update()
        {
            if (_currentState == null)
            {
                Debug.LogError($"StateMachine has no current state. owner={name}", this);
                enabled = false;
                return;
            }

            EnterCurrentState();

            if (_currentState.TryGetTransition(out var transitionState))
                Transition(transitionState);

            _currentState.OnUpdate();
        }

        // Purpose: Performs the transition workflow for this component.
        private void Transition(State transitionState)
        {
            string previousStateName = GetStateName(_currentState);
            string nextStateName = GetStateName(transitionState);
            _previousStateSO = _currentState?._originSO;

            if (_hasEnteredCurrentState)
                _currentState.OnStateExit();

            _currentState = transitionState;
            _hasEnteredCurrentState = false;
            _currentStateEnteredTime = Time.time;
            _lastTransitionFrame = Time.frameCount;

            EnterCurrentState();

            if (_logStateChanges)
            {
                Debug.Log(
                    $"[StateMachine] owner={DebugOwnerName}, machine={GetType().Name}, {previousStateName} -> {nextStateName}, frame={Time.frameCount}",
                    this);
            }
        }

        // Purpose: Returns the current state name.
        private static string GetStateName(State state)
        {
            if (state == null || state._originSO == null)
                return "<null>";

            return state._originSO.name;
        }
    }
}
