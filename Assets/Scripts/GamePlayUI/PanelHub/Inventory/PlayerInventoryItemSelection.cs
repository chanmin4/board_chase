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
    public readonly int ammoSlotIndex;
    public readonly ItemSO item;
    public readonly int amount;
    public readonly float armorDurability;
    public readonly float armorMaxDurability;

    public bool HasItem => item != null && amount > 0;
    public bool IsArmor => item is ArmorItemSO;
    public float MissingArmorDurability =>
        IsArmor
            ? Mathf.Max(0f, Mathf.Max(0f, armorMaxDurability) - Mathf.Max(0f, armorDurability))
            : 0f;

    public PlayerInventoryItemSelection(
        PlayerInventoryItemSelectionKind kind,
        int inventoryIndex,
        PlayerInventoryEquipmentSlotKind equipmentSlotKind,
        int ammoSlotIndex,
        ItemSO item,
        int amount,
        float armorDurability,
        float armorMaxDurability)
    {
        this.kind = kind;
        this.inventoryIndex = inventoryIndex;
        this.equipmentSlotKind = equipmentSlotKind;
        this.ammoSlotIndex = ammoSlotIndex;
        this.item = item;
        this.amount = Mathf.Max(0, amount);
        this.armorDurability = armorDurability;
        this.armorMaxDurability = armorMaxDurability;
    }
}
