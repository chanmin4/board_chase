/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Manages InventoryDragSourceKind item storage, equipment, transfer state, and related rules.
 * Execution boundary: Runtime (Assembly-CSharp)
 */


//드래그 중인 게 “인벤토리 칸인지 / 장비 칸인지” 잠깐 저장하는 static 상태.컴포넌트아님XX


public enum InventoryDragSourceKind
{
    None = 0,
    Inventory = 1,
    Equipment = 2,
    EnemyInventory = 3,
    EquipmentAmmo = 4,
    QuickSlot = 5,
    KeyTool = 6
}

public static class InventoryDragContext
{
    public static InventoryDragSourceKind SourceKind { get; private set; }
    public static int InventoryIndex { get; private set; } = -1;
    public static PlayerInventoryEquipmentSlotKind EquipmentSlotKind { get; private set; }
    public static int EquipmentSlotIndex { get; private set; } = -1;
    public static EnemyInventoryUI EnemyInventoryOwner { get; private set; }
    public static int EnemyInventorySlotIndex { get; private set; } = -1;
    public static int EquipmentAmmoSlotIndex { get; private set; } = -1;
    public static int QuickSlotIndex { get; private set; } = -1;
    public static PlayerInventoryRuntime KeyToolInventoryOwner { get; private set; }
    public static int KeyToolSlotIndex { get; private set; } = -1;

    public static void BeginKeyToolDrag(PlayerInventoryRuntime owner, int index)
    {
        Clear();
        SourceKind = InventoryDragSourceKind.KeyTool;
        KeyToolInventoryOwner = owner;
        KeyToolSlotIndex = index;
    }

    public static bool HasPayload => SourceKind != InventoryDragSourceKind.None;

    // Purpose: Begins the inventory drag workflow.
    public static void BeginInventoryDrag(int inventoryIndex)
    {
        SourceKind = InventoryDragSourceKind.Inventory;
        InventoryIndex = inventoryIndex;
        EquipmentSlotKind = default;
        EquipmentSlotIndex = -1;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
        EquipmentAmmoSlotIndex = -1;
        QuickSlotIndex = -1;
    }

    // Purpose: Begins the equipment drag workflow.
    public static void BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind slotKind)
    {
        BeginEquipmentDrag(slotKind, 0);
    }

    // Purpose: Begins the equipment drag workflow.
    public static void BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind slotKind, int equipmentSlotIndex)
    {
        SourceKind = InventoryDragSourceKind.Equipment;
        EquipmentSlotKind = slotKind;
        EquipmentSlotIndex = UnityEngine.Mathf.Max(0, equipmentSlotIndex);
        InventoryIndex = -1;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
        EquipmentAmmoSlotIndex = -1;
        QuickSlotIndex = -1;
    }

    // Purpose: Begins the enemy inventory drag workflow.
    public static void BeginEnemyInventoryDrag(EnemyInventoryUI owner, int slotIndex)
    {
        SourceKind = InventoryDragSourceKind.EnemyInventory;
        EnemyInventoryOwner = owner;
        EnemyInventorySlotIndex = slotIndex;
        InventoryIndex = -1;
        EquipmentSlotKind = default;
        EquipmentSlotIndex = -1;
        EquipmentAmmoSlotIndex = -1;
        QuickSlotIndex = -1;
    }

    // Purpose: Begins the equipment ammo drag workflow.
    public static void BeginEquipmentAmmoDrag(int ammoSlotIndex)
    {
        SourceKind = InventoryDragSourceKind.EquipmentAmmo;
        EquipmentAmmoSlotIndex = ammoSlotIndex;
        InventoryIndex = -1;
        EquipmentSlotKind = default;
        EquipmentSlotIndex = -1;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
        QuickSlotIndex = -1;
    }

    // Purpose: Begins the quick slot drag workflow.
    public static void BeginQuickSlotDrag(int quickSlotIndex)
    {
        SourceKind = InventoryDragSourceKind.QuickSlot;
        QuickSlotIndex = UnityEngine.Mathf.Max(0, quickSlotIndex);
        InventoryIndex = -1;
        EquipmentSlotKind = default;
        EquipmentSlotIndex = -1;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
        EquipmentAmmoSlotIndex = -1;
    }

    // Purpose: Clears the current clear state.
    public static void Clear()
    {
        KeyToolInventoryOwner = null;
        KeyToolSlotIndex = -1;
        InventoryDragGhostUI.Hide();
        SourceKind = InventoryDragSourceKind.None;
        InventoryIndex = -1;
        EquipmentSlotKind = default;
        EquipmentSlotIndex = -1;
        EnemyInventoryOwner = null;
        EnemyInventorySlotIndex = -1;
        EquipmentAmmoSlotIndex = -1;
        QuickSlotIndex = -1;
    }
}

