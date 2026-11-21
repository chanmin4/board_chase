/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Presents one pre-authored passive equipment slot.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PassiveInventorySlotUI : EquipmentInventorySlotUI
{
    [Header("Passive Slot")]
    [SerializeField] private CanvasGroup _canvasGroup;

    private int _passiveSlotIndex = -1;
    private bool _inspectionOnly;
    public void SetCategorySlotIndex(int index, bool inspectionOnly = false) { _passiveSlotIndex = index; _inspectionOnly = inspectionOnly; }

    // Purpose: Caches the component references required before runtime initialization.
    protected override void Awake()
    {
        base.Awake();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (IconImage == null)
            ResolveInheritedIconImage();
    }

    // Purpose: Initializes passive slot for use by this component.
    public void InitializePassiveSlot(PlayerInventoryUI owner, int passiveSlotIndex)
    {
        _passiveSlotIndex = Mathf.Max(0, passiveSlotIndex);
        SetSlotTypeTextVisible(true);
        SetDisplayNameTextVisible(false);
        SetRuntimeSlotTypeOverride("모듈");
        base.Initialize(owner, InventoryDragSourceKind.Equipment, -1);
    }

    // Purpose: Binds passive to the active runtime or UI target.
    public void BindPassive(PassiveItemSO item, bool unlocked)
    {
        SetVisible(unlocked);

        if (!unlocked)
        {
            base.Bind(null);
            string category = _passiveSlotIndex % 2 == 0 ? "전투" : "생존";
            int level = PassiveModuleRulesSO.Active?.SlotLevel((PassiveCategory)(_passiveSlotIndex % 2), _passiveSlotIndex / 2) ?? 1;
            SetRuntimeSlotTypeOverride(_inspectionOnly ? $"{category} {_passiveSlotIndex / 2 + 1} · 빈칸" : $"{category} {_passiveSlotIndex / 2 + 1} · 퀘스트 해금");
            return;
        }

        if (IconImage == null)
            ResolveInheritedIconImage();

        base.Bind(item);
        SetRuntimeSlotTypeOverride(
            item != null
                ? $"{(_passiveSlotIndex % 2 == 0 ? "전투" : "생존")} {_passiveSlotIndex / 2 + 1} · {ModuleRuneStyle.Numeral(item.Rarity)}"
                : $"{(_passiveSlotIndex % 2 == 0 ? "전투" : "생존")} {_passiveSlotIndex / 2 + 1}");
    }

    // Purpose: Handles the incoming left click request or event.
    protected override void HandleLeftClick()
    {
        if (Owner is PlayerInventoryUI playerInventory)
            playerInventory.HandlePassiveSlotLeftClicked(_passiveSlotIndex);
    }

    // Purpose: Handles the incoming right click request or event.
    protected override void HandleRightClick(Vector2 screenPosition)
    {
        if (Owner is PlayerInventoryUI playerInventory)
            playerInventory.HandlePassiveSlotRightClicked(_passiveSlotIndex);
    }

    // Purpose: Handles the incoming drop payload request or event.
    protected override void HandleDropPayload()
    {
        if (Owner is not PlayerInventoryUI playerInventory)
            return;

        if (InventoryDragContext.SourceKind == InventoryDragSourceKind.Inventory)
        {
            playerInventory.HandleInventorySlotDroppedOnPassive(
                InventoryDragContext.InventoryIndex,
                _passiveSlotIndex);
        }
    }

    // Purpose: Updates the visible state.
    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : .28f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    // Purpose: Resolves the inherited icon image needed by this component.
    private void ResolveInheritedIconImage()
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image != null && image.gameObject.name == "Icon")
            {
                SetIconImage(image);
                return;
            }
        }
    }
}
