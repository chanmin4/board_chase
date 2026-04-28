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

		public ScriptableObjects.StateSO CurrentStateSO => _currentState?._originSO;

		private string DebugOwnerName =>
			string.IsNullOrWhiteSpace(_debugOwnerLabel) ? gameObject.name : _debugOwnerLabel;

		private void Awake()
		{
			_currentState = _transitionTableSO.GetInitialState(this);
#if UNITY_EDITOR
			_debugger.Awake(this);
#endif
		}

#if UNITY_EDITOR
		private void OnEnable()
		{
			UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
		}

		private void OnAfterAssemblyReload()
		{
			_currentState = _transitionTableSO.GetInitialState(this);
			_debugger.Awake(this);
		}

		private void OnDisable()
		{
			UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
		}
#endif

		private void Start()
		{
			_currentState.OnStateEnter();

			if (_logStateChanges && _logInitialState)
			{
				Debug.Log(
					$"[StateMachine] owner={DebugOwnerName}, machine={GetType().Name}, start={GetStateName(_currentState)}, frame={Time.frameCount}",
					this);
			}
		}

		public new bool TryGetComponent<T>(out T component) where T : Component
		{
			var type = typeof(T);
			if (!_cachedComponents.TryGetValue(type, out var value))
			{
				if (base.TryGetComponent<T>(out component))
					_cachedComponents.Add(type, component);

				return component != null;
			}

			component = (T)value;
			return true;
		}

		public T GetOrAddComponent<T>() where T : Component
		{
			if (!TryGetComponent<T>(out var component))
			{
				component = gameObject.AddComponent<T>();
				_cachedComponents.Add(typeof(T), component);
			}

			return component;
		}

		public new T GetComponent<T>() where T : Component
		{
			return TryGetComponent(out T component)
				? component : throw new InvalidOperationException($"{typeof(T).Name} not found in {name}.");
		}

		private void Update()
		{
			if (_currentState.TryGetTransition(out var transitionState))
				Transition(transitionState);

			_currentState.OnUpdate();
		}

		private void Transition(State transitionState)
		{
			string previousStateName = GetStateName(_currentState);
			string nextStateName = GetStateName(transitionState);

			_currentState.OnStateExit();
			_currentState = transitionState;
			_currentState.OnStateEnter();

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