public enum PlayerInventoryDragSourceKind
{
    None = (int)InventoryDragSourceKind.None,
    Inventory = (int)InventoryDragSourceKind.Inventory,
    Equipment = (int)InventoryDragSourceKind.Equipment,
    EnemyInventory = (int)InventoryDragSourceKind.EnemyInventory,
    EquipmentAmmo = (int)InventoryDragSourceKind.EquipmentAmmo,
    QuickSlot = (int)InventoryDragSourceKind.QuickSlot
}

public static class PlayerInventoryDragContext
{
    public static PlayerInventoryDragSourceKind SourceKind =>
        (PlayerInventoryDragSourceKind)InventoryDragContext.SourceKind;

    public static int InventoryIndex => InventoryDragContext.InventoryIndex;
    public static PlayerInventoryEquipmentSlotKind EquipmentSlotKind => InventoryDragContext.EquipmentSlotKind;
    public static int EquipmentSlotIndex => InventoryDragContext.EquipmentSlotIndex;
    public static EnemyInventoryUI EnemyInventoryOwner => InventoryDragContext.EnemyInventoryOwner;
    public static int EnemyInventorySlotIndex => InventoryDragContext.EnemyInventorySlotIndex;
    public static int EquipmentAmmoSlotIndex => InventoryDragContext.EquipmentAmmoSlotIndex;
    public static int QuickSlotIndex => InventoryDragContext.QuickSlotIndex;
    public static bool HasPayload => InventoryDragContext.HasPayload;

    // Purpose: Begins the inventory drag workflow.
    public static void BeginInventoryDrag(int inventoryIndex)
    {
        InventoryDragContext.BeginInventoryDrag(inventoryIndex);
    }

    // Purpose: Begins the equipment drag workflow.
    public static void BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind slotKind)
    {
        InventoryDragContext.BeginEquipmentDrag(slotKind);
    }

    // Purpose: Begins the equipment drag workflow.
    public static void BeginEquipmentDrag(PlayerInventoryEquipmentSlotKind slotKind, int equipmentSlotIndex)
    {
        InventoryDragContext.BeginEquipmentDrag(slotKind, equipmentSlotIndex);
    }

    // Purpose: Begins the enemy inventory drag workflow.
    public static void BeginEnemyInventoryDrag(EnemyInventoryUI owner, int slotIndex)
    {
        InventoryDragContext.BeginEnemyInventoryDrag(owner, slotIndex);
    }

    // Purpose: Begins the equipment ammo drag workflow.
    public static void BeginEquipmentAmmoDrag(int ammoSlotIndex)
    {
        InventoryDragContext.BeginEquipmentAmmoDrag(ammoSlotIndex);
    }

    // Purpose: Begins the quick slot drag workflow.
    public static void BeginQuickSlotDrag(int quickSlotIndex)
    {
        InventoryDragContext.BeginQuickSlotDrag(quickSlotIndex);
    }

    // Purpose: Clears the current clear state.
    public static void Clear()
    {
        InventoryDragContext.Clear();
    }
}
