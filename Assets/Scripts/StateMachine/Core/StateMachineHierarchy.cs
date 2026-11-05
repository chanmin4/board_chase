using System.Collections.Generic;
using VSplatter.StateMachine.ScriptableObjects;

namespace VSplatter.StateMachine
{
    /// <summary>Read-only view of actual coordinator bindings, not a duplicate asset or clock.</summary>
    public interface IStateMachineHierarchySource
    {
        void GetHierarchyBindings(List<StateMachineChildBinding> bindings);
    }

    public readonly struct StateMachineChildBinding
    {
        public readonly StateMachine Parent;
        public readonly StateSO ParentState;
        public readonly StateMachine Child;
        public readonly bool Enabled;
        public readonly bool ResetOnEnable;
        public readonly bool DefaultEnabled;
        public StateMachineChildBinding(StateMachine parent, StateSO parentState, StateMachine child,
            bool enabled, bool resetOnEnable, bool defaultEnabled)
        { Parent = parent; ParentState = parentState; Child = child; Enabled = enabled; ResetOnEnable = resetOnEnable; DefaultEnabled = defaultEnabled; }
    }

    public static class StateMachineChildGate
    {
        /// <summary>Preserves old ordering: enable first, optional reset second.</summary>
        public static void Apply(StateMachine child, bool enabled, bool resetOnEnable = false)
        {
            if (child == null) return;
            bool wasEnabled = child.enabled;
            if (wasEnabled != enabled) child.enabled = enabled;
            if (!wasEnabled && enabled && resetOnEnable) child.ResetToInitialState(true, false);
        }
    }
}
