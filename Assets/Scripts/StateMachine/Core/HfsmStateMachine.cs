using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine
{
    /// <summary>
    /// 캐릭터에 붙이는 유일한 HFSM 호스트입니다. 기존 행동/조건의 Awake(StateMachine) 계약도 유지합니다.
    /// 루트(-200), 연결 판정(0), 내부 행동(+200)을 공용 프레임 서비스로 분리해 실행 시점을 보존합니다.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-200)]
    [AddComponentMenu("State Machine/HFSM State Machine")]
    public sealed class HfsmStateMachine : StateMachine
    {
        [SerializeField] private HfsmDefinitionSO _definition;
        private readonly List<HfsmScopeRuntime> _inner = new();
        private bool _started;
        private HfsmDefinitionSO _compiledDefinition;
        private bool[] _activeGates = Array.Empty<bool>();
        public HfsmDefinitionSO Definition => _definition;
        public override TransitionTableSO Table => _definition != null ? _definition.RootTable : base.Table;
        public int ScopeCount => 1 + _inner.Count;
        internal override StateSO EntryOverride => _definition != null ? _definition.Entry : null;
        public IHfsmScope GetScope(int index) => index == 0 ? this : _inner[index - 1];
        private IEnumerable<(StateSO state, int level)> ActiveStates()
        {
            for (int i = 0; i < ScopeCount; i++)
            {
                var scope = GetScope(i); if (!scope.IsScopeActive) continue;
                for (int j = 0; j < scope.Runtime.ActivePath.Count; j++)
                    if (scope.Runtime.ActivePath[j] is State state) yield return (state.Definition, scope.Level + j);
            }
        }
        public StateSO GetActiveState(int level) => ActiveStates().FirstOrDefault(s => s.level == level).state;
        public StateSO CurrentLeafState => ActiveStates().OrderByDescending(s => s.level).FirstOrDefault().state;
        public string ActiveStatePath => string.Join(" > ", ActiveStates().Select(s => s.state.name + "_" + s.level));

        protected override void Awake()
        { if (_definition == null) return; ConfigureRoot(); base.Awake(); }
        protected override void Start()
        {
            if (_definition == null) { Debug.LogError("HFSM 그래프를 연결하세요.", this); enabled = false; return; }
            // Original child Awake preceded ALL Starts. Compile before root Enter, after scene Awakes.
            InitializeScopes(); _started = true; base.Start();
        }
        protected override void OnEnable()
        {
            base.OnEnable(); foreach (var scope in _inner) scope.ResumeHost();
            if (_definition != null) HfsmPhaseScheduler.Register(this);
        }
        protected override void OnDisable()
        { HfsmPhaseScheduler.Unregister(this); foreach (var scope in _inner) scope.SuspendHost(); base.OnDisable(); }
        private void OnDestroy() => HfsmPhaseScheduler.Unregister(this);
        protected override void Update() { if (_definition != null) base.Update(); }
        private void ConfigureRoot()
        {
            string[] errors = _definition.Validate().ToArray();
            if (errors.Length != 0) throw new InvalidOperationException(_definition.name + ": " + string.Join(" / ", errors));
            SetTransitionTable(_definition.RootTable);
        }
        private void InitializeScopes()
        {
            if (_compiledDefinition == _definition) return;
            ConfigureRoot(); _inner.Clear();
            for (int i = 0; i < _definition.Scopes.Count; i++)
                _inner.Add(new HfsmScopeRuntime(this, _definition.Scopes[i], _definition.ScopeLevel(i + 1)));
            _activeGates = new bool[_inner.Count];
            _compiledDefinition = _definition;
        }
        /// <summary>연결 판정 단계. 전투 조건/행동을 추가로 평가하지 않습니다.</summary>
        public void RefreshActiveScopes()
        {
            if (_definition == null) return; InitializeScopes();
            // State values are read in gate phase, never after an inner Tick. A disabled ancestor disables descendants.
            var active = _activeGates;
            for (int i = 0; i < _inner.Count; i++)
            {
                var binding = _definition.Scopes[i]; var owner = GetScope(binding.OwnerScope);
                bool ownerActive = binding.OwnerScope == 0 ? IsScopeActive : active[binding.OwnerScope - 1];
                active[i] = isActiveAndEnabled && ownerActive &&
                    owner.Runtime.ActivePath.Any(n => n is State state && state.Definition == binding.OwnerState);
            }
            for (int i = 0; i < _inner.Count; i++)
                _inner[i].SetActive(active[i], _definition.Scopes[i].Reentry == HfsmDefinitionSO.ReentryPolicy.RestartFromEntry);
        }
        public void TickInnerScopes(float deltaTime)
        { foreach (var scope in _inner) if (isActiveAndEnabled) scope.Tick(deltaTime); }
        internal void StartInnerScopes() { foreach (var scope in _inner) scope.StartIfActive(); }
        public override void ResetToInitialState(bool enterImmediately = true, bool exitCurrentState = false)
        {
            if (_definition != null) ConfigureRoot();
            // An explicit hierarchy reset cleans inner scopes first; legacy no-exit reset stays no-exit.
            if (exitCurrentState) for (int i = _inner.Count - 1; i >= 0; i--) _inner[i].Stop(StateExitReason.Reset);
            base.ResetToInitialState(enterImmediately, exitCurrentState);
            foreach (var scope in _inner) scope.Reset(false);
            if (_started) RefreshActiveScopes();
        }
        public override void StopMachine(StateExitReason reason = StateExitReason.Stopped)
        { for (int i = _inner.Count - 1; i >= 0; i--) _inner[i].Stop(reason); base.StopMachine(reason); }
    }
}
