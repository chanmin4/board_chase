using System;
using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine
{
    /// <summary>
    /// MonoBehaviour가 아닌 내부 계층 실행기. 컴파일 사전/조건 캐시/행동 인스턴스는 이 단위마다 별개입니다.
    /// 비활성화는 기존 ChildStateMachine과 동일하게 Exit 없는 Suspend입니다.
    /// </summary>
    internal sealed class HfsmScopeRuntime : IHfsmScope
    {
        private readonly StateMachine _owner;
        private readonly StateSO _entry;
        private bool _transitioning;
        private bool _hasStarted;
        private bool _gateActive;
        public string Label { get; }
        public int Level { get; }
        public TransitionTableSO Table { get; }
        public HfsmRuntime Runtime { get; } = new();
        public StateSO CurrentStateSO => (Runtime.Current as State)?.Definition;
        public StateSO PreviousStateSO { get; private set; }
        public float CurrentStateEnteredTime { get; private set; }
        public float CurrentStateElapsedSeconds => Mathf.Max(0, Time.time - CurrentStateEnteredTime);
        public int LastTransitionFrame { get; private set; } = -1;
        public bool IsScopeActive => _gateActive && _owner != null && _owner.isActiveAndEnabled && !Runtime.IsSuspended;
        public StateMachineContext Context { get; }

        public HfsmScopeRuntime(StateMachine owner, HfsmDefinitionSO.ScopeBinding definition, int level)
        {
            _owner = owner; _entry = definition.Entry; Table = definition.Table;
            Label = string.IsNullOrWhiteSpace(definition.Label) ? Table.name : definition.Label; Level = level;
            Context = new StateMachineContext(owner, this);
            Runtime.Transitioning += (from, to, reason) =>
            { PreviousStateSO = (from as State)?.Definition; _transitioning = true; };
            Runtime.Entering += node =>
            { if (_transitioning) { CurrentStateEnteredTime = Time.time; LastTransitionFrame = Time.frameCount; } };
            Runtime.Transitioned += (from, to, reason) => _transitioning = false;
            Reset(false);
        }
        public void Reset(bool enterImmediately, bool exitPrevious = false)
        {
            if (exitPrevious) Runtime.Stop(StateExitReason.Reset);
            var entry = StateGraphCompiler.Compile(Table, _owner, Context, _entry);
            Runtime.Reset(entry, false); _transitioning = false; _hasStarted = false;
            PreviousStateSO = null; CurrentStateEnteredTime = Time.time; LastTransitionFrame = Time.frameCount;
#if UNITY_EDITOR
            Context.Debugger ??= new Debugging.StateMachineDebugger();
            if (_owner.Context.Debugger != null)
            {
                Context.Debugger.debugTransitions = _owner.Context.Debugger.debugTransitions;
                Context.Debugger.appendActionsInfo = _owner.Context.Debugger.appendActionsInfo;
                Context.Debugger.appendConditionsInfo = _owner.Context.Debugger.appendConditionsInfo;
            }
            Context.Debugger.Awake(Context);
#endif
            if (_gateActive && _owner.isActiveAndEnabled) Runtime.Resume(); else Runtime.Suspend();
            if (enterImmediately && IsScopeActive) { Runtime.Enter(); _hasStarted = true; }
        }
        public void SetActive(bool active, bool restart)
        {
            if (active == _gateActive) return;
            _gateActive = active;
            if (!active) { Runtime.Suspend(); return; }
            // Legacy gate ordering: enable/resume first, optional reset second.
            Runtime.Resume();
            if (restart) Reset(true);
        }
        public void Tick(float deltaTime)
        {
            if (!IsScopeActive) return;
            _hasStarted = true; Runtime.Tick(deltaTime);
        }
        public void StartIfActive()
        { if (IsScopeActive && !_hasStarted) { Runtime.Enter(); _hasStarted = true; } }
        public void SuspendHost() => Runtime.Suspend();
        public void ResumeHost() { if (_gateActive) Runtime.Resume(); }
        public void Stop(StateExitReason reason)
        { Runtime.Stop(reason); _gateActive = false; }
    }
}
