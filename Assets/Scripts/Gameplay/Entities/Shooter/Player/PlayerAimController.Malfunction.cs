using UnityEngine;

public partial class PlayerAimController
{
    private static PlayerAimController _malfunctionOwner;
    private float _functionCheckEndsAt;
    private int _functionCheckSlot = -1;
    private WeaponSO _functionCheckWeapon;
    public bool IsFunctionChecking => _functionCheckSlot >= 0;
    public static bool HasActiveWeaponMalfunction => _malfunctionOwner != null &&
        _malfunctionOwner.isActiveAndEnabled && _malfunctionOwner._equipmentRuntime != null &&
        _malfunctionOwner._equipmentRuntime.IsWeaponJammed(_malfunctionOwner._equipmentRuntime.ActiveWeaponSlotIndex);

    public bool CheckWeaponMalfunction(RangedWeaponSO weapon)
    {
        if (_equipmentRuntime == null || weapon == null) return false;
        int slot = _equipmentRuntime.ActiveWeaponSlotIndex;
        if (!_equipmentRuntime.IsWeaponJammed(slot) && UnityEngine.Random.value >= weapon.MalfunctionChance(_equipmentRuntime.CurrentWeaponCondition01)) return false;
        bool newlyJammed = !_equipmentRuntime.IsWeaponJammed(slot);
        _equipmentRuntime.SetWeaponJammed(slot, true);
        _malfunctionOwner = this;
        if (newlyJammed) MalfunctionMessage("총기고장! T키로 기능처치 하세요.");
        ApplyDryFireCooldown(BulletAmmoType.Basic);
        return true;
    }

    private void HandleFunctionCheck()
    {
        if (GamePause.IsPaused || GameplayActionInputBlocker.IsAttackBlocked || IsFunctionChecking ||
            _equipmentRuntime == null || !_equipmentRuntime.IsWeaponJammed(_equipmentRuntime.ActiveWeaponSlotIndex) ||
            EntityComponentResolver.Resolve<Damageable>(this)?.IsDead == true) return;
        CancelReload("총기 기능처치");
        _functionCheckSlot = _equipmentRuntime.ActiveWeaponSlotIndex;
        _functionCheckWeapon = _equipmentRuntime.CurrentWeapon;
        _functionCheckEndsAt = Time.time + 5f;
        MalfunctionMessage("총기 기능처치 중 · 5초 · 이동 가능");
    }

    private void UpdateFunctionCheck()
    {
        if (_equipmentRuntime != null && _equipmentRuntime.IsWeaponJammed(_equipmentRuntime.ActiveWeaponSlotIndex)) _malfunctionOwner = this;
        if (!IsFunctionChecking) return;
        if (_equipmentRuntime == null || _equipmentRuntime.ActiveWeaponSlotIndex != _functionCheckSlot ||
            _equipmentRuntime.CurrentWeapon != _functionCheckWeapon ||
            !_equipmentRuntime.IsWeaponJammed(_functionCheckSlot) ||
            EntityComponentResolver.Resolve<Damageable>(this)?.IsDead == true || GameplayActionInputBlocker.IsAttackBlocked)
        { CancelFunctionCheck(); return; }
        if (Time.time >= _functionCheckEndsAt)
        {
            _equipmentRuntime.SetWeaponJammed(_functionCheckSlot, false);
            CancelFunctionCheck();
            MalfunctionMessage("기능처치 완료. 정상 사격 가능합니다.");
            return;
        }
        var actor = EntityComponentResolver.ResolveEntityRoot(this) ?? transform;
        _reloadCastBarChannel?.RaiseEvent(new InteractionCastBarSnapshot(true, InteractionType.Reload,
            actor, actor, 1f - (_functionCheckEndsAt - Time.time) / 5f, 5f));
    }

    private void CancelFunctionCheck()
    {
        if (IsFunctionChecking) _reloadCastBarChannel?.Clear();
        _functionCheckSlot = -1;
        _functionCheckWeapon = null;
    }

    private void MalfunctionMessage(string message)
    {
        _shoot?.PublishWeaponMalfunctionMessage(message);
        Debug.Log($"[Weapon malfunction] {message}", this);
    }
}
