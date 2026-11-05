using System;

namespace VSplatter.StateMachine
{
    /// <summary>Unity-independent state contract. A node with an InitialChild is a composite.</summary>
    public abstract class HfsmNode
    {
        public HfsmNode Parent { get; private set; }
        public HfsmNode InitialChild { get; private set; }
        public virtual string Name => GetType().Name;

        public void SetParent(HfsmNode parent)
        {
            for (var ancestor = parent; ancestor != null; ancestor = ancestor.Parent)
                if (ancestor == this) throw new InvalidOperationException("HFSM parent cycle: " + Name);
            Parent = parent;
        }

        public void SetInitialChild(HfsmNode child)
        {
            if (child != null && child.Parent != this)
                throw new InvalidOperationException("An initial child must belong directly to its composite.");
            InitialChild = child;
        }

        public virtual void Enter() { }
        public abstract void Tick(float deltaTime);
        public virtual void Exit(StateExitReason reason) { }
        public virtual bool TryTransition(out HfsmTransition transition) { transition = default; return false; }
    }

    public enum StateExitReason { Transition, Interrupted, Reset, Stopped }

    public readonly struct HfsmTransition
    {
        public readonly HfsmNode Target;
        public readonly StateExitReason Reason;
        public HfsmTransition(HfsmNode target, StateExitReason reason = StateExitReason.Transition)
        { Target = target; Reason = reason; }
    }
}
