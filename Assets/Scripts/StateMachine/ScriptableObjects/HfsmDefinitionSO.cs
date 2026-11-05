using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VSplatter.StateMachine.ScriptableObjects
{
    /// <summary>
    /// 캐릭터 HFSM의 연결 지도입니다. 기존 테이블/SO를 참조하며 현재 상태·타이머를 저장하지 않습니다.
    /// 0번 실행 단위는 루트, Scopes[i]는 i+1번입니다. 소속은 앞에 정의한 실행 단위만 참조합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Character_HFSM", menuName = "State Machines/HFSM Definition")]
    public sealed class HfsmDefinitionSO : ScriptableObject
    {
        public enum ReentryPolicy { ResumePrevious, RestartFromEntry }
        [Serializable]
        public struct ScopeBinding
        {
            public string Label;
            public int OwnerScope;
            public StateSO OwnerState;
            public TransitionTableSO Table;
            public StateSO Entry;
            public ReentryPolicy Reentry;
        }
        [SerializeField] private TransitionTableSO _rootTable;
        [SerializeField] private StateSO _entry;
        [SerializeField] private ScopeBinding[] _scopes = Array.Empty<ScopeBinding>();
        public TransitionTableSO RootTable => _rootTable;
        public StateSO Entry => _entry != null ? _entry : _rootTable != null ? _rootTable.InitialState : null;
        public IReadOnlyList<ScopeBinding> Scopes => _scopes ?? Array.Empty<ScopeBinding>();
#if UNITY_EDITOR
        public static event Action DefinitionChanged;
        private void OnValidate() => DefinitionChanged?.Invoke();
#endif

        public IEnumerable<string> Validate()
        {
            if (_rootTable == null) yield return "1계층 테이블을 연결하세요.";
            else if (!Contains(_rootTable, Entry)) yield return "1계층 시작 상태가 해당 테이블에 없습니다.";
            for (int i = 0; i < Scopes.Count; i++)
            {
                var binding = Scopes[i];
                if (binding.OwnerScope < 0 || binding.OwnerScope > i)
                { yield return $"내부 연결 {i + 1}: 소속 실행 단위는 앞에 정의되어야 합니다. 순환 연결은 금지됩니다."; continue; }
                var ownerTable = binding.OwnerScope == 0 ? _rootTable : Scopes[binding.OwnerScope - 1].Table;
                if (binding.OwnerState == null || !Contains(ownerTable, binding.OwnerState))
                    yield return $"내부 연결 {i + 1}: 소속 상태를 해당 테이블에서 선택하세요.";
                if (binding.Table == null) yield return $"내부 연결 {i + 1}: 실행할 테이블이 없습니다.";
                else if (!Contains(binding.Table, binding.Entry != null ? binding.Entry : binding.Table.InitialState))
                    yield return $"내부 연결 {i + 1}: 시작 상태가 해당 테이블에 없습니다.";
            }
        }
        public static bool Contains(TransitionTableSO table, StateSO state)
            => table != null && state != null && (table.InitialState == state ||
                table.Transitions.Any(r => r.FromState == state ||
                    (r.UseSharedRule ? r.SharedRule != null && r.SharedRule.ToState == state : r.ToState == state)) ||
                table.Hierarchy.Any(h => h.State == state || h.Parent == state || h.InitialChild == state));

        public int ScopeLevel(int scopeIndex)
        {
            if (scopeIndex == 0) return 1;
            var binding = Scopes[scopeIndex - 1];
            if (binding.OwnerScope < 0 || binding.OwnerScope >= scopeIndex) return 1;
            var table = binding.OwnerScope == 0 ? RootTable : Scopes[binding.OwnerScope - 1].Table;
            int depth = 1; var state = binding.OwnerState; var visited = new HashSet<StateSO>();
            while (table != null && state != null && visited.Add(state))
            { state = table.Hierarchy.FirstOrDefault(h => h.State == state).Parent; if (state != null) depth++; }
            return ScopeLevel(binding.OwnerScope) + depth;
        }
    }
}
