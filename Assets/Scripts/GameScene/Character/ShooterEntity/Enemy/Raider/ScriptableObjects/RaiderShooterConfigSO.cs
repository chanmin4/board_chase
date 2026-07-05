using System;
using UnityEngine;

public readonly struct EnemyShooterLoadout
{
    public WeaponSO Weapon { get; }
    public BulletSO Bullet { get; }
    public int BulletAmount { get; }
    public ArmorItemSO Armor { get; }

    public EnemyShooterLoadout(
        WeaponSO weapon,
        BulletSO bullet,
        int bulletAmount,
        ArmorItemSO armor)
    {
        Weapon = weapon;
        Bullet = bullet;
        BulletAmount = Mathf.Max(0, bulletAmount);
        Armor = armor;
    }
}

[CreateAssetMenu(
    fileName = "RaiderShooterConfig",
    menuName = "Game/Enemy/Raider Shooter Config")]
public class RaiderShooterConfigSO : EnemyShooterConfigSO
{
    [Serializable]
    public sealed class BulletWeightEntry
    {
        public BulletSO bullet;
        [Min(0f)] public float weight = 1f;
        [Min(0)] public int minLootAmount = 15;
        [Min(0)] public int maxLootAmount = 20;

        public int RollLootAmount()
        {
            int min = Mathf.Max(0, minLootAmount);
            int max = Mathf.Max(min, maxLootAmount);
            return UnityEngine.Random.Range(min, max + 1);
        }
    }

    [Serializable]
    public sealed class WeaponWeightEntry
    {
        public WeaponSO weapon;
        [Min(0f)] public float weight = 1f;
        public BulletWeightEntry[] bulletEntries;
    }

    [Serializable]
    public sealed class ArmorWeightEntry
    {
        public ArmorItemSO armor;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Raider Loadout")]
    [SerializeField] private WeaponWeightEntry[] _weaponEntries;
    [SerializeField] private ArmorWeightEntry[] _armorEntries;

    [Header("Fallback Bullet Loot")]
    [SerializeField, Min(0)] private int _fallbackBulletMinLootAmount = 15;
    [SerializeField, Min(0)] private int _fallbackBulletMaxLootAmount = 20;

    public EnemyShooterLoadout PickLoadout()
    {
        WeaponWeightEntry weaponEntry = PickWeaponEntry();
        ArmorItemSO armor = PickArmor();

        if (weaponEntry == null)
            return new EnemyShooterLoadout(null, null, 0, armor);

        BulletWeightEntry bulletEntry = PickBulletEntry(weaponEntry);
        BulletSO bullet = bulletEntry != null ? bulletEntry.bullet : null;
        int bulletAmount = bulletEntry != null ? bulletEntry.RollLootAmount() : 0;

        if (bullet == null)
        {
            bullet = Bullet;
            bulletAmount = bullet != null ? RollFallbackBulletAmount() : 0;
        }

        return new EnemyShooterLoadout(
            weaponEntry.weapon,
            bullet,
            bulletAmount,
            armor);
    }

    private WeaponWeightEntry PickWeaponEntry()
    {
        return PickWeighted(
            _weaponEntries,
            entry => entry != null && entry.weapon != null && entry.weight > 0f,
            entry => entry.weight);
    }

    private static BulletWeightEntry PickBulletEntry(WeaponWeightEntry weaponEntry)
    {
        if (weaponEntry == null)
            return null;

        BulletWeightEntry bulletEntry = PickWeighted(
            weaponEntry.bulletEntries,
            entry => entry != null && entry.bullet != null && entry.weight > 0f,
            entry => entry.weight);

        return bulletEntry;
    }

    private ArmorItemSO PickArmor()
    {
        ArmorWeightEntry armorEntry = PickWeighted(
            _armorEntries,
            entry => entry != null && entry.weight > 0f,
            entry => entry.weight);

        return armorEntry != null ? armorEntry.armor : null;
    }

    private int RollFallbackBulletAmount()
    {
        int min = Mathf.Max(0, _fallbackBulletMinLootAmount);
        int max = Mathf.Max(min, _fallbackBulletMaxLootAmount);
        return UnityEngine.Random.Range(min, max + 1);
    }

    private static T PickWeighted<T>(
        T[] entries,
        Predicate<T> isValid,
        Func<T, float> getWeight)
        where T : class
    {
        if (entries == null || entries.Length == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            T entry = entries[i];

            if (isValid != null && !isValid(entry))
                continue;

            totalWeight += Mathf.Max(0f, getWeight(entry));
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);

        for (int i = 0; i < entries.Length; i++)
        {
            T entry = entries[i];

            if (isValid != null && !isValid(entry))
                continue;

            roll -= Mathf.Max(0f, getWeight(entry));

            if (roll <= 0f)
                return entry;
        }

        return null;
    }
}
