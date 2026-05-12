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

        public ScriptableObjects.StateSO CurrentStateSO => _currentState?._originSO;

        private string DebugOwnerName =>
            string.IsNullOrWhiteSpace(_debugOwnerLabel) ? gameObject.name : _debugOwnerLabel;

        private void Awake()
        {
            ResetToInitialState(false);
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void OnAfterAssemblyReload()
        {
            ResetToInitialState(false);
        }

        private void OnDisable()
        {
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }
#endif

        private void Start()
        {
            EnterCurrentState();
        }

        public void ResetToInitialState(bool enterImmediately = true, bool exitCurrentState = false)
        {
            if (exitCurrentState && _hasEnteredCurrentState && _currentState != null)
                _currentState.OnStateExit();

            _currentState = _transitionTableSO.GetInitialState(this);
            _hasEnteredCurrentState = false;

#if UNITY_EDITOR
            _debugger.Awake(this);
#endif

            if (enterImmediately && isActiveAndEnabled)
                EnterCurrentState();
        }

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

        private void Transition(State transitionState)
        {
            string previousStateName = GetStateName(_currentState);
            string nextStateName = GetStateName(transitionState);

            if (_hasEnteredCurrentState)
                _currentState.OnStateExit();

            _currentState = transitionState;
            _hasEnteredCurrentState = false;

            EnterCurrentState();

            if (_logStateChanges)
            {
                Debug.Log(
                    $"[StateMachine] owner={DebugOwnerName}, machine={GetType().Name}, {previousStateName} -> {nextStateName}, frame={Time.frameCount}",
                    this);
            }
        }

        private static string GetStateName(State state)
        {
            if (state == null || state._originSO == null)
                return "<null>";

            return state._originSO.name;
        }
    }
}
