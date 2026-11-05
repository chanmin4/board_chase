using UnityEngine;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine
{
    /// <summary>
    /// 행동/조건이 속한 실행 단위입니다. 같은 호스트를 사용해도 계층별 시간과 캐시는 섞지 않습니다.
    /// 기존 Awake(StateMachine)는 컴포넌트 조회 계약을 유지하고, 새 행동은 Context로 계층 상태를 읽습니다.
    /// </summary>
    public sealed class StateMachineContext
    {
        public StateMachine Owner { get; }
        public IHfsmScope Scope { get; }
        public StateSO CurrentState => Scope.CurrentStateSO;
        public float ElapsedSeconds => Scope.CurrentStateElapsedSeconds;
        internal StateMachineContext(StateMachine owner, IHfsmScope scope) { Owner = owner; Scope = scope; }
        public T GetComponent<T>() where T : Component => Owner.GetComponent<T>();
        public bool TryGetComponent<T>(out T component) where T : Component => Owner.TryGetComponent(out component);
#if UNITY_EDITOR
        internal Debugging.StateMachineDebugger Debugger { get; set; }
#endif
    }
}
