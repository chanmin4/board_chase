/*
 * Role hierarchy: Presentation > UI > Gameplay > PanelHub > Inventory
 * Primary responsibility: Opens the carried Key Tool as a 4-column nested
 * inventory surface while reusing the Player Inventory slot prefab.
 * Execution boundary: Runtime (Assembly-CSharp)
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// A lightweight child panel of <see cref="PlayerInventoryUI"/>. It is not a
/// second ownership surface: it presents the runtime-owned physical key list
/// inside the one Key Tool item currently carried by the player.
/// </summary>
[DisallowMultipleComponent]
public sealed class KeyToolContainerPanel : MonoBehaviour, IInventorySlotOwner
{
    private PlayerInventoryUI _host;
    private PlayerInventoryRuntime _inventory;
    private KeyToolItemSO _keyTool;
    private CanvasGroup _group;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _usageText;
    private TextMeshProUGUI _feedbackText;
    private RectTransform _slotGrid;
    private GridLayoutGroup _gridLayout;
    private readonly List<InventoryItemSlotUI> _slots = new();
    private bool _isOpen;

    // Purpose: Gets or builds the panel below the active Player Inventory UI, then opens the selected Key Tool.
    public static bool TryOpen(
        PlayerInventoryUI host,
        PlayerInventoryRuntime inventory,
        KeyToolItemSO keyTool)
    {
        if (host == null || inventory == null || keyTool == null || !inventory.HasKeyTool)
            return false;

        KeyToolContainerPanel panel = host.GetComponentInChildren<KeyToolContainerPanel>(true);
        if (panel == null)
        {
            GameObject root = new(
                nameof(KeyToolContainerPanel),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(KeyToolContainerPanel));
            root.layer = host.gameObject.layer;
            root.transform.SetParent(host.transform, false);
            panel = root.GetComponent<KeyToolContainerPanel>();
            panel.BuildRuntimeUi();
        }

        panel.Open(host, inventory, keyTool);
        return panel._isOpen;
    }

    // Purpose: Closes the Key Tool when Escape is pressed without affecting the containing Player Panel.
    private void Update()
    {
        if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    // Purpose: Releases transient event subscriptions when the panel is destroyed with its Player Panel hierarchy.
    private void OnDestroy()
    {
        UnsubscribeInventory();
    }

    // Purpose: Opens the configured Key Tool and prepares its visible physical key slots.
    private void Open(
        PlayerInventoryUI host,
        PlayerInventoryRuntime inventory,
        KeyToolItemSO keyTool)
    {
        UnsubscribeInventory();
        _host = host;
        _inventory = inventory;
        _keyTool = keyTool;
        _inventory.KeyToolChanged += Refresh;
        _isOpen = true;

        EnsureSlots();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SetVisible(true);
        Refresh();
    }

    // Purpose: Hides the nested surface and returns control to the normal Player Inventory panel.
    private void Close()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        UnsubscribeInventory();
        SetVisible(false);
    }

    // Purpose: Refreshes all reusable slot visuals from the current physical Key Tool contents.
    private void Refresh()
    {
        if (!_isOpen)
            return;

        if (_inventory == null || !_inventory.HasKeyTool || _inventory.KeyTool != _keyTool)
        {
            Close();
            return;
        }

        EnsureSlots();

        if (_titleText != null)
            _titleText.text = _keyTool.DisplayName.ToUpperInvariant();

        if (_usageText != null)
            _usageText.text = UIRuntimeLocalization.Format(
                "UI_Inventory",
                "Inventory_KeyToolUsageFormat",
                "{0} key slots  •  {1} columns",
                _inventory.KeyToolSlotCapacity,
                _inventory.KeyToolColumnCount);

        for (int index = 0; index < _slots.Count; index++)
        {
            InventoryItemSlotUI slot = _slots[index];
            if (slot == null)
                continue;

            PlayerKeyToolSlotSnapshot snapshot = _inventory.GetKeyToolSlotSnapshot(index);
            slot.gameObject.SetActive(index < _inventory.KeyToolSlotCapacity);

            if (snapshot.HasKey)
                slot.BindKeyToolItem(snapshot.Key, snapshot.RemainingUses);
            else
                slot.Bind(null);
        }
    }

    // Purpose: Ensures the reusable grid follows the carried Key Tool's configured capacity and column count.
    private void EnsureSlots()
    {
        if (_inventory == null || _slotGrid == null || _gridLayout == null)
            return;

        int capacity = _inventory.KeyToolSlotCapacity;
        _gridLayout.constraintCount = Mathf.Max(1, _inventory.KeyToolColumnCount);

        InventoryItemSlotUI template = _host != null ? _host.InventorySlotTemplate : null;
        if (template == null)
            return;

        while (_slots.Count < capacity)
        {
            int index = _slots.Count;
            InventoryItemSlotUI slot = Instantiate(template, _slotGrid);
            slot.name = $"KeyToolSlot_{index:00}";
            slot.gameObject.SetActive(true);
            slot.Initialize(this, index, false, true, InventoryDragSourceKind.Inventory);
            _slots.Add(slot);
        }

        for (int index = 0; index < _slots.Count; index++)
        {
            InventoryItemSlotUI slot = _slots[index];
            if (slot == null)
                continue;

            slot.name = $"KeyToolSlot_{index:00}";
            slot.Initialize(this, index, false, true, InventoryDragSourceKind.Inventory);
            slot.gameObject.SetActive(index < capacity);
        }
    }

    // Purpose: Handles a double-clicked Key Tool slot by returning its physical key to normal inventory.
    public void HandleInventorySlotLeftClicked(int slotIndex)
    {
        if (_inventory == null)
            return;

        if (_inventory.TryWithdrawKeyToolSlot(slotIndex, out string message))
            SetFeedback(message, true);
        else
            SetFeedback(message, false);

        Refresh();
    }

    // Purpose: Ignores context-menu requests; Key Tool slots use only double-click withdrawal in this first pass.
    public void HandleInventorySlotRightClicked(int slotIndex, Vector2 screenPosition)
    {
    }

    // Purpose: Keeps drag-and-drop disabled for the contained-key first pass.
    public void HandleInventorySlotDroppedOnInventory(int fromIndex, int toIndex)
    {
    }

    // Purpose: Rejects moving nested keys directly into equipment.
    public void HandleInventorySlotDroppedOnEquipment(int inventoryIndex, PlayerInventoryEquipmentSlotKind slotKind)
    {
    }

    // Purpose: Rejects moving equipment directly into nested key storage.
    public void HandleEquipmentSlotDroppedOnInventory(PlayerInventoryEquipmentSlotKind slotKind, int targetInventoryIndex)
    {
    }

    // Purpose: Rejects moving nested keys into ammo slots.
    public void HandleInventorySlotDroppedOnAmmo(int inventoryIndex, int ammoSlotIndex)
    {
    }

    // Purpose: Rejects moving ammo directly into nested key storage.
    public void HandleAmmoSlotDroppedOnInventory(int ammoSlotIndex, int targetInventoryIndex)
    {
    }

    // Purpose: Rejects rearranging nested keys through the ammo-slot path.
    public void HandleAmmoSlotDroppedOnAmmo(int fromAmmoSlotIndex, int toAmmoSlotIndex)
    {
    }

    // Purpose: Keeps equipment-only click behavior outside the Key Tool panel.
    public void HandleEquipmentSlotLeftClicked(PlayerInventoryEquipmentSlotKind slotKind)
    {
    }

    // Purpose: Keeps equipment-only context behavior outside the Key Tool panel.
    public void HandleEquipmentSlotRightClicked(PlayerInventoryEquipmentSlotKind slotKind, Vector2 screenPosition)
    {
    }

    // Purpose: Keeps ammo-only click behavior outside the Key Tool panel.
    public void HandleAmmoSlotLeftClicked(int ammoSlotIndex)
    {
    }

    // Purpose: Keeps ammo-only context behavior outside the Key Tool panel.
    public void HandleAmmoSlotRightClicked(int ammoSlotIndex, Vector2 screenPosition)
    {
    }

    // Purpose: Leaves item detail ownership with PlayerInventoryUI rather than duplicating its tooltip hierarchy.
    public void HandleItemTooltipEntered(ItemSO item, RectTransform anchor)
    {
    }

    // Purpose: Leaves item detail ownership with PlayerInventoryUI rather than duplicating its tooltip hierarchy.
    public void HandleItemTooltipExited()
    {
    }

    // Purpose: Builds the panel once using the current inventory slot prefab for all physical key cells.
    private void BuildRuntimeUi()
    {
        RectTransform root = transform as RectTransform;
        SetFullStretch(root);

        Image dimmer = GetComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.38f);
        dimmer.raycastTarget = true;

        _group = GetComponent<CanvasGroup>();

        Image frame = CreateImage("KeyToolFrame", transform, new Color(0.01f, 0.08f, 0.11f, 0.98f));
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.52f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(610f, 710f);
        frame.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0.78f, 1f, 0.85f);

        _titleText = CreateText("Title", frame.transform, 32f, FontStyles.Bold, TextAlignmentOptions.Center);
        _titleText.color = new Color(0.74f, 0.95f, 1f, 1f);
        SetAnchors(_titleText.rectTransform, new Vector2(0.1f, 0.89f), new Vector2(0.9f, 0.96f));

        _usageText = CreateText("Capacity", frame.transform, 16f, FontStyles.Normal, TextAlignmentOptions.Center);
        _usageText.color = new Color(0.59f, 0.78f, 0.86f, 1f);
        SetAnchors(_usageText.rectTransform, new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.885f));

        _slotGrid = CreateRect("KeySlots", frame.transform);
        SetAnchors(_slotGrid, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.81f));
        _gridLayout = _slotGrid.gameObject.AddComponent<GridLayoutGroup>();
        _gridLayout.cellSize = new Vector2(118f, 118f);
        _gridLayout.spacing = new Vector2(9f, 9f);
        _gridLayout.childAlignment = TextAnchor.UpperCenter;
        _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _gridLayout.constraintCount = 4;

        _feedbackText = CreateText("Feedback", frame.transform, 16f, FontStyles.Normal, TextAlignmentOptions.Center);
        _feedbackText.color = new Color(0.63f, 0.84f, 0.91f, 1f);
        SetAnchors(_feedbackText.rectTransform, new Vector2(0.09f, 0.095f), new Vector2(0.91f, 0.15f));

        TextMeshProUGUI help = CreateText("Help", frame.transform, 14f, FontStyles.Normal, TextAlignmentOptions.Center);
        help.text = UIRuntimeLocalization.Get(
            "UI_Inventory",
            "Inventory_KeyToolHelp",
            "Double-click a key to return it to inventory  •  Esc / X: Close");
        help.color = new Color(0.49f, 0.63f, 0.69f, 1f);
        SetAnchors(help.rectTransform, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.08f));

        Button closeButton = CreateButton("Close", frame.transform, new Color(0.36f, 0.08f, 0.1f, 1f));
        closeButton.onClick.AddListener(Close);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.93f, 0.925f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(46f, 46f);
        TextMeshProUGUI closeLabel = CreateText("Label", closeButton.transform, 25f, FontStyles.Bold, TextAlignmentOptions.Center);
        closeLabel.text = "X";
        SetFullStretch(closeLabel.rectTransform);

        SetVisible(false);
    }

    // Purpose: Shows or hides the panel while keeping its raycast behavior synchronized.
    private void SetVisible(bool visible)
    {
        if (_group == null)
            return;

        _group.alpha = visible ? 1f : 0f;
        _group.interactable = visible;
        _group.blocksRaycasts = visible;
    }

    // Purpose: Presents one local transfer result in the panel footer.
    private void SetFeedback(string message, bool success)
    {
        if (_feedbackText == null)
            return;

        _feedbackText.text = message ?? string.Empty;
        _feedbackText.color = success
            ? new Color(0.54f, 0.95f, 0.71f, 1f)
            : new Color(1f, 0.55f, 0.48f, 1f);
    }

    // Purpose: Removes the runtime inventory observer before rebinding or destroying the panel.
    private void UnsubscribeInventory()
    {
        if (_inventory != null)
            _inventory.KeyToolChanged -= Refresh;
    }

    // Purpose: Creates one generic rect-transform child.
    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject child = new(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    // Purpose: Creates one solid-color Image child.
    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        Image image = CreateRect(objectName, parent).gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    // Purpose: Creates one button with an image target.
    private static Button CreateButton(string objectName, Transform parent, Color color)
    {
        Image image = CreateImage(objectName, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    // Purpose: Creates one non-raycast TMP text element.
    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateRect(objectName, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    // Purpose: Fills the parent rect without retaining authored offsets.
    private static void SetFullStretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Purpose: Applies normalized stretch anchors without introducing offsets.
    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
