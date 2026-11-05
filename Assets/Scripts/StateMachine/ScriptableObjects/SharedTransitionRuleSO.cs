using System;
using UnityEngine;

namespace VSplatter.StateMachine.ScriptableObjects
{
    /// <summary>
    /// 공유 전환의 작성 데이터입니다. 실행 상태나 BT 노드가 아닙니다.
    /// 대상/조건/종료 사유만 소유하며 적용 상태와 검사 순서는 각 테이블의 행이 소유합니다.
    /// 캐릭터의 타겟, 타이머, 행동 인스턴스, 조건 캐시는 절대 여기에 저장하지 않습니다.
    /// </summary>
    public sealed class SharedTransitionRuleSO : ScriptableObject
    {
        [Tooltip("그래프에 표시할 설명용 이름입니다. 파일 이름과 실행 조건은 바꾸지 않습니다.")]
        [SerializeField] private string _displayName;
        [SerializeField] private StateSO _toState;
        [SerializeField] private TransitionTableSO.ConditionUsage[] _conditions = Array.Empty<TransitionTableSO.ConditionUsage>();
        [SerializeField] private bool _always;
        [SerializeField] private StateExitReason _exitReason;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public StateSO ToState => _toState;
        public TransitionTableSO.ConditionUsage[] Conditions => _conditions;
        public bool Always => _always;
        public StateExitReason ExitReason => _exitReason;

        internal TransitionTableSO.TransitionItem Resolve(TransitionTableSO.TransitionItem usage)
        {
            usage.ToState = _toState;
            usage.Conditions = _conditions;
            usage.Always = _always;
            usage.ExitReason = _exitReason;
            return usage; // Source, local priority and original slot are unchanged.
        }

#if UNITY_EDITOR
        public static event Action DefinitionChanged;
        private void OnValidate() => DefinitionChanged?.Invoke();
        public void SetDisplayNameForAuthoring(string displayName) => _displayName = displayName;
        /// <summary>명시적인 작성 작업 전용. 조건 배열을 복사해 원본 테이블과 편집 배열을 공유하지 않습니다.</summary>
        public void InitializeForAuthoring(TransitionTableSO.TransitionItem source)
        {
            _toState = source.ToState;
            _conditions = source.Conditions != null
                ? (TransitionTableSO.ConditionUsage[])source.Conditions.Clone() : null;
            _always = source.Always;
            _exitReason = source.ExitReason;
        }
#endif
    }
}
