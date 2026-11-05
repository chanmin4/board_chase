using System;
using System.Collections.Generic;
using NUnit.Framework;
using VSplatter.StateMachine;

public sealed class HfsmRuntimeTests
{
    private sealed class Node : HfsmNode
    {
        private readonly string _name;
        private readonly List<string> _trace;
        public Func<HfsmTransition?> Next;
        public Node(string name, List<string> trace) { _name = name; _trace = trace; }
        public override string Name => _name;
        public override void Enter() => _trace.Add("enter " + Name);
        public override void Tick(float dt) => _trace.Add("tick " + Name);
        public override void Exit(StateExitReason reason) => _trace.Add("exit " + Name + " " + reason);
        public override bool TryTransition(out HfsmTransition transition)
        { var next = Next?.Invoke(); transition = next ?? default; return next.HasValue; }
    }

    [Test] public void TransitionBeforeTick_OnlyDestinationUpdates_OneTransitionPerTick()
    {
        var t = new List<string>(); var a = new Node("a", t); var b = new Node("b", t); var c = new Node("c", t);
        a.Next = () => new HfsmTransition(b); b.Next = () => new HfsmTransition(c);
        var r = new HfsmRuntime(); r.Reset(a); r.Tick(.1f);
        CollectionAssert.AreEqual(new[] { "enter a", "exit a Transition", "enter b", "tick b" }, t);
        Assert.AreSame(b, r.Current);
    }

    [Test] public void SelfTransition_Reenters_InsteadOfBeingIgnored()
    {
        var t = new List<string>(); var a = new Node("a", t); a.Next = () => new HfsmTransition(a);
        var r = new HfsmRuntime(); r.Reset(a); r.Tick(0);
        CollectionAssert.AreEqual(new[] { "enter a", "exit a Transition", "enter a", "tick a" }, t);
    }

    [Test] public void SuspendResume_DoesNotExitOrReenter_AndKeepsCurrentState()
    {
        var t = new List<string>(); var a = new Node("a", t); var r = new HfsmRuntime(); r.Reset(a); r.Enter();
        r.Suspend(); r.Tick(0); r.Enter(); Assert.AreEqual(1, t.Count);
        r.Resume(); r.Tick(0); CollectionAssert.AreEqual(new[] { "enter a", "tick a" }, t);
    }

    [Test] public void LegacyReset_DoesNotSynthesizeExit()
    {
        var t = new List<string>(); var r = new HfsmRuntime(); var a = new Node("a", t); var b = new Node("b", t);
        r.Reset(a); r.Enter(); r.Reset(b, false); r.Enter();
        CollectionAssert.AreEqual(new[] { "enter a", "enter b" }, t);
    }

    [Test] public void ExplicitReset_ExitsPreviousBeforeEnteringNext()
    {
        var t = new List<string>(); var r = new HfsmRuntime(); r.Reset(new Node("a", t)); r.Enter();
        r.Reset(new Node("b", t)); r.Enter();
        CollectionAssert.AreEqual(new[] { "enter a", "exit a Reset", "enter b" }, t);
    }

    [Test] public void Composite_EntersOutsideIn_StopsInsideOut()
    {
        var t = new List<string>(); var alive = new Node("alive", t); var combat = new Node("combat", t); var attack = new Node("attack", t);
        combat.SetParent(alive); attack.SetParent(combat); alive.SetInitialChild(combat); combat.SetInitialChild(attack);
        var r = new HfsmRuntime(); r.Reset(alive); r.Enter(); r.Stop(StateExitReason.Interrupted);
        CollectionAssert.AreEqual(new[] { "enter alive", "enter combat", "enter attack", "exit attack Interrupted", "exit combat Interrupted", "exit alive Interrupted" }, t);
        Assert.IsNull(r.Current);
    }

    [Test] public void SiblingTransition_DoesNotReenterSharedParent()
    {
        var t = new List<string>(); var parent = new Node("parent", t); var a = new Node("a", t); var b = new Node("b", t);
        a.SetParent(parent); b.SetParent(parent); parent.SetInitialChild(a); a.Next = () => new HfsmTransition(b);
        var r = new HfsmRuntime(); r.Reset(parent); r.Enter(); t.Clear(); r.Tick(0);
        CollectionAssert.AreEqual(new[] { "exit a Transition", "enter b", "tick parent", "tick b" }, t);
    }

    [Test] public void ParentDeath_PreemptsChildAttack_AndExitsChildrenFirst()
    {
        var t = new List<string>(); var alive = new Node("alive", t); var attack = new Node("attack", t); var dead = new Node("dead", t);
        attack.SetParent(alive); alive.SetInitialChild(attack); alive.Next = () => new HfsmTransition(dead, StateExitReason.Interrupted);
        attack.Next = () => throw new Exception("Child guard must not run after ancestor interrupt");
        var r = new HfsmRuntime(); r.Reset(alive); r.Enter(); t.Clear(); r.Tick(0);
        CollectionAssert.AreEqual(new[] { "exit attack Interrupted", "exit alive Interrupted", "enter dead", "tick dead" }, t);
    }

    [Test] public void DeferredEntry_IsNotEnteredWhileSuspended()
    { var t = new List<string>(); var r = new HfsmRuntime(); r.Reset(new Node("a", t)); r.Suspend(); r.Tick(0); Assert.IsEmpty(t); r.Resume(); r.Enter(); Assert.AreEqual("enter a", t[0]); }

    [Test] public void ParentCycle_IsRejected()
    { var t = new List<string>(); var a = new Node("a", t); var b = new Node("b", t); b.SetParent(a); Assert.Throws<InvalidOperationException>(() => a.SetParent(b)); }

    [Test] public void InitialChild_MustBeDirectChild()
    { var t = new List<string>(); var a = new Node("a", t); var b = new Node("b", t); Assert.Throws<InvalidOperationException>(() => a.SetInitialChild(b)); }

    [Test] public void TwoActors_DoNotShareExecutionState()
    {
        var t = new List<string>(); var a = new HfsmRuntime(); var b = new HfsmRuntime(); a.Reset(new Node("a", t)); b.Reset(new Node("b", t));
        a.Enter(); b.Enter(); a.Stop(); Assert.IsNotNull(b.Current); Assert.IsTrue(b.IsEntered);
    }

    [Test] public void FlatRandomizedTrace_MatchesOriginalOneStepLoop()
    {
        // The original MonoBehaviour loop: enter; first matching transition; exit/enter; update destination.
        for (int seed = 0; seed < 40; seed++)
        {
            var trace = new List<string>(); var expected = new List<string>(); var nodes = new[] { new Node("0", trace), new Node("1", trace), new Node("2", trace) };
            int target = -1; foreach (var n in nodes) n.Next = () => target >= 0 ? new HfsmTransition(nodes[target]) : (HfsmTransition?)null;
            var r = new HfsmRuntime(); r.Reset(nodes[0]); int current = 0; bool entered = false; var random = new Random(seed);
            for (int frame = 0; frame < 80; frame++)
            {
                target = random.Next(-1, 3);
                if (!entered) { expected.Add("enter " + current); entered = true; }
                if (target >= 0) { expected.Add("exit " + current + " Transition"); current = target; expected.Add("enter " + current); }
                expected.Add("tick " + current); r.Tick(.016f);
            }
            CollectionAssert.AreEqual(expected, trace, "seed=" + seed);
        }
    }
}
