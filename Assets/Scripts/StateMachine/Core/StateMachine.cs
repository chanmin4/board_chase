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
    public class StateMachine : MonoBehaviour, IHfsmScope
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
        private readonly HfsmRuntime _runtime = new();
        private bool _runtimeConnected;
        private bool _transitionInProgress;
        private ScriptableObjects.StateSO _previousStateSO;
        private float _currentStateEnteredTime;
        private int _lastTransitionFrame = -1;
        private StateMachineContext _context;

        public StateMachineContext Context => _context ??= new StateMachineContext(this, this);
        public virtual string Label => _transitionTableSO != null ? _transitionTableSO.name : name;
        public virtual int Level => 1;
        public bool IsScopeActive => isActiveAndEnabled && !_runtime.IsSuspended;
        internal virtual ScriptableObjects.StateSO EntryOverride => null;
        protected void SetTransitionTable(ScriptableObjects.TransitionTableSO table) => _transitionTableSO = table;

        public ScriptableObjects.StateSO CurrentStateSO => _currentState?._originSO;
        public ScriptableObjects.StateSO PreviousStateSO => _previousStateSO;
        public float CurrentStateEnteredTime => _currentStateEnteredTime;
        public float CurrentStateElapsedSeconds => Mathf.Max(0f, Time.time - _currentStateEnteredTime);
        public int LastTransitionFrame => _lastTransitionFrame;
        public virtual ScriptableObjects.TransitionTableSO Table => _transitionTableSO;
        public HfsmRuntime Runtime => _runtime;

        private string DebugOwnerName =>
            string.IsNullOrWhiteSpace(_debugOwnerLabel) ? gameObject.name : _debugOwnerLabel;

        // Purpose: Caches the component references required before runtime initialization.
        protected virtual void Awake()
        {
            ResetToInitialState(false);
        }

        // Purpose: Activates runtime subscriptions and refreshes the enabled state.
        protected virtual void OnEnable()
        {
            _runtime.Resume();
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
#endif
        }

#if UNITY_EDITOR
        // Purpose: Responds to the after assembly reload callback.
        private void OnAfterAssemblyReload()
        {
            ResetToInitialState(false);
        }
#endif

        // Purpose: Releases active subscriptions and transient state while disabled.
        protected virtual void OnDisable()
        {
            // Compatibility: enabled=false historically paused without Exit or re-entry.
            _runtime.Suspend();
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
#endif
        }

        // Purpose: Completes initialization after scene dependencies are ready.
        protected virtual void Start()
        {
            EnterCurrentState();
        }

        // Purpose: Performs the reset to initial state workflow for this component.
        public virtual void ResetToInitialState(bool enterImmediately = true, bool exitCurrentState = false)
        {
            ConnectRuntime();
            // Exit before building new action instances, matching the original reset order.
            if (exitCurrentState) _runtime.Stop(StateExitReason.Reset);
            _currentState = _transitionTableSO.GetInitialState(this);
            _runtime.Reset(_currentState, false);
            _transitionInProgress = false;
            _previousStateSO = null;
            _currentStateEnteredTime = Time.time;
            _lastTransitionFrame = Time.frameCount;

#if UNITY_EDITOR
            Context.Debugger = _debugger ??= new Debugging.StateMachineDebugger();
            _debugger.Awake(Context);
#endif

            if (enterImmediately && isActiveAndEnabled)
                EnterCurrentState();
        }

        // Purpose: Enters the current state state or area.
        private void EnterCurrentState()
        {
            if (_currentState != null) _runtime.Enter();
        }

        private void ConnectRuntime()
        {
            if (_runtimeConnected) return;
            _runtimeConnected = true;
            _runtime.Transitioning += (previous, next, reason) =>
            {
                _previousStateSO = (previous as State)?._originSO;
                _transitionInProgress = true;
            };
            _runtime.Entering += node =>
            {
                _currentState = node as State;
                if (_transitionInProgress)
                {
                    _currentStateEnteredTime = Time.time;
                    _lastTransitionFrame = Time.frameCount;
                }
                else if (_logStateChanges && _logInitialState)
                    Debug.Log($"[StateMachine] owner={DebugOwnerName}, machine={GetType().Name}, start={node.Name}, frame={Time.frameCount}", this);
            };
            _runtime.Transitioned += (previous, next, reason) =>
            {
                _transitionInProgress = false;
                if (_logStateChanges)
                    Debug.Log($"[StateMachine] owner={DebugOwnerName}, machine={GetType().Name}, {previous?.Name} -> {next?.Name}, frame={Time.frameCount}", this);
            };
        }

        /// <summary>Explicit cancellation, unlike legacy enabled=false (suspend).</summary>
        public virtual void StopMachine(StateExitReason reason = StateExitReason.Stopped)
        { _runtime.Stop(reason); _currentState = null; enabled = false; }

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
        protected virtual void Update()
        {
            if (_currentState == null)
            {
                Debug.LogError($"StateMachine has no current state. owner={name}", this);
                enabled = false;
                return;
            }

            _runtime.Tick(Time.deltaTime);
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
