using System;
using UnityEngine;

public readonly struct EnemyShooterLoadout
{
    public WeaponSO Weapon { get; }
    public BulletSO Bullet { get; }

    public EnemyShooterLoadout(WeaponSO weapon, BulletSO bullet)
    {
        Weapon = weapon;
        Bullet = bullet;
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
    }

    [Serializable]
    public sealed class WeaponWeightEntry
    {
        public WeaponSO weapon;
        [Min(0f)] public float weight = 1f;
        public BulletWeightEntry[] bulletEntries;
    }

    [Header("Raider Loadout")]
    [SerializeField] private WeaponWeightEntry[] _weaponEntries;

    public EnemyShooterLoadout PickLoadout()
    {
        WeaponWeightEntry weaponEntry = PickWeaponEntry();

        if (weaponEntry == null)
            return default;

        BulletSO bullet = PickBullet(weaponEntry);

        if (bullet == null)
            bullet = Bullet;

        return new EnemyShooterLoadout(weaponEntry.weapon, bullet);
    }

    private WeaponWeightEntry PickWeaponEntry()
    {
        return PickWeighted(
            _weaponEntries,
            entry => entry != null && entry.weapon != null && entry.weight > 0f,
            entry => entry.weight);
    }

    private static BulletSO PickBullet(WeaponWeightEntry weaponEntry)
    {
        if (weaponEntry == null)
            return null;

        BulletWeightEntry bulletEntry = PickWeighted(
            weaponEntry.bulletEntries,
            entry => entry != null && entry.bullet != null && entry.weight > 0f,
            entry => entry.weight);

        return bulletEntry != null ? bulletEntry.bullet : null;
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