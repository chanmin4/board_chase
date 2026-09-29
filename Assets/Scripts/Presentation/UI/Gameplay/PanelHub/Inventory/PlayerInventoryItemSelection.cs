/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Manages PlayerInventoryItemSelectionKind item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;

public enum PlayerInventoryItemSelectionKind
{
    None = 0,
    Inventory = 1,
    Equipment = 2,
    Ammo = 3
}

public readonly struct PlayerInventoryItemSelection
{
    public static readonly PlayerInventoryItemSelection None = new(
        PlayerInventoryItemSelectionKind.None,
        -1,
        default,
        -1,
        null,
        0,
        -1f,
        -1f);

    public readonly PlayerInventoryItemSelectionKind kind;
    public readonly int inventoryIndex;
    public readonly PlayerInventoryEquipmentSlotKind equipmentSlotKind;
    public readonly int equipmentSlotIndex;
    public readonly int ammoSlotIndex;
    public readonly ItemSO item;
    public readonly int amount;
    public readonly float armorDurability;
    public readonly float armorMaxDurability;
    public readonly float weaponCurrentDurability;
    public readonly float weaponMaxDurability;
    public readonly float gasMaskFilterDurability;
    public readonly float gasMaskMaxFilterDurability;

    public bool HasItem => item != null && amount > 0;
    public bool IsArmor => item is ArmorItemSO;
    public bool IsRangedWeapon => item is RangedWeaponSO;
    public bool IsGasMask => item is GasMaskItemSO;
    public float MissingArmorDurability =>
        IsArmor
            ? Mathf.Max(0f, Mathf.Max(0f, armorMaxDurability) - Mathf.Max(0f, armorDurability))
            : 0f;
    public float MissingWeaponDurability =>
        IsRangedWeapon
            ? Mathf.Max(0f, Mathf.Max(0f, weaponMaxDurability) - Mathf.Max(0f, weaponCurrentDurability))
            : 0f;
    public float MissingGasMaskFilterDurability =>
        IsGasMask
            ? Mathf.Max(0f, Mathf.Max(0f, gasMaskMaxFilterDurability) - Mathf.Max(0f, gasMaskFilterDurability))
            : 0f;
    public bool IsRepairable => IsArmor || IsRangedWeapon || IsGasMask;

    public PlayerInventoryItemSelection(
        PlayerInventoryItemSelectionKind kind,
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind equipmentSlotKind,
        int ammoSlotIndex,
        ItemSO item,
        int amount,
        float armorDurability,
        float armorMaxDurability,
        int equipmentSlotIndex = -1,
        float weaponCurrentDurability = -1f,
        float weaponMaxDurability = -1f,
        float gasMaskFilterDurability = -1f,
        float gasMaskMaxFilterDurability = -1f)
    {
        this.kind = kind;
        this.inventoryIndex = inventoryIndex;
        this.equipmentSlotKind = equipmentSlotKind;
        this.equipmentSlotIndex = equipmentSlotIndex;
        this.ammoSlotIndex = ammoSlotIndex;
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.armorDurability = armorDurability;
        this.armorMaxDurability = armorMaxDurability;
        this.weaponCurrentDurability = weaponCurrentDurability;
        this.weaponMaxDurability = weaponMaxDurability;
        this.gasMaskFilterDurability = gasMaskFilterDurability;
        this.gasMaskMaxFilterDurability = gasMaskMaxFilterDurability;
    }
}
