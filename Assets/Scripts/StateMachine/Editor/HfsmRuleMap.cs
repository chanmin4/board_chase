using System.Collections.Generic;
using System.Linq;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine.Editor
{
    /// <summary>표시 전용 묶음. 실행 상태/계층/행 순서를 생성하거나 변경하지 않습니다.</summary>
    internal static class HfsmRuleMap
    {
        internal sealed class Group
        {
            public SharedTransitionRuleSO Rule;
            public HfsmGraphModel.Node Target;
            public HfsmGraphPresentation.Link[] Usages;
            public string Key => Rule != null ? "shared/" + Rule.GetInstanceID() : "local/" + Usages[0].Edge.Scope.Key + "/" + Usages[0].Edge.Row;
            public string Title => Rule != null ? "공유 규칙 · " + Rule.DisplayName : "개별 전환 · " + HfsmGraphPresentation.StateLabel(Usages[0].Edge.From);
            public string Sources => string.Join(" / ", Usages.Select(l => HfsmGraphPresentation.StateLabel(l.Edge.From)).Distinct());
        }

        public static List<Group> Build(HfsmGraphModel.Scope root, HfsmGraphModel.Scope scope)
            => HfsmGraphPresentation.Links(root, scope)
                .GroupBy(l => (definition: l.Edge.Rule != null ? (object)l.Edge.Rule : l.Edge, target: l.To))
                .Select(g => new Group { Rule = g.First().Edge.Rule, Target = g.Key.target, Usages = g.ToArray() }).ToList();
    }
}
