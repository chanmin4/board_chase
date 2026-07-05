using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EnemyLootBulletStack
{
    [SerializeField] private BulletSO _bullet;
    [SerializeField, Min(0)] private int _amount;

    public BulletSO Bullet => _bullet;
    public int Amount => Mathf.Max(0, _amount);
    public bool IsAvailable => _bullet != null && Amount > 0;

    public void Set(BulletSO bullet, int amount)
    {
        _bullet = bullet;
        _amount = Mathf.Max(0, amount);
    }

    public void Clear()
    {
        _bullet = null;
        _amount = 0;
    }
}

[DisallowMultipleComponent]
public class EnemyLootInventoryRuntime : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private bool _canLootOnlyWhenDead = true;
    [SerializeField, Min(0f)] private float _interactSeconds = 1.5f;
    [SerializeField] private bool _suppressRandomDropReward = true;

    [Header("Events")]
    [SerializeField] private EnemyLootOpenRequestEventChannelSO _openRequestChannel;
    [SerializeField] private UIOverlayRequestEventChannelSO _overlayRequestChannel;

    [Header("Refs")]
    [SerializeField] private Damageable _damageable;
    [SerializeField] private EntityEquipmentRuntime _equipmentRuntime;

    [Header("Loot Runtime ")]
    [SerializeField][ReadOnly] private WeaponSO _weapon;
    [SerializeField][ReadOnly] private ArmorItemSO _armor;
    [SerializeField, Min(0f)][ReadOnly] private float _armorDurability;
    [SerializeField][ReadOnly] private EnemyLootBulletStack[] _bulletStacks =
    {
        new(),
        new(),
        new()
    };

    private bool _openInProgress;

    public WeaponSO Weapon => _weapon;
    public ArmorItemSO Armor => _armor;
    public float ArmorDurability => ResolveArmorDurability();
    public IReadOnlyList<EnemyLootBulletStack> BulletStacks => _bulletStacks;
    public float InteractSeconds => Mathf.Max(0f, _interactSeconds);
    public bool SuppressRandomDropReward => _suppressRandomDropReward;
    public bool HasLoot => _weapon != null || _armor != null || HasBulletLoot();
    public bool CanInteract =>
        HasLoot &&
        (!_canLootOnlyWhenDead ||
         _damageable == null ||
         _damageable.IsDead);

    private void Reset()
    {
        ResolveRefs();
    }

    private void Awake()
    {
        ResolveRefs();
        NormalizeBulletStacks();
    }

    public void Initialize(EnemyShooterLoadout loadout)
    {
        ResolveRefs();
        NormalizeBulletStacks();

        _weapon = loadout.Weapon;
        _armor = loadout.Armor;
        _armorDurability = _armor != null ? _armor.MaxDurability : 0f;

        for (int i = 0; i < _bulletStacks.Length; i++)
            _bulletStacks[i].Clear();

        if (loadout.Bullet != null && loadout.BulletAmount > 0)
            _bulletStacks[0].Set(loadout.Bullet, loadout.BulletAmount);
    }

    public void SetWeapon(WeaponSO weapon)
    {
        _weapon = weapon;
    }

    public void SetArmor(ArmorItemSO armor, float durability)
    {
        _armor = armor;
        _armorDurability = armor != null
            ? Mathf.Clamp(durability, 0f, armor.MaxDurability)
            : 0f;
    }

    public void SetBulletStack(int index, BulletSO bullet, int amount)
    {
        NormalizeBulletStacks();

        if (index < 0 || index >= _bulletStacks.Length)
            return;

        _bulletStacks[index].Set(bullet, amount);
    }

    public void ClearWeapon()
    {
        _weapon = null;
    }

    public void ClearArmor()
    {
        _armor = null;
        _armorDurability = 0f;
    }

    public void ClearBulletStack(int index)
    {
        NormalizeBulletStacks();

        if (index < 0 || index >= _bulletStacks.Length)
            return;

        _bulletStacks[index].Clear();
    }

    public bool TrySplitBulletStack(
        int index,
        int amount,
        out int newIndex,
        out string message)
    {
        NormalizeBulletStacks();
        newIndex = -1;

        if (index < 0 || index >= _bulletStacks.Length)
            return Fail("Invalid bullet loot slot.", out message);

        EnemyLootBulletStack source = _bulletStacks[index];

        if (source == null || !source.IsAvailable)
            return Fail("No bullet loot.", out message);

        if (source.Amount <= 1)
            return Fail("This stack cannot be split.", out message);

        for (int i = 0; i < _bulletStacks.Length; i++)
        {
            if (i == index)
                continue;

            if (_bulletStacks[i] == null)
                _bulletStacks[i] = new EnemyLootBulletStack();

            if (!_bulletStacks[i].IsAvailable)
            {
                newIndex = i;
                break;
            }
        }

        if (newIndex < 0)
            return Fail("No empty loot bullet slot.", out message);

        int splitAmount = Mathf.Clamp(amount, 1, source.Amount - 1);
        int remainingAmount = Mathf.Max(1, source.Amount - splitAmount);
        BulletSO bullet = source.Bullet;

        source.Set(bullet, remainingAmount);
        _bulletStacks[newIndex].Set(bullet, splitAmount);
        message = $"{bullet.DisplayName} split.";
        return true;
    }

    public bool TryInteract(Component actor)
    {
        if (!CanInteract)
            return false;

        if (_openRequestChannel == null)
            return false;

        SyncArmorDurabilityFromEquipment();

        if (_openInProgress)
            return true;

        if (InteractSeconds > 0f)
        {
            StartCoroutine(OpenAfterDelay(actor, InteractSeconds));
            return true;
        }

        OpenLootPanel(actor);
        return true;
    }

    private IEnumerator OpenAfterDelay(Component actor, float seconds)
    {
        _openInProgress = true;
        yield return new WaitForSeconds(seconds);
        _openInProgress = false;

        if (!CanInteract)
            yield break;

        OpenLootPanel(actor);
    }

    private void OpenLootPanel(Component actor)
    {
        _overlayRequestChannel?.Open(UIOverlayId.PlayerPanelHub);
        _openRequestChannel?.RaiseEvent(
            new EnemyLootOpenRequest(this, actor, InteractSeconds));
    }

    public bool TryTakeWeapon(PlayerInventoryRuntime inventory, out string message)
    {
        if (_weapon == null)
            return Fail("No weapon loot.", out message);

        if (inventory == null)
            return Fail("Player inventory is missing.", out message);

        WeaponSO weapon = _weapon;

        if (!inventory.TryPickup(weapon, out message))
            return false;

        _weapon = null;
        return true;
    }

    public bool TryTakeArmor(PlayerInventoryRuntime inventory, out string message)
    {
        if (_armor == null)
            return Fail("No armor loot.", out message);

        if (inventory == null)
            return Fail("Player inventory is missing.", out message);

        SyncArmorDurabilityFromEquipment();

        ArmorItemSO armor = _armor;
        float durability = ArmorDurability;

        if (!inventory.TryPickupArmorLoot(armor, durability, out message))
            return false;

        _armor = null;
        _armorDurability = 0f;
        return true;
    }

    public bool TryTakeBulletStack(
        int index,
        PlayerInventoryRuntime inventory,
        out string message)
    {
        NormalizeBulletStacks();

        if (index < 0 || index >= _bulletStacks.Length)
            return Fail("Invalid bullet loot slot.", out message);

        EnemyLootBulletStack stack = _bulletStacks[index];

        if (stack == null || !stack.IsAvailable)
            return Fail("No bullet loot.", out message);

        if (inventory == null)
            return Fail("Player inventory is missing.", out message);

        BulletSO bullet = stack.Bullet;
        int amount = stack.Amount;

        if (!inventory.TryPickup(bullet, 1, amount, 0f, out message))
            return false;

        stack.Clear();
        return true;
    }

    public void SyncArmorDurabilityFromEquipment()
    {
        if (_equipmentRuntime == null)
            return;

        if (_equipmentRuntime.CurrentArmor != _armor)
            return;

        _armorDurability = _equipmentRuntime.CurrentArmorDurability;
    }

    private void ResolveRefs()
    {
        if (_damageable == null)
            _damageable = GetComponent<Damageable>() ??
                          GetComponentInChildren<Damageable>(true) ??
                          GetComponentInParent<Damageable>();

        if (_equipmentRuntime == null)
            _equipmentRuntime = GetComponent<EntityEquipmentRuntime>() ??
                                GetComponentInChildren<EntityEquipmentRuntime>(true) ??
                                GetComponentInParent<EntityEquipmentRuntime>();
    }

    private void NormalizeBulletStacks()
    {
        if (_bulletStacks == null || _bulletStacks.Length == 0)
        {
            _bulletStacks = new[]
            {
                new EnemyLootBulletStack(),
                new EnemyLootBulletStack(),
                new EnemyLootBulletStack()
            };
            return;
        }

        for (int i = 0; i < _bulletStacks.Length; i++)
        {
            if (_bulletStacks[i] == null)
                _bulletStacks[i] = new EnemyLootBulletStack();
        }
    }

    private bool HasBulletLoot()
    {
        NormalizeBulletStacks();

        for (int i = 0; i < _bulletStacks.Length; i++)
        {
            if (_bulletStacks[i] != null && _bulletStacks[i].IsAvailable)
                return true;
        }

        return false;
    }

    private float ResolveArmorDurability()
    {
        if (_armor == null)
            return 0f;

        return Mathf.Clamp(_armorDurability, 0f, _armor.MaxDurability);
    }

    private static bool Fail(string error, out string message)
    {
        message = error;
        return false;
    }
}
