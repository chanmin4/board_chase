// Stable category keys shared by authoring and runtime loot. Do not rename persisted keys.
public static class LootItemCategories
{
    public const string CurrencyKey = "Valuable:" + CurrencyItemSO.CategoryId;

    public static string GetKey(ItemSO item)
    {
        if (item == null)
            return "Other";
        if (item is CurrencyItemSO)
            return CurrencyKey;
        if (item is ValuableItemSO valuable)
            return ValuableCategoryCatalogSO.Active?.Find(valuable.ValuableCategoryId) != null
                ? "Valuable:" + valuable.ValuableCategoryId : "Valuable";
        if (item is MeleeWeaponSO)
            return "MeleeWip";
        if (item is RangedWeaponSO firearm)
            return firearm.WeaponType switch
            {
                RangedWeaponType.SubmachineGun => "WeaponSmg",
                RangedWeaponType.AssaultRifle => "WeaponAr",
                RangedWeaponType.LightMachineGun => "WeaponLmg",
                RangedWeaponType.Pistol or RangedWeaponType.Revolver => "WeaponPistol",
                RangedWeaponType.Shotgun => "WeaponShotgun",
                RangedWeaponType.SniperRifle => "WeaponSniper",
                _ => "WeaponSpecial"
            };
        if (item is WeaponSO)
            return "Weapon";
        if (item is BulletSO bullet)
            return bullet.Family switch
            {
                BulletFamily.Light => "BulletLight",
                BulletFamily.Medium => "BulletMedium",
                BulletFamily.Heavy => "BulletHeavy",
                BulletFamily.ShotgunShell => "BulletShotgun",
                BulletFamily.Sniper => "BulletSniper",
                _ => "BulletSpecial"
            };
        if (item is ThrowableUsableItemSO)
            return "Throwable";
        if (item is MedicalItemSO)
            return "Medical";
        if (item is ConsumableItemSO)
            return "Consumable";
        if (item is ArmorItemSO)
            return "Armor";
        if (item is GasMaskItemSO)
            return "HeadEquipment";
        if (item is BackpackItemSO)
            return "Backpack";
        if (item is KeyItemSO || item is KeyToolItemSO)
            return "Key";
        if (item is PassiveItemSO)
            return "Passive";
        return "Other";
    }
}
