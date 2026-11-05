using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class StaminaInputTests
{
    private GameObject _actor;
    private ShooterStaminaRuntime _stamina;
    private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;

    [SetUp] public void SetUp()
    {
        _actor=new GameObject("Stamina input boundary test");_actor.SetActive(false);
        _stamina=_actor.AddComponent<ShooterStaminaRuntime>();
        Set("_initialized",true);Set("_temporarySettings",new ShooterLocomotionSettings{maximumStamina=200});
    }
    [TearDown] public void TearDown()=>Object.DestroyImmediate(_actor);
    private void Set(string field,object value)=>typeof(ShooterStaminaRuntime).GetField(field,Private).SetValue(_stamina,value);
    private bool Authorized=>(bool)typeof(ShooterStaminaRuntime).GetField("_sprintAuthorized",Private).GetValue(_stamina);

    [TestCase(0f,false,0f)]
    [TestCase(18f,false,18f)]
    [TestCase(19.999f,false,19.999f)]
    [TestCase(20f,true,0f)]
    [TestCase(24f,true,0f)]
    [TestCase(80f,true,30f)]
    public void DashChecksTenPercentNotFullCost(float current,bool accepted,float remaining)
    {
        Set("_current",current);
        Assert.That(_stamina.TrySpendDash(),Is.EqualTo(accepted));
        Assert.That(_stamina.Current,Is.EqualTo(remaining).Within(.0001f));
    }

    [Test] public void SharedStaminaTuningIsTwentyPerSecondFiftyPerDashAndTwoSecondDelay()
    {
        var defaults=new ShooterLocomotionSettings();
        var common=UnityEditor.AssetDatabase.LoadAssetAtPath<ShooterCommonRulesSO>("Assets/Resources/ShooterCommonRules.asset");
        Assert.That(common,Is.Not.Null);
        foreach(var settings in new[]{defaults,common.Locomotion})
        {
            Assert.That(settings.sprintCostPerSecond,Is.EqualTo(20));
            Assert.That(settings.dashCost,Is.EqualTo(50));
            Assert.That(settings.recoveryDelay,Is.EqualTo(2));
        }
    }

    [Test] public void HeldPressDoesNotRecheckThresholdOrAutoStartAfterRejection()
    {
        Set("_current",24f);_stamina.SetSprintRequested(true);Assert.That(Authorized,Is.True);
        Set("_current",18f);_stamina.SetSprintRequested(true);Assert.That(Authorized,Is.True);
        _stamina.SetSprintRequested(false);_stamina.SetSprintRequested(true);Assert.That(Authorized,Is.False);
        Set("_current",50f);_stamina.SetSprintRequested(true);Assert.That(Authorized,Is.False);
        _stamina.SetSprintRequested(false);_stamina.SetSprintRequested(true);Assert.That(Authorized,Is.True);
    }

    [Test] public void MovementGateDoesNotInventAnotherKeyPress()
    {
        Set("_current",18f);_stamina.SetSprintRequested(true,false);
        Set("_current",50f);_stamina.SetSprintRequested(true,true);
        Assert.That(Authorized,Is.False);
    }
}
