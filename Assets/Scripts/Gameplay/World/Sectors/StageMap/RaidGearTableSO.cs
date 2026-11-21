using UnityEngine;

/// <summary>Stage balance overlay, not a replacement for enemy archetypes or HFSM/loadout authoring.</summary>
[CreateAssetMenu(menuName = "Game/Raid/Stage Gear Table", fileName = "RaidGearTable")]
public sealed class RaidGearTableSO : ScriptableObject
{
    [SerializeField] private ArmorItemSO[] _armor;
    [SerializeField] private BulletSO[] _basicAmmo;
    [Tooltip("Test weights for stage / stage+1 / stage+2. Not final balance.")]
    [SerializeField] private Vector3 _tierWeights = new(1f, 2f, 1f);

    public int PickGrade(int stage, bool strongRoom, float roll)
    {
        int first = stage==3 ? 2 : Mathf.Clamp(stage, 1, 6); // S2 and S3 are equivalent branches.
        float low = strongRoom ? 0f : Mathf.Max(0f, _tierWeights.x);
        float middle = Mathf.Max(0f, _tierWeights.y), high = Mathf.Max(0f, _tierWeights.z);
        float value = Mathf.Clamp01(roll) * (low + middle + high);
        return Mathf.Min(8, first + (value < low ? 0 : value < low + middle ? 1 : 2));
    }

    public ArmorItemSO FindArmor(int grade)
    {
        if (_armor != null) foreach (var armor in _armor)
            if (armor != null && armor.EquipmentTier == grade) return armor;
        return null;
    }

    public BulletSO FindAmmo(RangedWeaponSO weapon, int grade)
    {
        if (weapon != null && _basicAmmo != null) foreach (var bullet in _basicAmmo)
            if (bullet != null && bullet.AmmoType == BulletAmmoType.Basic &&
                (int)bullet.Tier == grade && weapon.IsCompatibleWith(bullet)) return bullet;
        return null;
    }

    public EnemyShooterLoadout Adjust(EnemyShooterLoadout original, int stage, bool strongRoom)
    {
        // Intentionally naked/melee/throwable archetypes stay that way. Special ammo stays authored.
        ArmorItemSO armor = original.Armor == null ? null :
            FindArmor(PickGrade(stage, strongRoom, Random.value)) ?? original.Armor;
        var ammo = original.AmmoStacks != null ? (ShooterLoadoutAmmoStack[])original.AmmoStacks.Clone()
            : System.Array.Empty<ShooterLoadoutAmmoStack>();
        for (int i = 0; i < ammo.Length; i++)
        {
            var stack = ammo[i];
            if (!stack.HasBullet || stack.bullet.AmmoType != BulletAmmoType.Basic ||
                original.GetWeapon(stack.sourceWeaponSlotIndex) is not RangedWeaponSO weapon) continue;
            var replacement = FindAmmo(weapon, PickGrade(stage, strongRoom, Random.value));
            if (replacement != null) ammo[i] = new ShooterLoadoutAmmoStack(
                stack.sourceWeaponSlotIndex, stack.slotIndex, replacement, stack.amount);
        }
        return new EnemyShooterLoadout(original.WeaponStacks, ammo, armor,
            original.Currency, original.CurrencyAmount, original.InventoryStacks);
    }

    public static EnemyShooterLoadout ApplyToRaid(EnemyShooterLoadout original, int stage, SectorRuntime sector)
    {
#if UNITY_EDITOR
        var lab = CombatLabRuntime.Active;
        if (lab != null && lab.Sector == sector)
        {
            var labTable = Resources.Load<RaidGearTableSO>("RaidGearTable");
            return labTable != null ? labTable.Adjust(original, stage, lab.StrongRoom) : original;
        }
#endif
        var raid = RaidRuntime.Active;
        if (stage <= 0 || raid == null || raid.State?.layout?.generationVersion != 3 ||
            !raid.State.active || sector == null || !raid.State.layout.TryGetRoom(sector.Coord, out var room))
            return original;
        var table = Resources.Load<RaidGearTableSO>("RaidGearTable");
        return table != null ? table.Adjust(original, stage, room.raidRole == RaidRoomRole.Vault) : original;
    }
}
