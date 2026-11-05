using System;
using System.Collections.Generic;

namespace VSplatter.StateMachine
{
    /// <summary>
    /// Owns lifecycle ordering, not Unity scheduling or gameplay. One transition per Tick;
    /// ancestor transitions have precedence. Equal-level ordering belongs to the node.
    /// Flat graphs deliberately retain the original transition-before-update contract.
    /// </summary>
    public sealed class HfsmRuntime
    {
        private readonly List<HfsmNode> _active = new();
        private readonly List<HfsmNode> _destination = new();
        private HfsmNode _entry;
        public IReadOnlyList<HfsmNode> ActivePath => _active;
        public HfsmNode Current => _active.Count > 0 ? _active[_active.Count - 1] : _entry;
        public bool IsEntered => _active.Count > 0;
        public bool IsSuspended { get; private set; }
        public event Action<HfsmNode, HfsmNode, StateExitReason> Transitioned;
        public event Action<HfsmNode, HfsmNode, StateExitReason> Transitioning;
        public event Action<HfsmNode> Entering;

        public void Reset(HfsmNode entry, bool exitCurrent = true)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (exitCurrent) ExitFrom(0, StateExitReason.Reset);
            else _active.Clear(); // Explicit legacy reset policy; never invent completion effects.
            _entry = entry;
            IsSuspended = false;
        }

        public void Enter()
        {
            if (IsEntered || IsSuspended || _entry == null) return;
            BuildDestination(_entry);
            EnterDestination(0);
        }

        public void Tick(float deltaTime)
        {
            if (IsSuspended) return;
            Enter();
            for (int i = 0; i < _active.Count; i++)
            {
                if (!_active[i].TryTransition(out var next) || next.Target == null) continue;
                Change(_active[i], next);
                break;
            }
            for (int i = 0; i < _active.Count; i++) _active[i].Tick(deltaTime);
        }

        public void Suspend() => IsSuspended = true;
        public void Resume() => IsSuspended = false;
        public void Stop(StateExitReason reason = StateExitReason.Stopped)
        { ExitFrom(0, reason); _entry = null; IsSuspended = false; }

        private void Change(HfsmNode source, HfsmTransition transition)
        {
            var previous = Current;
            BuildDestination(transition.Target);
            Transitioning?.Invoke(previous, transition.Target, transition.Reason);
            int common = 0;
            while (common < _active.Count && common < _destination.Count && _active[common] == _destination[common]) common++;
            // External self / ancestor-to-descendant transitions re-enter their source scope.
            int sourceIndex = _active.IndexOf(source);
            common = Math.Min(common, sourceIndex);
            ExitFrom(common, transition.Reason);
            EnterDestination(common);
            Transitioned?.Invoke(previous, Current, transition.Reason);
        }

        private void BuildDestination(HfsmNode target)
        {
            _destination.Clear();
            for (var node = target; node != null; node = node.Parent) _destination.Add(node);
            _destination.Reverse();
            for (var child = target.InitialChild; child != null; child = child.InitialChild)
                _destination.Add(child);
        }

        private void EnterDestination(int first)
        {
            for (int i = first; i < _destination.Count; i++)
            {
                var node = _destination[i];
                _active.Add(node);
                Entering?.Invoke(node);
                node.Enter();
            }
        }

        private void ExitFrom(int first, StateExitReason reason)
        {
            for (int i = _active.Count - 1; i >= first; i--)
            {
                var node = _active[i];
                _active.RemoveAt(i);
                node.Exit(reason);
            }
        }
    }
}
