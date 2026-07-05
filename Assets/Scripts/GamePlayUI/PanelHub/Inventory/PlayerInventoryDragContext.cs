
//드래그 중인 게 “인벤토리 칸인지 / 장비 칸인지” 잠깐 저장하는 static 상태.컴포넌트아님XX


public enum InventoryDragSourceKind
{
    None = 0,
    Inventory = 1,
    Equipment = 2,
    EnemyInventory = 3,
    EquipmentAmmo = 4
}

public static class InventoryDragContext
{
    public static InventoryDragSourceKind SourceKind { get; private set; }
    public static int InventoryIndex { get; private set; } = -1;
    public static PlayerInventoryEquipmentSlotKind EquipmentSlotKind { get; private set; }
    public static EnemyInventoryUI EnemyInventoryOwner { get; private set; }
    public static int EnemyInventorySlotIndex { get; private set; } = -1;
    public static int EquipmentAmmoSlotIndex { get; private set; } = -1;

    public static bool HasPayload => SourceKind != InventoryDragSourceKind.None;

    public static void BeginInventoryDrag(int inventoryIndex)
    {
        SourceKind = InventoryDragSourceKind.Inventory;
        InventoryIndex = inventoryIndex;
    }

    public static void BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind slotKind)
    {
        SourceKind = InventoryDragSourceKind.Equipment;
        EquipmentSlotKind = slotKind;
        InventoryIndex = -1;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
        EquipmentAmmoSlotIndex = -1;
    }

    public static void BeginEnemyInventoryDrag(EnemyInventoryUI owner, int slotIndex)
    {
        SourceKind = InventoryDragSourceKind.EnemyInventory;
        EnemyInventoryOwner = owner;
        EnemyInventorySlotIndex = slotIndex;
        InventoryIndex = -1;
        EquipmentSlotKind = default;
        EquipmentAmmoSlotIndex = -1;
    }

    public static void BeginEquipmentAmmoDrag(int ammoSlotIndex)
    {
        SourceKind = InventoryDragSourceKind.EquipmentAmmo;
        EquipmentAmmoSlotIndex = ammoSlotIndex;
        InventoryIndex = -1;
        EquipmentSlotKind = default;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
    }

    public static void Clear()
    {
        SourceKind = InventoryDragSourceKind.None;
        InventoryIndex = -1;
        EquipmentSlotKind = default;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
        EquipmentAmmoSlotIndex = -1;
    }
}

public enum PlayerInventoryDragSourceKind
{
    None = (int)InventoryDragSourceKind.None,
    Inventory = (int)InventoryDragSourceKind.Inventory,
    Equipment = (int)InventoryDragSourceKind.Equipment,
    EnemyInventory = (int)InventoryDragSourceKind.EnemyInventory,
    EquipmentAmmo = (int)InventoryDragSourceKind.EquipmentAmmo
}

public static class PlayerInventoryDragContext
{
    public static PlayerInventoryDragSourceKind SourceKind =>
        (PlayerInventoryDragSourceKind)InventoryDragContext.SourceKind;

    public static int InventoryIndex => InventoryDragContext.InventoryIndex;
    public static PlayerInventoryEquipmentSlotKind EquipmentSlotKind => InventoryDragContext.EquipmentSlotKind;
    public static EnemyInventoryUI EnemyInventoryOwner => InventoryDragContext.EnemyInventoryOwner;
    public static int EnemyInventorySlotIndex => InventoryDragContext.EnemyInventorySlotIndex;
    public static int EquipmentAmmoSlotIndex => InventoryDragContext.EquipmentAmmoSlotIndex;
    public static bool HasPayload => InventoryDragContext.HasPayload;

    public static void BeginInventoryDrag(int inventoryIndex)
    {
        InventoryDragContext.BeginInventoryDrag(inventoryIndex);
    }

    public static void BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind slotKind)
    {
        InventoryDragContext.BeginEquipmentDrag(slotKind);
    }

    public static void BeginEnemyInventoryDrag(EnemyInventoryUI owner, int slotIndex)
    {
        InventoryDragContext.BeginEnemyInventoryDrag(owner, slotIndex);
    }

    public static void BeginEquipmentAmmoDrag(int ammoSlotIndex)
    {
        InventoryDragContext.BeginEquipmentAmmoDrag(ammoSlotIndex);
    }

    public static void Clear()
    {
        InventoryDragContext.Clear();
    }
}
