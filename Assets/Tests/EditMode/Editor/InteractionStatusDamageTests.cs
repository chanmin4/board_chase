using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class InteractionStatusDamageTests
{
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private GameObject _actor;
    private Damageable _damageable;
    private InteractionManager _manager;
    private FieldInfo Cast=>typeof(InteractionManager).GetField("_castingInteraction",Private);

    [SetUp] public void SetUp()
    {
        _actor=new GameObject("Interaction status damage test");_actor.SetActive(false);
        _damageable=_actor.AddComponent<Damageable>();
        typeof(Damageable).GetField("_maxHealth",Private).SetValue(_damageable,100f);
        typeof(Damageable).GetField("_currentHealth",Private).SetValue(_damageable,100f);
        _manager=_actor.AddComponent<InteractionManager>();
        var callback=typeof(InteractionManager).GetMethod("CancelForDamage",Private);
        _damageable.OnDamaged+=(target,amount)=>callback.Invoke(_manager,new object[]{target,amount});
    }
    [TearDown] public void TearDown()=>UnityEngine.Object.DestroyImmediate(_actor);

    [TestCase(false)]
    [TestCase(true)]
    public void StatusTickPreservesCastButNextDirectHitCancels(bool infection)
    {
        var cast=new Interaction(InteractionType.Loot,_actor);Cast.SetValue(_manager,cast);
        if(infection)_damageable.ReceiveInfectionDamageOverTime(5);
        else _damageable.ReceiveStatusDamageOverTime(5);
        Assert.That(_damageable.CurrentHealth,Is.EqualTo(95));
        Assert.That(Cast.GetValue(_manager),Is.SameAs(cast));
        Assert.That(_damageable.IsPeriodicStatusDamage,Is.False);
        _damageable.ReceiveFixedMeleeAttack(1);
        Assert.That(Cast.GetValue(_manager),Is.Null);
    }

    [Test] public void NestedDirectHitDoesNotInheritTheStatusTickExemption()
    {
        bool nested=false;
        _damageable.OnDamaged+=(target,amount)=>
        {
            if(nested)return;
            Assert.That(target.IsPeriodicStatusDamage,Is.True);
            nested=true;target.ReceiveFixedMeleeAttack(1);
            Assert.That(target.IsPeriodicStatusDamage,Is.True);
        };
        Cast.SetValue(_manager,new Interaction(InteractionType.Portal,_actor));
        _damageable.ReceiveStatusDamageOverTime(1);
        Assert.That(Cast.GetValue(_manager),Is.Null);
        Assert.That(_damageable.IsPeriodicStatusDamage,Is.False);
    }

    [Test] public void NotificationExceptionDoesNotLeakTheStatusDamageFlag()
    {
        _damageable.OnDamaged+=(target,amount)=>throw new InvalidOperationException("test listener");
        Assert.Throws<InvalidOperationException>(()=>_damageable.ReceiveInfectionDamageOverTime(1));
        Assert.That(_damageable.IsPeriodicStatusDamage,Is.False);
    }
}
