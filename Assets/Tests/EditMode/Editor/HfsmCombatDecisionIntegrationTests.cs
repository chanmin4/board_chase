using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>미리보기 씬/메모리 SO만 사용합니다. 실제 캐릭터·작업 씬·장비·테이블을 저장하거나 변경하지 않습니다.</summary>
public sealed class HfsmCombatDecisionIntegrationTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<UnityEngine.Object> _owned = new();
    private Scene _scene;
    private EnemyShooterCombatController _combat;
    private EnemyShooterConfigSO _config;
    private EnemyShooterShoot _shoot;
    private EntityEquipmentRuntime _equipment;
    private EnemyLootInventoryRuntime _loot;
    private ShooterThrowableAttack _thrower;
    private Transform _target;
    private ThrowableUsableItemSO _grenade;

    private static FieldInfo Field(object obj, string name)
    {
        for (Type type = obj.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Flags | BindingFlags.DeclaredOnly);
            if (field != null) return field;
        }
        throw new MissingFieldException(obj.GetType().Name, name);
    }
    private static void Set(object obj, string name, object value) => Field(obj, name).SetValue(obj, value);
    private static T Get<T>(object obj, string name) => (T)Field(obj, name).GetValue(obj);
    private T Temp<T>() where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); _owned.Add(value); return value; }
    private GameObject Object(string name)
    { var go = ShooterCustomizeTestObjects.Create(name, _scene); go.SetActive(false); return go; }
    private void Muzzle(bool clear)
    { Set(_combat, "_muzzleClearanceFrame", Time.frameCount); Set(_combat, "_muzzleClearanceResult", clear); }
    private void Burst(bool fired = true, int shots = 2, EnemyShooterAttackPhase phase = EnemyShooterAttackPhase.FiringBurst)
    {
        Set(_combat, "_attackActive", true); Set(_combat, "_hasFiredCurrentBurst", fired);
        Set(_combat, "_burstShotsRemaining", shots); Set(_combat, "_attackPhase", phase);
    }
    private void ReadyThrowable()
    {
        Set(_config, "_useThrowableAttack", true);
        Set(_config, "_throwableBlockedFireSeconds", 0.2f);
        _target.position = _combat.transform.position + Vector3.forward * 10;
        Set(_combat, "_canSeeTarget", true); Muzzle(false);
        var opportunity = Get<EnemyShooterThrowableOpportunity>(_combat, "_throwableOpportunity");
        // 에디터 Time.time이 0이어도 대기 없이 검증할 수 있도록 명시적인 과거 관측을 넣습니다.
        opportunity.Observe(_target.GetInstanceID(), true, false, false, Time.time - 0.5f);
        opportunity.Observe(_target.GetInstanceID(), true, false, false, Time.time);
        Set(_combat, "_lastVisibleThrowablePoint", _target.position);
        var stack = new PlayerInventoryItemStack(); stack.Set(_grenade, 2);
        Set(_loot, "_items", new List<PlayerInventoryItemStack> { stack });
    }

    [SetUp] public void Setup()
    {
        _scene = EditorSceneManager.NewPreviewScene();
        var root = Object("combat decision test"); root.transform.position = new Vector3(10000, 10000, 10000);
        _config = Temp<EnemyShooterConfigSO>();
        Set(_config, "_useMaxRangeAsAttackKeepRange", false); Set(_config, "_attackKeepRange", 30f);
        Set(_config, "_useMeleeAttack", true);
        Set(_config, "_autoSwitchCombatWeaponSlots", true);
        _equipment = root.AddComponent<EntityEquipmentRuntime>();
        var ranged = Temp<RangedWeaponSO>(); Set(ranged, "_weaponType", RangedWeaponType.Pistol);
        var melee = Temp<MeleeWeaponSO>();
        Set(_equipment, "_weaponSlots", new WeaponSO[] { ranged, melee });
        Set(_equipment, "_weaponSlotAmounts", new[] { 1, 1 });
        Set(_equipment, "_weaponCurrentDurabilities", new[] { 100f, 100f });
        Set(_equipment, "_weaponMaxDurabilities", new[] { 100f, 100f });
        var visualEquipment = root.AddComponent<ShooterEquipmentController>(); Set(visualEquipment, "_equipmentRuntime", _equipment);
        _shoot = root.AddComponent<EnemyShooterShoot>(); Set(_shoot, "_config", _config);
        Set(_shoot, "_runtimeBullet", Temp<LightBulletSO>()); Set(_shoot, "_equipmentController", visualEquipment);
        Set(_shoot, "_magazineAmmo", 10);
        var meleeAttack = root.AddComponent<ShooterMeleeAttack>(); Set(meleeAttack, "_equipmentController", visualEquipment);
        _thrower = root.AddComponent<ShooterThrowableAttack>();
        _loot = root.AddComponent<EnemyLootInventoryRuntime>();
        _combat = root.AddComponent<EnemyShooterCombatController>();
        Set(_combat, "_shoot", _shoot); Set(_combat, "_equipmentController", visualEquipment);
        Set(_combat, "_meleeAttack", meleeAttack); Set(_combat, "_throwableAttack", _thrower); Set(_combat, "_lootInventory", _loot);
        _target = Object("known target").transform; _target.position = root.transform.position + Vector3.forward;
        Set(_combat, "_target", _target); Set(_combat, "_canSeeTarget", true); Muzzle(true);
        _grenade = Temp<ThrowableUsableItemSO>();
        var view = Object("temporary grenade view").AddComponent<WeaponView>(); Set(_grenade, "_heldViewPrefab", view);
    }
    [TearDown] public void Cleanup()
    {
        if (_scene.IsValid()) EditorSceneManager.ClosePreviewScene(_scene);
        foreach (var obj in _owned) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        _owned.Clear();
    }

    [Test] public void CloseTargetCannotInterruptStartedBurstOrSwitchWeapon()
    {
        Burst();
        Assert.That(_combat.CanChangeCombatAction, Is.False);
        Assert.That(_combat.ShouldUseMelee, Is.False);
        Assert.That(_combat.CanAttackTarget, Is.True);
        Assert.That(_combat.ShouldChaseTarget, Is.False);
        Assert.That(_equipment.ActiveWeaponSlotIndex, Is.Zero);
        Assert.That(_combat.BurstShotsRemaining, Is.EqualTo(2));
    }
    [Test] public void ButcherStyleMeleeIsImmediateWithoutStartedBurstAndAfterLastShot()
    {
        Assert.That(_combat.ShouldUseMelee, Is.True);
        Burst(fired: false); Assert.That(_combat.ShouldUseMelee, Is.True);
        Burst(shots: 0, phase: EnemyShooterAttackPhase.BurstCooldown);
        Assert.That(_combat.CanChangeCombatAction, Is.True); Assert.That(_combat.ShouldUseMelee, Is.True);
        Assert.That(_combat.CanAttackTarget, Is.False);
    }
    [Test] public void ThrowableWaitsForCurrentBurstButNotTheEntireBurstCooldown()
    {
        ReadyThrowable(); Set(_config, "_finishStartedBurstAfterSightLost", true);
        Burst(); Assert.That(_combat.ShouldUseThrowable, Is.False);
        Assert.That(_equipment.ActiveWeaponSlotIndex, Is.Zero);
        Burst(shots: 0, phase: EnemyShooterAttackPhase.BurstCooldown);
        Set(_combat, "_nextFireTime", Time.time + 20);
        Assert.That(_combat.ShouldUseThrowable, Is.True);
        Assert.That(_loot.ItemStacks[0].Amount, Is.EqualTo(2));
    }
    [Test] public void VisibleClearShotOverridesLegacyThrowableFlagsAndReadiedOpportunity()
    {
        ReadyThrowable(); Set(_config, "_throwableBlockWhenCanAttack", false); Set(_config, "_throwablePreferWhenNoLineOfSight", false);
        Muzzle(true);
        Assert.That(_combat.ShouldUseThrowable, Is.False);
    }
    [Test] public void VisionLostButClearPhysicalShotDoesNotSelectThrowable()
    {
        ReadyThrowable(); Set(_combat, "_canSeeTarget", false); Muzzle(true);
        Assert.That(_combat.ShouldUseThrowable, Is.False);
    }
    [Test] public void SelectionRequiresInventoryCooldownRangeSelfSafetyAndSafeArc()
    {
        ReadyThrowable(); Assert.That(_combat.ShouldUseThrowable, Is.True);
        Set(_combat, "_nextThrowableAllowedTime", Time.time + 1); Assert.That(_combat.ShouldUseThrowable, Is.False);
        Set(_combat, "_nextThrowableAllowedTime", 0f);
        Set(_config, "_throwableMaxRange", 9f); Assert.That(_combat.ShouldUseThrowable, Is.False);
        Set(_config, "_throwableMaxRange", 35f);
        Set(_config, "_throwableSelfSafetyPadding", 20f); Assert.That(_combat.ShouldUseThrowable, Is.False);
        Set(_config, "_throwableSelfSafetyPadding", 1f);
        Set(_loot, "_items", new List<PlayerInventoryItemStack>()); Assert.That(_combat.ShouldUseThrowable, Is.False);
        Assert.That(_thrower.CanThrowEnemyUsableWithSafeArc(Temp<ThrowableUsableItemSO>(), _target.position, out _), Is.False);
    }
    [Test] public void HiddenTargetUsesLastVisiblePositionInsteadOfCurrentTransform()
    {
        ReadyThrowable(); Vector3 seen = _target.position;
        Set(_combat, "_canSeeTarget", false); _target.position += Vector3.right * 10;
        object[] args = { Vector3.zero };
        bool resolved = (bool)typeof(EnemyShooterCombatController).GetMethod("TryResolveThrowableTargetPoint", Flags).Invoke(_combat, args);
        Assert.That(resolved, Is.True); Assert.That((Vector3)args[0], Is.EqualTo(seen));
    }
    [Test] public void FailureTargetLossAndEmergencyReleaseBurstProtection()
    {
        Burst(); Set(_equipment, "_weaponCurrentDurabilities", new[] { 0f, 100f });
        Assert.That(_combat.CanChangeCombatAction, Is.True);
        Set(_equipment, "_weaponCurrentDurabilities", new[] { 100f, 100f });
        Set(_combat, "_isEvadingIncomingThrowable", true);
        Assert.That(_combat.CanChangeCombatAction, Is.True); Assert.That(_combat.ShouldUseMelee, Is.False);
        Set(_combat, "_isEvadingIncomingThrowable", false); Set(_combat, "_target", null);
        Assert.That(_combat.CanChangeCombatAction, Is.True);
    }
    [Test] public void EmptyMagazineAndReloadCannotHoldActionSwitchUntilBurstEnd()
    {
        Burst(); Set(_shoot, "_magazineAmmo", 0);
        Assert.That(_combat.CanChangeCombatAction, Is.True); Assert.That(_combat.ShouldUseMelee, Is.True);
        Set(_shoot, "_magazineAmmo", 10); Set(_shoot, "_isReloading", true);
        Assert.That(_combat.CanChangeCombatAction, Is.True);
    }
    [Test] public void EmergencyEvasionActuallyClearsQueuedBurstShots()
    {
        Burst(); typeof(EnemyShooterCombatController).GetMethod("AbortCurrentAttackForIncomingThrowable", Flags).Invoke(_combat, null);
        Assert.That(_combat.BurstShotsRemaining, Is.Zero); Assert.That(_combat.CanChangeCombatAction, Is.True);
    }
    [Test] public void ClearingCombatResetsThrowableOpportunityAndCurrentBurst()
    {
        ReadyThrowable(); Burst(); _combat.ClearCombat();
        Assert.That(_combat.CanChangeCombatAction, Is.True); Assert.That(_combat.ShouldUseThrowable, Is.False);
    }
    [Test] public void AttackMovementSettingsKeepTheirExistingMeaning()
    {
        Set(_config, "_attackMovementMode", EnemyShooterAttackMovementMode.StopWhileAttacking);
        Assert.That(_config.MoveWhileAttacking, Is.False); Assert.That(_config.MoveDuringBurstInterval, Is.False);
        Set(_config, "_attackMovementMode", EnemyShooterAttackMovementMode.RepositionBetweenBursts);
        Assert.That(_config.MoveWhileAttacking, Is.False); Assert.That(_config.MoveDuringBurstInterval, Is.True);
        Set(_config, "_attackMovementMode", EnemyShooterAttackMovementMode.MoveWhileFiring);
        Assert.That(_config.MoveWhileAttacking, Is.True); Assert.That(_config.MoveDuringBurstInterval, Is.True);
    }
    [Test] public void AuthoredButcherRetainsMeleeAndDisabledThrowersRemainDisabled()
    {
        const string path = "Assets/ScriptableObject/GameScene/Character/Shooter/EnemyShooter/NormalEnemy/";
        var butcher = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(path + "Butcher/ButcherShooterConfig.asset");
        Assert.That(butcher, Is.Not.Null); Assert.That(butcher.UseMeleeAttack, Is.True);
        foreach (string name in new[] { "Raider", "Agent", "Scraper" })
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyShooterConfigSO>(path + name + "/" + name + "ShooterConfig.asset");
            Assert.That(config, Is.Not.Null); Assert.That(config.UseThrowableAttack, Is.False, name);
            Assert.That(config.ThrowableBlockedFireSeconds, Is.EqualTo(1.5f), name);
        }
    }
}
